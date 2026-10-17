using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using UIFramework.Core.Dpi;
using UIFramework.Core.Skinning;

namespace UIFramework.Core.Rendering
{
    /// <summary>
    /// Bündelt sämtliche GDI+-Aufrufe des Frameworks an einer Stelle.
    ///
    /// Zustandslos und ohne Kenntnis von Controls: bekommt Graphics, Rectangle,
    /// ElementAppearance und dpi — sonst nichts. Genau deshalb kann das DataGrid
    /// (Teilprojekt 2) diese Methoden später pro Zelle rufen, wo es gar keine
    /// Control-Instanzen gibt.
    ///
    /// Dies ist auch der einzige Ort, an dem ein Direct2D-Backend andocken müsste.
    /// </summary>
    public static class SkinPainter
    {
        public static void DrawBackground(Graphics g, Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (appearance.Background.A == 0 && !appearance.HasGradient) return;

            var corners = DpiScale.Scale(appearance.Corners, dpi);
            var previousMode = g.SmoothingMode;
            g.SmoothingMode = corners.IsZero ? SmoothingMode.None : SmoothingMode.AntiAlias;

            try
            {
                using (var path = RoundedRectangle.Create(bounds, corners))
                {
                    if (appearance.HasGradient)
                    {
                        using (var brush = new LinearGradientBrush(
                            bounds, appearance.Background, appearance.BackgroundGradientEnd.Value,
                            LinearGradientMode.Vertical))
                        {
                            g.FillPath(brush, path);
                        }
                    }
                    else
                    {
                        g.FillPath(ResourceCache.Shared.GetBrush(appearance.Background), path);
                    }
                }
            }
            finally
            {
                g.SmoothingMode = previousMode;
            }
        }

        public static void DrawBorder(Graphics g, Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (appearance.BorderWidth <= 0 || appearance.BorderColor.A == 0) return;

            int width = DpiScale.Scale(appearance.BorderWidth, dpi);
            if (width <= 0) return;

            var corners = DpiScale.Scale(appearance.Corners, dpi);
            var previousMode = g.SmoothingMode;
            g.SmoothingMode = corners.IsZero ? SmoothingMode.None : SmoothingMode.AntiAlias;

            try
            {
                // Der Stift zeichnet mittig auf dem Pfad: ohne dieses Einrücken
                // läge die halbe Rahmenbreite außerhalb der Bounds.
                int inset = width / 2;
                var rect = Rectangle.Inflate(bounds, -inset, -inset);
                if (rect.Width <= 0 || rect.Height <= 0) return;

                using (var path = RoundedRectangle.Create(rect, corners))
                {
                    var pen = ResourceCache.Shared.GetPen(appearance.BorderColor, width);
                    g.DrawPath(pen, path);
                }
            }
            finally
            {
                g.SmoothingMode = previousMode;
            }
        }

        public static void DrawText(Graphics g, string text, Rectangle bounds, ElementAppearance appearance,
            int dpi, ContentAlignment alignment)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (string.IsNullOrEmpty(text)) return;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var font = ResourceCache.Shared.GetFont(appearance.Font, dpi);

            // TextRenderer statt Graphics.DrawString: ClearType. Bei 9pt ist der
            // Unterschied deutlich sichtbar.
            TextRenderer.DrawText(g, text, font, bounds, appearance.ForeColor, ToTextFormatFlags(alignment));
        }

        /// <summary>
        /// Wie <see cref="DrawText"/>, nimmt aber unbeschnittene Bounds entgegen und
        /// zieht Padding UND Rahmenbreite des Appearance selbst über DpiScale ab.
        /// Einzugs-Konvention: Padding + Rahmenbreite auf allen vier Seiten — dieselbe
        /// wie <see cref="GetContentRectangle"/> und <see cref="InflateByPadding"/>
        /// (NICHT dieselbe wie <see cref="DrawFocus"/>, die bewusst nur um das
        /// Padding einzieht). So muss kein Control (Controls-Assembly) selbst mit
        /// DpiScale rechnen.
        /// </summary>
        public static void DrawPaddedText(Graphics g, string text, Rectangle bounds, ElementAppearance appearance,
            int dpi, ContentAlignment alignment)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (string.IsNullOrEmpty(text)) return;

