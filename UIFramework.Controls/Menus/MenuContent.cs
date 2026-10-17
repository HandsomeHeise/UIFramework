using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// EIN aufgeklapptes Menü-Level als Popup-Gast. Das Interface bleibt
    /// unverändert schmal (Spec-Entscheidung): Alles, was Menüs zusätzlich
    /// brauchen — Hover-Meldung, Klick-Meldung, Tastatur-Auswahl, Zeilen-
    /// geometrie für die Untermenü-Platzierung — sind Member DIESER Klasse;
    /// der Controller kennt die Instanz, der Host nur das Interface.
    /// Tasten beantwortet HandleKey nie: Im Menü-Modus wandert der Fokus
    /// nicht, Tastatur kommt ausschließlich über den MessageFilter/Controller.
    /// </summary>
    internal sealed class MenuContent : IPopupContent
    {
        private readonly IList<MenuEntry> _entries;
        private Rectangle[] _rows = new Rectangle[0];   // von Measure gesetzt
        private bool _measured;
        private int _hoverIndex = -1;
        private int _selectedIndex = -1;

        // Spaltenbreiten/Zeilenhöhen, von Measure gesetzt — Gutter und
        // Pfeilzone sind quadratisch (Breite = Zeilenhöhe normaler Einträge).
        private int _rowHeight = 1;
        private int _separatorHeight = 1;
        private int _gap;
        private int _textWidth;
        private int _shortcutWidth;

        public event Action<int> HoveredIndexChanged;
        public event Action<MenuEntry> EntryClicked;
        public event EventHandler VisualChanged;

        /// <summary>Interface-Pflicht (IPopupContent), aber bei einem Menü-Level
        /// bedeutungslos: Ob und wann ein Popup schließt, entscheidet der
        /// MenuController (Tastatur/Klick außerhalb/Ausführen), nicht der
        /// gemalte Gast. Leere Accessoren statt eines feldgestützten Events,
        /// damit das NIE gefeuert werden kann (und der Compiler kein CS0067
        /// meldet, weil der Fall bewusst und nicht vergessen ist).</summary>
        public event EventHandler CloseRequested { add { } remove { } }

        public MenuContent(IList<MenuEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            _entries = entries;
        }

        public IList<MenuEntry> Entries { get { return _entries; } }

        /// <summary>Tastatur-Auswahl (−1 = keine). Setzen feuert VisualChanged —
        /// No-op-Unterdrückung bei gleichem Wert, Konvention des Frameworks.</summary>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                RaiseVisualChanged();
            }
        }

        /// <summary>Zeilen-Rechteck (Popup-lokale Koordinaten), gültig nach Measure —
        /// der Controller braucht es für die Untermenü-Platzierung.</summary>
        public Rectangle RowBounds(int index)
        {
            if (!_measured)
                throw new InvalidOperationException(
                    "MenuContent.RowBounds ist erst nach Measure gültig — vorher wurden die Zeilen nicht vermessen.");
            return _rows[index];
        }

        public Size Measure(Graphics g, int dpi, int anchorWidth)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            var itemAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuItem, ElementState.Normal);
            var separatorAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuSeparator, ElementState.Normal);
            var popupAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuPopup, ElementState.Normal);

            _rowHeight = SkinPainter.InflateByPadding(
                SkinPainter.MeasureMnemonicText(g, "Xg", itemAppearance, dpi), itemAppearance, dpi).Height;
            _separatorHeight = SkinPainter.InflateByPadding(Size.Empty, separatorAppearance, dpi).Height;
            _gap = _rowHeight / 2;

            int maxTextWidth = 0;
            int maxShortcutWidth = 0;
            foreach (var entry in _entries)
            {
                int textWidth = SkinPainter.MeasureMnemonicText(g, entry.Text, itemAppearance, dpi).Width;
                if (textWidth > maxTextWidth) maxTextWidth = textWidth;

                if (entry.Shortcut != Keys.None)
                {
                    int shortcutWidth = SkinPainter.MeasureText(
                        g, ShortcutDisplay.Format(entry.Shortcut), itemAppearance, dpi).Width;
                    if (shortcutWidth > maxShortcutWidth) maxShortcutWidth = shortcutWidth;
                }
            }
            _textWidth = maxTextWidth;
            _shortcutWidth = maxShortcutWidth;

            // Gutter + Text + Lücke + Shortcut + Pfeilzone — Rahmen/Einzug kommt
            // NICHT hier dazu, sondern über InflateByPadding (Umkehrung von
            // GetContentRectangle): dieselbe Padding+Rahmen-Arithmetik, ohne
            // dass diese Assembly selbst DpiScale anfassen müsste.
            int contentWidth = _rowHeight + _textWidth + _gap + _shortcutWidth + _rowHeight;
            int contentHeight = 0;
            foreach (var entry in _entries)
                contentHeight += entry.IsSeparator ? _separatorHeight : _rowHeight;

            var outer = SkinPainter.InflateByPadding(new Size(contentWidth, contentHeight), popupAppearance, dpi);
            var size = new Size(Math.Max(anchorWidth, outer.Width), outer.Height);

            var content = SkinPainter.GetContentRectangle(new Rectangle(Point.Empty, size), popupAppearance, dpi);
            _rows = new Rectangle[_entries.Count];
            int y = content.Top;
            for (int i = 0; i < _entries.Count; i++)
            {
                int height = _entries[i].IsSeparator ? _separatorHeight : _rowHeight;
                _rows[i] = new Rectangle(content.Left, y, content.Width, height);
                y += height;
            }
            _measured = true;

            return size;
        }

        public void Paint(Graphics g, Rectangle bounds, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            var popupAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuPopup, ElementState.Normal);
            SkinPainter.DrawBackground(g, bounds, popupAppearance, dpi);
            SkinPainter.DrawBorder(g, bounds, popupAppearance, dpi);

            var separatorAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuSeparator, ElementState.Normal);

            for (int i = 0; i < _rows.Length; i++)
            {
                var entry = _entries[i];
                var row = _rows[i];

                if (entry.IsSeparator)
                {
                    SkinPainter.DrawSeparatorLine(g, row, separatorAppearance, dpi);
                    continue;
                }

                ElementState state;
                if (!entry.IsSelectable) state = ElementState.Disabled;
                else if (i == _hoverIndex || i == _selectedIndex) state = ElementState.Hovered;
                else state = ElementState.Normal;

                var rowAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuItem, state);
                SkinPainter.DrawBackground(g, row, rowAppearance, dpi);

                var gutter = GutterZone(row);
                var text = TextZone(row);
                var shortcut = ShortcutZone(row);
                var arrow = ArrowZone(row);

                if (entry.Checked) DrawCheck(g, gutter, rowAppearance);

                SkinPainter.DrawMnemonicText(g, entry.Text, text, rowAppearance, dpi, ContentAlignment.MiddleLeft);

                if (entry.Shortcut != Keys.None)
                {
                    SkinPainter.DrawText(g, ShortcutDisplay.Format(entry.Shortcut), shortcut, rowAppearance, dpi,
                        ContentAlignment.MiddleRight);
                }

                if (entry.HasChildren) DrawSubmenuArrow(g, arrow, rowAppearance);
            }
        }

        // ---- Spaltengeometrie einer Zeile (eine Quelle für Paint UND Tests) --
        //
        // Strikt sequenziell von links, wie im Plan festgelegt: Gutter, Text,
        // Lücke, Shortcut-Spalte, Pfeilzone — jede Spalte beginnt exakt dort,
        // wo die vorige endet. Bewusst NICHT an den rechten Zeilenrand
        // gepinnt: Bei anchorWidth > Eigenbreite (Popup breiter als der
        // Inhalt braucht) bleibt die Lücke dadurch dort, wo die sequenzielle
        // Formel sie vorsieht (vor der Pfeilzone), statt vor den an den Rand
        // gepinnten Pfeil zu wandern. Divergenz-Test:
        // The_submenu_arrow_stays_sequential_after_the_shortcut_column_when_anchorWidth_widens_the_popup.

        private Rectangle GutterZone(Rectangle row)
        {
            return new Rectangle(row.Left, row.Top, _rowHeight, row.Height);
        }

        private Rectangle TextZone(Rectangle row)
        {
            return new Rectangle(GutterZone(row).Right, row.Top, _textWidth, row.Height);
        }

        private Rectangle ShortcutZone(Rectangle row)
        {
            return new Rectangle(TextZone(row).Right + _gap, row.Top, _shortcutWidth, row.Height);
        }

        private Rectangle ArrowZone(Rectangle row)
        {
            return new Rectangle(ShortcutZone(row).Right, row.Top, _rowHeight, row.Height);
        }

        /// <summary>Haken als Polylinie im inneren Drittel der (quadratischen)
        /// Gutter-Box — Technik von CheckEdit.PaintContent.</summary>
        private static void DrawCheck(Graphics g, Rectangle box, ElementAppearance appearance)
        {
            int side = box.Height;
            var pen = ResourceCache.Shared.GetPen(appearance.ForeColor, Math.Max(2, side / 8));
            int x0 = box.Left + side / 4;
            int y0 = box.Top + side / 2;
            int x1 = box.Left + side * 2 / 5;
            int y1 = box.Top + side * 7 / 10;
            int x2 = box.Left + side * 3 / 4;
            int y2 = box.Top + side * 3 / 10;
            g.DrawLines(pen, new[] { new Point(x0, y0), new Point(x1, y1), new Point(x2, y2) });
        }

        /// <summary>Untermenü-Pfeil: gefülltes Dreieck nach rechts, Kantenlänge
        /// ≈ Zeilenhöhe/3 — Technik von ButtonEditBase.DrawGlyph.</summary>
        private static void DrawSubmenuArrow(Graphics g, Rectangle zone, ElementAppearance appearance)
        {
            int size = zone.Height / 3;
            if (size < 2) return;

            int cx = zone.Left + zone.Width / 2;
            int cy = zone.Top + zone.Height / 2;
            var brush = ResourceCache.Shared.GetBrush(appearance.ForeColor);

            g.FillPolygon(brush, new[]
            {
                new Point(cx - size / 2, cy - size),
                new Point(cx - size / 2, cy + size),
                new Point(cx + size / 2, cy)
            });
        }

        private int HitTestRow(Point location)
        {
            for (int i = 0; i < _rows.Length; i++)
                if (_rows[i].Contains(location)) return i;
            return -1;
        }

        public void HandleMouseMove(Point location)
        {
            int index = HitTestRow(location);
            if (index >= 0 && !_entries[index].IsSelectable) index = -1;

            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                RaiseVisualChanged();
                RaiseHoveredIndexChanged(index);
            }
        }

        public void HandleMouseClick(Point location)
        {
            int index = HitTestRow(location);
            if (index < 0) return;

            var entry = _entries[index];
            if (!entry.IsSelectable) return;   // Separator/Disabled/daneben: nichts

            var handler = EntryClicked;
            if (handler != null) handler(entry);
        }

        /// <summary>Tasten laufen im Menü-Modus über den Controller, nie über
        /// den Host — dieser Gast lehnt jede Taste ab.</summary>
        public bool HandleKey(Keys key)
        {
            return false;
        }

        private void RaiseVisualChanged()
        {
            var handler = VisualChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void RaiseHoveredIndexChanged(int index)
        {
            var handler = HoveredIndexChanged;
            if (handler != null) handler(index);
        }

        // ---- Nur für Tests --------------------------------------------------

        /// <summary>Pfeilzonen-Geometrie einer Zeile — die Spaltenrechtecke selbst
        /// sind sonst nicht öffentlich beobachtbar. Muster: CalendarContent.NextArrowForTests.</summary>
        internal Rectangle SubmenuArrowBoundsForTests(int index)
        {
            return ArrowZone(RowBounds(index));
        }
    }
}