            var content = GetContentRectangle(bounds, appearance, dpi);

            DrawText(g, text, content, appearance, dpi, alignment);
        }

        /// <summary>
        /// Berechnet das Innenrechteck für Kind-Inhalte: bounds abzüglich skaliertem
        /// Padding und skalierter Rahmenbreite auf allen vier Seiten. Einzugs-
        /// Konvention: Padding + Rahmenbreite (nicht nur Padding) — dieselbe wie
        /// <see cref="DrawPaddedText"/> und <see cref="InflateByPadding"/>. Nicht-
        /// zeichnende Hilfsmethode wie <see cref="MeasureText"/> — lebt hier, damit
        /// die DpiScale-Arithmetik nicht in die Controls-Assembly abwandert (etwa in
        /// SkinPanel.DisplayRectangle).
        /// </summary>
        public static Rectangle GetContentRectangle(Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));

            var padding = DpiScale.Scale(appearance.Padding, dpi);
            int border = DpiScale.Scale(appearance.BorderWidth, dpi);

            int left = padding.Left + border;
            int top = padding.Top + border;
            int right = padding.Right + border;
            int bottom = padding.Bottom + border;

            var rect = new Rectangle(
                bounds.Left + left,
                bounds.Top + top,
                bounds.Width - left - right,
                bounds.Height - top - bottom);

            if (rect.Width < 0) rect.Width = 0;
            if (rect.Height < 0) rect.Height = 0;

            return rect;
        }

        /// <summary>
        /// Vergrößert eine gemessene Inhaltsgröße um das DPI-skalierte Padding UND
        /// die DPI-skalierte Rahmenbreite des Appearance — die Umkehrrichtung zu
        /// <see cref="GetContentRectangle"/>. Einzugs-Konvention: Padding +
        /// Rahmenbreite auf allen vier Seiten, wie bei <see cref="GetContentRectangle"/>
        /// und <see cref="DrawPaddedText"/>. Lebt hier aus demselben Grund wie jene
        /// Methode: die DpiScale-Arithmetik darf die Controls-Assembly nicht
        /// erreichen (etwa in SkinLabel.GetPreferredSize).
        /// </summary>
        public static Size InflateByPadding(Size contentSize, ElementAppearance appearance, int dpi)
        {
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));

            var padding = DpiScale.Scale(appearance.Padding, dpi);
            int border = DpiScale.Scale(appearance.BorderWidth, dpi);

            return new Size(
                contentSize.Width + padding.Horizontal + (border * 2),
                contentSize.Height + padding.Vertical + (border * 2));
        }

        public static Size MeasureText(Graphics g, string text, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (string.IsNullOrEmpty(text)) return Size.Empty;

            var font = ResourceCache.Shared.GetFont(appearance.Font, dpi);
            return TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// Zeichnet den Fokusring. Einzugs-Konvention bewusst abweichend von
        /// <see cref="GetContentRectangle"/>/<see cref="DrawPaddedText"/>/
        /// <see cref="InflateByPadding"/>: zieht NUR das Padding ab, NICHT die
        /// Rahmenbreite — der Ring soll auf dem Rahmen liegen, nicht innerhalb von
        /// ihm. Ein Angleichen an die Padding+Rahmen-Konvention würde den Ring bei
        /// jedem Rahmen &gt; 0 sichtbar verkleinern (frühere Review-Entscheidung).
        /// </summary>
        public static void DrawFocus(Graphics g, Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));

            var padding = DpiScale.Scale(appearance.Padding, dpi);
            var rect = new Rectangle(
                bounds.Left + padding.Left,
                bounds.Top + padding.Top,
                bounds.Width - padding.Horizontal,
                bounds.Height - padding.Vertical);

            if (rect.Width <= 0 || rect.Height <= 0) return;

            DrawBorder(g, rect, appearance, dpi);
        }

        /// <summary>
        /// Wie <see cref="DrawText"/>, aber MIT Mnemonic-Verarbeitung: "&amp;D"
        /// unterstreicht das D, "&amp;&amp;" zeichnet ein echtes &amp;. Eigene Methode
        /// statt eines Flags an DrawText, damit der NoPrefix-Schutz dort (Reitertitel
        /// "Module &amp; Maps") unangetastet bleibt. Bewusst ohne EndEllipsis: Menüs
        /// messen sich passend, nichts wird abgeschnitten.
        /// </summary>
        public static void DrawMnemonicText(Graphics g, string text, Rectangle bounds, ElementAppearance appearance,
            int dpi, ContentAlignment alignment)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (string.IsNullOrEmpty(text)) return;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var font = ResourceCache.Shared.GetFont(appearance.Font, dpi);
            TextRenderer.DrawText(g, text, font, bounds, appearance.ForeColor,
                MnemonicFlags | AlignmentFlags(alignment));
        }

        /// <summary>Gegenstück zu <see cref="DrawMnemonicText"/>: misst mit
        /// Prefix-Verarbeitung, "&amp;Datei" ist also so breit wie "Datei".</summary>
        public static Size MeasureMnemonicText(Graphics g, string text, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (string.IsNullOrEmpty(text)) return Size.Empty;

            var font = ResourceCache.Shared.GetFont(appearance.Font, dpi);
            return TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), MnemonicFlags);
        }

        /// <summary>
        /// Eine horizontale Trennlinie, vertikal mittig in bounds, horizontal um das
        /// skalierte Padding eingezogen — BorderColor und DPI-skalierte BorderWidth
        /// der Erscheinung. Für Menü-Separatoren: Die Linie ist dort kein Rahmen um
        /// etwas, sondern eigenständiger Inhalt; ein Control darf die Dicke nicht
        /// selbst skalieren (keine DPI-Arithmetik in der Controls-Assembly).
        /// </summary>
        public static void DrawSeparatorLine(Graphics g, Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (appearance.BorderWidth <= 0 || appearance.BorderColor.A == 0) return;

            int width = DpiScale.Scale(appearance.BorderWidth, dpi);
            if (width <= 0) return;

            var padding = DpiScale.Scale(appearance.Padding, dpi);
            int y = bounds.Top + bounds.Height / 2;
            int left = bounds.Left + padding.Left;
            int right = bounds.Right - padding.Right;
            if (right <= left) return;

            var pen = ResourceCache.Shared.GetPen(appearance.BorderColor, width);
            g.DrawLine(pen, left, y, right, y);
        }

        /// <summary>
        /// Malt ein App-Bild skaliert in die Zielzone. Die Zone kommt bereits in
        /// Gerätepixeln (das Ribbon leitet sie aus der gemessenen Textzeilenhöhe
        /// ab — großes Bild 2x, kleines 1x); hier passiert deshalb bewusst KEINE
        /// DpiScale-Rechnung, nur hochwertige Interpolation. enabled=false malt
        /// halbtransparent über eine ColorMatrix — ausgegraute Knöpfe brauchen
        /// keine zweiten Bitmaps von der App. Das Bild gehört der App: es wird
        /// weder disposed noch kopiert.
        /// </summary>
        public static void DrawScaledImage(Graphics g, Image image, Rectangle bounds, bool enabled)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (image == null) return;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var previous = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            try
            {
                if (enabled)
                {
                    g.DrawImage(image, bounds);
                }
                else
                {
                    // Nur die Deckkraft dämpfen — Farbstich bleibt erhalten, das
                    // Auge erkennt den Knopf weiterhin.
                    var matrix = new ColorMatrix { Matrix33 = 0.35f };
                    using (var attributes = new ImageAttributes())
                    {
                        attributes.SetColorMatrix(matrix);
                        g.DrawImage(image, bounds, 0, 0, image.Width, image.Height,
                            GraphicsUnit.Pixel, attributes);
                    }
                }
            }
            finally
            {
                g.InterpolationMode = previous;
            }
        }

        /// <summary>
        /// Senkrechtes Gegenstück zu <see cref="DrawSeparatorLine"/>: Linie in
        /// BorderColor/DPI-skalierter BorderWidth, horizontal mittig, oben/unten
        /// um das skalierte Padding eingezogen. Für Ribbon-Separatoren zwischen
        /// Item-Spalten.
        /// </summary>
        public static void DrawVerticalSeparatorLine(Graphics g, Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (appearance.BorderWidth <= 0 || appearance.BorderColor.A == 0) return;

            int width = DpiScale.Scale(appearance.BorderWidth, dpi);
            if (width <= 0) return;

            var padding = DpiScale.Scale(appearance.Padding, dpi);
            int x = bounds.Left + bounds.Width / 2;
            int top = bounds.Top + padding.Top;
            int bottom = bounds.Bottom - padding.Bottom;
            if (bottom <= top) return;

            var pen = ResourceCache.Shared.GetPen(appearance.BorderColor, width);
            g.DrawLine(pen, x, top, x, bottom);
        }

        /// <summary>
        /// Die Aufklapp-Glyphe eines Baumknotens: ein gefülltes Dreieck in
        /// ForeColor, zugeklappt nach rechts (▶), aufgeklappt nach unten (▼),
        /// mittig in bounds, Kante = halbe kürzere Seite. Die Zone kommt in
        /// Gerätepixeln (der Baum leitet sie aus der gemessenen Textzeilenhöhe
        /// ab) — hier deshalb bewusst KEINE DpiScale-Rechnung, Muster
        /// DrawScaledImage. Kein Bitmap: die Glyphe folgt dem Skin.
        /// </summary>
        public static void DrawDisclosureGlyph(Graphics g, Rectangle bounds, bool expanded, ElementAppearance appearance)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (appearance.ForeColor.A == 0) return;

            int half = Math.Max(2, Math.Min(bounds.Width, bounds.Height) / 4);
            int cx = bounds.Left + bounds.Width / 2;
            int cy = bounds.Top + bounds.Height / 2;

            Point[] points = expanded
                ? new[]
                {
                    new Point(cx - half, cy - half / 2),
                    new Point(cx + half, cy - half / 2),
                    new Point(cx, cy - half / 2 + half)
                }
                : new[]
                {
                    new Point(cx - half / 2, cy - half),
                    new Point(cx - half / 2, cy + half),
                    new Point(cx - half / 2 + half, cy)
                };

            var previousMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            try
            {
                g.FillPolygon(ResourceCache.Shared.GetBrush(appearance.ForeColor), points);
            }
            finally
            {
                g.SmoothingMode = previousMode;
            }
        }

        // NoPadding|SingleLine OHNE NoPrefix (Mnemonic-Verarbeitung an) und OHNE
        // EndEllipsis (Menüs messen sich passend).
        private const TextFormatFlags MnemonicFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

        private static TextFormatFlags ToTextFormatFlags(ContentAlignment alignment)
        {
            // NoPrefix: ohne dieses Flag behandelt TextRenderer ein einzelnes
            // "&" als Tastaturkürzel-Marker (unterstreicht das folgende
            // Zeichen, das "&" selbst verschwindet) -- kein Control in diesem
            // Framework nutzt Mnemonics, aber jeder Text mit einem echten "&"
            // (z. B. ein Reitertitel "Module & Maps") würde sonst falsch
            // gezeichnet. Live an einem Verwender-Fenster gefunden: der
            // Reiter zeigte "Module _Maps" statt "Module & Maps".
            // Menü-Texte sind die Ausnahme und laufen über DrawMnemonicText.
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix;

            return flags | AlignmentFlags(alignment);
        }

        private static TextFormatFlags AlignmentFlags(ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    return TextFormatFlags.Top | TextFormatFlags.Left;
                case ContentAlignment.TopCenter:
                    return TextFormatFlags.Top | TextFormatFlags.HorizontalCenter;
                case ContentAlignment.TopRight:
                    return TextFormatFlags.Top | TextFormatFlags.Right;
                case ContentAlignment.MiddleLeft:
                    return TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                case ContentAlignment.MiddleRight:
                    return TextFormatFlags.VerticalCenter | TextFormatFlags.Right;
                case ContentAlignment.BottomLeft:
                    return TextFormatFlags.Bottom | TextFormatFlags.Left;
                case ContentAlignment.BottomCenter:
                    return TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter;
                case ContentAlignment.BottomRight:
                    return TextFormatFlags.Bottom | TextFormatFlags.Right;
                default:
                    return TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;
            }
        }
    }
}
