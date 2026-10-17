using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>Ergebnis eines Treffertests im Ribbon.</summary>
    internal enum RibbonHitKind
    {
        None,
        TabHeader,
        Item
    }

    /// <summary>
    /// Ein geometrischer Treffer im Ribbon — TabHeader trägt den Tab-Index,
    /// Item das getroffene Modellobjekt. Rein deskriptiv: ob ein Treffer
    /// tatsächlich etwas auslöst (Enabled-Prüfung, Klick-Ausführung), liegt
    /// beim Aufrufer (Muster MenuBar.HitTest/IsBarItemSelectable) — dieselbe
    /// Trennung, die Task 6 für die Item-Ausführung braucht.
    /// </summary>
    internal struct RibbonHitInfo
    {
        public RibbonHitKind Kind;
        public int TabIndex;
        public RibbonItem Item;
    }

    /// <summary>
    /// Das Ribbon-Control: Tab-Köpfe oben, darunter die Gruppen des gewählten
    /// Tabs. Elementweise gemalt wie <see cref="MenuBar"/> (und GridControl im
    /// Grid-Teilprojekt) — Tabs/Gruppen/Knöpfe sind keine Child-Controls, sondern Rechtecke, die
    /// <see cref="RibbonLayout"/> berechnet und diese Klasse malt und trifft.
    ///
    /// Nimmt NIE den Fokus (dieselbe Kerninvariante wie <see cref="MenuBar"/>)
    /// und verwaltet ihre Höhe selbst (siehe <see cref="ApplyPreferredHeight"/>).
    /// Item-Ausführung (Task 6): Down markiert Pressed, Up über demselben
    /// Item führt aus (siehe <see cref="Execute"/>) — Up woanders bricht ab.
    /// </summary>
    public class RibbonControl : SkinnedControl
    {
        private readonly List<RibbonTab> _tabs = new List<RibbonTab>();
        private int _selectedTabIndex = -1;

        private Rectangle[] _tabHeaderBounds = new Rectangle[0];
        private RibbonPlacedItem[] _placedItems = new RibbonPlacedItem[0];

        private int _hoverTabIndex = -1;
        private RibbonItem _hoverItem;
        private RibbonItem _pressedItem;

        public RibbonControl()
        {
            // Kerninvariante wie MenuBar: das Ribbon nimmt nie den Fokus —
            // Dropdown-Popups (Task 6) sind ohnehin nicht-aktivierend.
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }

        /// <summary>Die Tabs des Ribbons, von links nach rechts.</summary>
        public IList<RibbonTab> Tabs
        {
            get { return _tabs; }
        }

        /// <summary>
        /// Der gewählte Tab-Index — geklemmt auf [0, Tabs.Count-1], −1 wenn
        /// Tabs leer ist. Die Klemmung passiert beim LESEN (nicht nur beim
        /// Schreiben): Tabs ist eine rohe Liste ohne Änderungs-Hook, das
        /// Control kann also nicht auf Tabs.Add reagieren. Dieselbe Klemmung
        /// bei jedem Zugriff sorgt dafür, dass ein frisch befülltes Ribbon
        /// ohne expliziten Setter-Aufruf automatisch Tab 0 zeigt (−1 klemmt
        /// auf 0, sobald Tabs.Count &gt; 0 ist) UND dass ein Tab, der durch
        /// Entfernen ungültig wird, beim nächsten Zugriff sofort wieder in
        /// den gültigen Bereich fällt.
        /// </summary>
        public int SelectedTabIndex
        {
            get
            {
                if (_tabs.Count == 0) return -1;
                int clamped = _selectedTabIndex;
                if (clamped < 0) clamped = 0;
                else if (clamped > _tabs.Count - 1) clamped = _tabs.Count - 1;
                return clamped;
            }
            set
            {
                int before = SelectedTabIndex;
                _selectedTabIndex = value;
                if (SelectedTabIndex != before)
                {
                    // Stale Hover nach Tab-Wechsel vermeiden (Mitnahme-Fund
                    // Abschluss-Review): ein Hover-Item/-Tab aus der alten
                    // Auswahl bleibt sonst bis zur nächsten Mausbewegung
                    // optisch "hängen", obwohl der Tab-Wechsel selbst schon
                    // von woanders (z. B. Tastatur/App-Code) ausgelöst sein
                    // kann, ohne dass die Maus sich je bewegt hätte.
                    _hoverTabIndex = -1;
                    _hoverItem = null;
                    Invalidate();
                }
            }
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Ribbon; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        // ---- Maße -------------------------------------------------------
        //
        // Kein DpiScale in dieser Assembly: jede Zahl kommt aus einer echten
        // SkinPainter-Messung desselben Skins/DPI. Die GRUNDGRÖSSE ist die
        // aufgeblasene Zeilenhöhe eines Musterknopfs ("Xg" deckt Ober- UND
        // Unterlänge ab, deshalb repräsentativ für JEDEN Text derselben
        // Schrift — TextRenderer misst die Höhe fontbasiert, nicht
        // glyphenformabhängig). Davon leitet sich alles ab:
        //   - Bildkante groß = 2× rohe Zeilenhöhe, klein = 1× (Quadrat).
        //   - contentHeight = 3× aufgeblasene Zeilenhöhe — genau drei kleine
        //     Boxen passen exakt übereinander (RibbonLayout.ArrangeGroupItems
        //     teilt contentHeight/3 als Zeilenhöhe; die Division geht ohne
        //     Rest auf, weil contentHeight selbst als 3× dieser Zeilenhöhe
        //     definiert ist).
        //   - columnGap = rohe Zeilenhöhe / 2. Für die Lücke ZWISCHEN Gruppen
        //     gibt es im Brief keinen eigenen Wert — hier bewusst derselbe
        //     columnGap wiederverwendet statt einer erfundenen zweiten Zahl.
        // Breiten sind NIE generisch: Tab-Köpfe und Item-Boxen messen ihren
        // eigenen Text (das Layout selbst bleibt aber lückenlos/uniform,
        // siehe RibbonLayout). Höhen sind dagegen immer generisch (aus "Xg"),
        // damit Tab-Wechsel oder unterschiedlich lange Texte die Höhe nie
        // verändern — dieselbe Stabilität, die MenuBar mit "Xg" für die
        // Item-Messung erkämpft hat.
        // Image == null lässt die Bildzone eines Items entfallen (reiner
        // Textknopf), leerer/null Text die Textzeile (reiner Bildknopf) —
        // bindend laut Design-Spec, Fix-Runde 1 (siehe BuildBox/PaintItem).
        private struct RibbonMetrics
        {
            public ElementAppearance TabAppearance;
            public ElementAppearance ButtonAppearance;
            public ElementAppearance GroupAppearance;
            public ElementAppearance SeparatorAppearance;

            public int TextLineHeight;   // roh: MeasureText("Xg").Height
            public int ContentHeight;    // 3 × aufgeblasene Zeilenhöhe
            public int ColumnGap;        // auch als Gruppen-Lücke wiederverwendet
            public int LargeImageEdge;   // 2 × TextLineHeight
            public int SmallImageEdge;   // 1 × TextLineHeight
            public int TabRowHeight;
            public int TitleHeight;
            public int GroupPadLeft;
            public int GroupPadTop;
            public int GroupPadRight;
            public int GroupPadBottom;
        }

        private RibbonMetrics ComputeMetrics(Graphics g)
        {
            int dpi = DeviceDpi;

            var tabAppearance = SkinManager.Current.GetAppearance(ElementKeys.RibbonTabHeader, ElementState.Normal);
            var buttonAppearance = SkinManager.Current.GetAppearance(ElementKeys.RibbonButton, ElementState.Normal);
            var groupAppearance = SkinManager.Current.GetAppearance(ElementKeys.RibbonGroup, ElementState.Normal);
            var separatorAppearance = SkinManager.Current.GetAppearance(ElementKeys.RibbonSeparator, ElementState.Normal);

            var xg = SkinPainter.MeasureText(g, "Xg", buttonAppearance, dpi);   // roh, unaufgeblasen
            int textLineHeight = xg.Height;
            int lineHeight = SkinPainter.InflateByPadding(xg, buttonAppearance, dpi).Height;   // GRUNDGRÖSSE

            int contentHeight = 3 * lineHeight;
            int columnGap = textLineHeight / 2;

            int tabRowHeight = SkinPainter.InflateByPadding(
                SkinPainter.MeasureText(g, "Xg", tabAppearance, dpi), tabAppearance, dpi).Height;

            int titleHeight = SkinPainter.InflateByPadding(
                SkinPainter.MeasureText(g, "Xg", groupAppearance, dpi), groupAppearance, dpi).Height;

            // Einzelne Padding-Seiten ohne DpiScale: InflateByPadding und
            // GetContentRectangle sind zueinander invers (Muster
            // MenuContent.Measure) — eine Platzhaltergröße genügt, das
            // Padding selbst hängt nicht von der Innengröße ab.
            const int probe = 100000;
            var probeContent = SkinPainter.GetContentRectangle(
                new Rectangle(0, 0, probe, probe), groupAppearance, dpi);

            return new RibbonMetrics
            {
                TabAppearance = tabAppearance,
                ButtonAppearance = buttonAppearance,
                GroupAppearance = groupAppearance,
                SeparatorAppearance = separatorAppearance,
                TextLineHeight = textLineHeight,
                ContentHeight = contentHeight,
                ColumnGap = columnGap,
                LargeImageEdge = 2 * textLineHeight,
                SmallImageEdge = textLineHeight,
                TabRowHeight = tabRowHeight,
                TitleHeight = titleHeight,
                GroupPadLeft = probeContent.Left,
                GroupPadTop = probeContent.Top,
                GroupPadRight = probe - probeContent.Right,
                GroupPadBottom = probe - probeContent.Bottom
            };
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var metrics = ComputeMetrics(g);
                var ribbonAppearance = CurrentAppearance;

                int ribbonPadding = SkinPainter.InflateByPadding(Size.Empty, ribbonAppearance, DeviceDpi).Height;
                int groupPadding = metrics.GroupPadTop + metrics.GroupPadBottom;

                int height = ribbonPadding + metrics.TabRowHeight + metrics.ContentHeight
                    + metrics.TitleHeight + groupPadding;

                // Breite: Elternvorgabe (Muster MenuBar) — das Ribbon ist so
                // breit wie sein Host, nicht wie sein eigener Inhalt.
                return new Size(proposedSize.Width, height);
            }
        }

        // ---- Höhe intrinsisch (EXAKT das MenuBar-Muster) -----------------
        //
        // Ohne eigene Höhenverwaltung bliebe Height auf Control.DefaultSize
        // stehen und Dock=Top zeigte einen 0 px hohen Streifen (F1-Befund
        // aus 4a, an MenuBar vermessen). Die drei Aufrufer unten decken die
        // drei Momente ab, an denen sich der Höhenbedarf ändern kann:
        // Handle-Erzeugung (erster verlässlicher Zeitpunkt mit echtem
        // DeviceDpi), DPI-Wechsel zur Laufzeit und jeder Layout-Durchlauf.

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyPreferredHeight();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            ApplyPreferredHeight();
            base.OnDpiChangedAfterParent(e);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // Selbstheilende Bremse gegen Doppel-Skalierung: ein Scale(factor)
            // auf allen Kindern (z. B. MainForm.OnLoad) würde eine bereits
            // bei echtem DeviceDpi korrekt gesetzte Height ein zweites Mal
            // multiplizieren. Control.Scale(SizeF) löst intern über
            // Suspend-/ResumeLayout genau einen OnLayout-Durchlauf auf dem
            // skalierten Control aus — diese Bremse reicht deshalb aus, ohne
            // eigenen Scale()/ScaleControl()-Override. Der Gleichheitstest in
            // ApplyPreferredHeight ist zugleich die Konvergenzbedingung: eine
            // zweite, durch diese Zuweisung selbst ausgelöste Runde trifft
            // den Zielwert bereits und bleibt aus — keine Endlosschleife.
            ApplyPreferredHeight();
        }

        /// <summary>
        /// Setzt NUR die Höhe neu (nie Size als Ganzes — die Breite bleibt
        /// Elternvorgabe) und nur, wenn sie vom aktuellen Bedarf abweicht.
        /// Diese Bedingung ist die Bremse gegen Endlosschleifen bei den
        /// Aufrufern und macht wiederholte Aufrufe no-op-sicher.
        /// </summary>
        private void ApplyPreferredHeight()
        {
            int preferred = GetPreferredSize(Size.Empty).Height;
            if (Height != preferred) Height = preferred;
        }

        // ---- Malen --------------------------------------------------------

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            int dpi = DeviceDpi;
            var metrics = ComputeMetrics(g);
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, dpi);

            // ---- Tab-Köpfe ----
            var headerSizes = new Size[_tabs.Count];
            for (int i = 0; i < _tabs.Count; i++)
            {
                headerSizes[i] = SkinPainter.InflateByPadding(
                    SkinPainter.MeasureText(g, _tabs[i].Text, metrics.TabAppearance, dpi),
                    metrics.TabAppearance, dpi);
            }

            _tabHeaderBounds = RibbonLayout.LayoutTabHeaders(headerSizes, content.Location);

            int selectedIndex = SelectedTabIndex;

            for (int i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];

                ElementState state;
                if (!tab.Enabled) state = ElementState.Disabled;
                else if (i == selectedIndex) state = ElementState.Selected;
                else if (i == _hoverTabIndex) state = ElementState.Hovered;
                else state = ElementState.Normal;

                var headerAppearance = SkinManager.Current.GetAppearance(ElementKeys.RibbonTabHeader, state);
                var bounds = _tabHeaderBounds[i];

                SkinPainter.DrawBackground(g, bounds, headerAppearance, dpi);
                SkinPainter.DrawBorder(g, bounds, headerAppearance, dpi);
                SkinPainter.DrawText(g, tab.Text, bounds, headerAppearance, dpi, ContentAlignment.MiddleCenter);
            }

            // ---- Gruppen des gewählten Tabs ----
            var placed = new List<RibbonPlacedItem>();

            if (selectedIndex >= 0 && selectedIndex < _tabs.Count)
            {
                // Die tatsächlich gemalte Kopfzeilenhöhe (alle Köpfe teilen
                // sich per LayoutTabHeaders dieselbe Höhe) statt der
                // generischen metrics.TabRowHeight — beide sind bei nicht-
                // leerem Tabtext identisch (Schriftmetrik, s. o.), aber so
                // kann nie eine Lücke/Überlappung zwischen Kopfzeile und
                // Gruppenfläche entstehen.
                int tabRowHeight = _tabHeaderBounds.Length > 0 ? _tabHeaderBounds[0].Height : metrics.TabRowHeight;
                var groupOrigin = new Point(content.Left, content.Top + tabRowHeight);

                var groups = _tabs[selectedIndex].Groups;
                var contentSizes = new List<Size>(groups.Count);
                var arrangedPerGroup = new List<RibbonPlacedItem[]>(groups.Count);

                for (int i = 0; i < groups.Count; i++)
                {
                    var items = groups[i].Items;
                    var boxes = new List<RibbonBox>(items.Count);
                    for (int j = 0; j < items.Count; j++)
                        boxes.Add(BuildBox(g, items[j], dpi, metrics));

                    var arranged = RibbonLayout.ArrangeGroupItems(boxes, metrics.ContentHeight, metrics.ColumnGap);

                    int width = 0;
                    for (int j = 0; j < arranged.Length; j++)
                        if (arranged[j].Bounds.Right > width) width = arranged[j].Bounds.Right;

                    contentSizes.Add(new Size(width, metrics.ContentHeight));
                    arrangedPerGroup.Add(arranged);
                }

                var panes = RibbonLayout.LayoutGroups(
                    contentSizes, arrangedPerGroup, groupOrigin, metrics.TitleHeight, metrics.ColumnGap,
                    metrics.GroupPadLeft, metrics.GroupPadTop, metrics.GroupPadRight, metrics.GroupPadBottom);

                for (int i = 0; i < panes.Length; i++)
                {
                    var pane = panes[i];

                    SkinPainter.DrawBackground(g, pane.Frame, metrics.GroupAppearance, dpi);
                    SkinPainter.DrawBorder(g, pane.Frame, metrics.GroupAppearance, dpi);
                    SkinPainter.DrawPaddedText(g, groups[i].Title, pane.TitleRow, metrics.GroupAppearance, dpi,
                        ContentAlignment.MiddleCenter);

                    for (int j = 0; j < pane.Items.Length; j++)
                    {
                        PaintItem(g, pane.Items[j], metrics, dpi);
                        placed.Add(pane.Items[j]);
                    }
                }
            }

            // Clipping: was rechts über die Client-Breite hinausragt, malt
            // GDI schlicht nicht mehr (Rechtecke außerhalb der Zeichenfläche
            // sind harmlos) — der Hit-Test prüft zusätzlich selbst gegen
            // ClientRectangle (siehe HitTest), Zusammenklappen ist 4b2.
            _placedItems = placed.ToArray();
        }

        private static RibbonBox BuildBox(Graphics g, RibbonItem item, int dpi, RibbonMetrics metrics)
        {
            if (item.Kind == RibbonItemKind.Separator)
            {
                int separatorWidth = SkinPainter.InflateByPadding(Size.Empty, metrics.SeparatorAppearance, dpi).Width;
                return new RibbonBox { Item = item, Size = new Size(separatorWidth, metrics.ContentHeight) };
            }

            // Image == null lässt die Bildzone entfallen (reiner Textknopf),
            // leerer/null Text lässt die Textzeile entfallen (reiner
            // Bildknopf) — Fix-Runde 1: die Spec (Modell-Abschnitt) verlangt
            // das bindend, nicht nur als Malpfad-Kosmetik, sondern schon hier
            // in der Messung, sonst reserviert die Box unbenutzten Platz UND
            // der Hit-Test träfe daneben vom Gemalten. hasImage/hasText
            // steuern deshalb Boxmessung UND Malpfad (PaintItem) konsistent
            // aus demselben Paar Bedingungen.
            bool hasImage = item.Image != null;
            bool hasText = !string.IsNullOrEmpty(item.Text);
            var textSize = SkinPainter.MeasureText(g, item.Text, metrics.ButtonAppearance, dpi);   // leer -> Size.Empty
            Size contentSize;

            if (item.Size == RibbonItemSize.Large)
            {
                // Bild (2×) über einer Textzeile — Breite = das breitere der
                // beiden, Höhe nur für InflateByPadding relevant (die
                // tatsächliche Platzierungshöhe bestimmt RibbonLayout selbst
                // über contentHeight, nicht diese Box). Ohne Bild trägt die
                // Bildkante nichts zu Breite/Höhe bei, ohne Text die Textzeile
                // nicht — fehlen beide, bleibt eine minimale (nur gepolsterte)
                // Box übrig, kein Crash.
                int edge = hasImage ? metrics.LargeImageEdge : 0;
                int textRow = hasText ? metrics.TextLineHeight : 0;
                contentSize = new Size(Math.Max(edge, textSize.Width), edge + textRow);
            }
            else
            {
                // Bild (1×) neben dem Text, kein zusätzlicher innerer Abstand
                // (der Brief nennt keinen — die Erscheinung liefert das
                // Padding ringsum ohnehin über InflateByPadding). Ohne Bild
                // beginnt der Text bei 0 (kein reservierter Bildanteil), ohne
                // Text bleibt nur die quadratische Bildkante übrig.
                int edge = hasImage ? metrics.SmallImageEdge : 0;
                int textRow = hasText ? metrics.TextLineHeight : 0;
                contentSize = new Size(edge + textSize.Width, Math.Max(edge, textRow));
            }

            var size = SkinPainter.InflateByPadding(contentSize, metrics.ButtonAppearance, dpi);
            return new RibbonBox { Item = item, Size = size };
        }

        private void PaintItem(Graphics g, RibbonPlacedItem placedItem, RibbonMetrics metrics, int dpi)
        {
            var item = placedItem.Item;
            var bounds = placedItem.Bounds;

            if (item.Kind == RibbonItemKind.Separator)
            {
                SkinPainter.DrawVerticalSeparatorLine(g, bounds, metrics.SeparatorAppearance, dpi);
                return;
            }

            var state = ItemState(item);
            var itemAppearance = SkinManager.Current.GetAppearance(ElementKeys.RibbonButton, state);

            SkinPainter.DrawBackground(g, bounds, itemAppearance, dpi);
            SkinPainter.DrawBorder(g, bounds, itemAppearance, dpi);

            var inner = InnerZone(bounds, metrics, dpi);

            // Dieselben hasImage/hasText wie in BuildBox — die Box wurde
            // bereits ohne die fehlende Zone gemessen, der Malpfad muss ihr
            // exakt folgen (sonst träfe der Hit-Test daneben vom Gemalten).
            bool hasImage = item.Image != null;
            bool hasText = !string.IsNullOrEmpty(item.Text);

            if (item.Size == RibbonItemSize.Large)
            {
                int edge = metrics.LargeImageEdge;

                if (hasImage && hasText)
                {
                    var imageZone = new Rectangle(inner.Left + (inner.Width - edge) / 2, inner.Top, edge, edge);
                    var textZone = Rectangle.FromLTRB(inner.Left, imageZone.Bottom, inner.Right, inner.Bottom);

                    SkinPainter.DrawScaledImage(g, item.Image, imageZone, item.Enabled);
                    SkinPainter.DrawText(g, item.Text, textZone, itemAppearance, dpi, ContentAlignment.BottomCenter);
                }
                else if (hasImage)
                {
                    // Reiner Bildknopf: die Bildkante zentriert in der vollen
                    // Innenfläche (keine Textzeile reserviert ihr Platz).
                    var imageZone = new Rectangle(
                        inner.Left + (inner.Width - edge) / 2, inner.Top + (inner.Height - edge) / 2, edge, edge);
                    SkinPainter.DrawScaledImage(g, item.Image, imageZone, item.Enabled);
                }
                else if (hasText)
                {
                    // Reiner Textknopf: Text zentriert über die volle
                    // Innenfläche (keine Bildkante zieht ihn nach unten).
                    SkinPainter.DrawText(g, item.Text, inner, itemAppearance, dpi, ContentAlignment.MiddleCenter);
                }
                // Weder Bild noch Text: nichts zu malen — Fläche/Rahmen stehen bereits.
            }
            else
            {
                int edge = metrics.SmallImageEdge;

                if (hasImage && hasText)
                {
                    var imageZone = new Rectangle(inner.Left, inner.Top + (inner.Height - edge) / 2, edge, edge);
                    var textZone = Rectangle.FromLTRB(imageZone.Right, inner.Top, inner.Right, inner.Bottom);

                    SkinPainter.DrawScaledImage(g, item.Image, imageZone, item.Enabled);
                    SkinPainter.DrawText(g, item.Text, textZone, itemAppearance, dpi, ContentAlignment.MiddleLeft);
                }
                else if (hasImage)
                {
                    var imageZone = new Rectangle(inner.Left, inner.Top + (inner.Height - edge) / 2, edge, edge);
                    SkinPainter.DrawScaledImage(g, item.Image, imageZone, item.Enabled);
                }
                else if (hasText)
                {
                    // Kein Bildeinzug: der Text beginnt bei inner.Left, wie in
                    // BuildBox gemessen (contentWidth = reine Textbreite).
                    SkinPainter.DrawText(g, item.Text, inner, itemAppearance, dpi, ContentAlignment.MiddleLeft);
                }
            }
        }

        /// <summary>
        /// Innenzone eines gemalten Items — IMMER aus der Normal-Button-
        /// Erscheinung der Metrik abgeleitet (Muster MenuBar.PaintContent,
        /// "Messen IMMER mit Normal"), NIE aus der Zustands-Erscheinung des
        /// Items: Hovered/Pressed/Selected tragen in der Skin-Tabelle eine
        /// BorderWidth von 1, Normal/Disabled 0. Käme inner aus itemAppearance
        /// (Abschluss-Review-Fund), säße der Inhalt eines gecheckten
        /// ToggleButtons dauerhaft um die Rahmenbreite versetzt, und bei
        /// Hover/Press zappelte er zusätzlich bei jedem Zustandswechsel.
        /// Hintergrund/Rahmen/Textfarben bleiben bewusst zustandsabhängig
        /// (itemAppearance in PaintItem) — nur die GEOMETRIE ist fix.
        /// </summary>
        private static Rectangle InnerZone(Rectangle bounds, RibbonMetrics metrics, int dpi)
        {
            return SkinPainter.GetContentRectangle(bounds, metrics.ButtonAppearance, dpi);
        }

        /// <summary>
        /// Rangfolge wie überall im Framework (Disabled &gt; Pressed &gt;
        /// Hovered &gt; Selected &gt; Normal). Ein Separator ist nie
        /// "disabled" im Sinn dieser Rangfolge (er kennt in der Skin-Tabelle
        /// ohnehin nur Normal), deshalb die explizite Ausnahme.
        /// </summary>
        private ElementState ItemState(RibbonItem item)
        {
            if (item.Kind != RibbonItemKind.Separator && !item.IsInteractive) return ElementState.Disabled;
            if (ReferenceEquals(item, _pressedItem)) return ElementState.Pressed;
            if (ReferenceEquals(item, _hoverItem)) return ElementState.Hovered;
            if (item.Checked) return ElementState.Selected;
            return ElementState.Normal;
        }

        // ---- Maus ---------------------------------------------------------

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var hit = HitTest(e.Location);
            int hoverTab = hit.Kind == RibbonHitKind.TabHeader ? hit.TabIndex : -1;
            RibbonItem hoverItem = hit.Kind == RibbonHitKind.Item ? hit.Item : null;

            if (hoverTab != _hoverTabIndex || !ReferenceEquals(hoverItem, _hoverItem))
            {
                _hoverTabIndex = hoverTab;
                _hoverItem = hoverItem;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var hit = HitTest(e.Location);
                // Schranke gegen geschrumpftes Modell (Mitnahme-Fund
                // Abschluss-Review): _tabHeaderBounds stammt vom letzten
                // Repaint — hat die App danach Tabs entfernt, trifft ein
                // Klick VOR dem nächsten Repaint einen Index, den _tabs nicht
                // mehr kennt. Ohne die Schranke würfe _tabs[hit.TabIndex]
                // eine IndexOutOfRangeException.
                if (hit.Kind == RibbonHitKind.TabHeader && hit.TabIndex < _tabs.Count
                    && _tabs[hit.TabIndex].Enabled)
                {
                    SelectedTabIndex = hit.TabIndex;
                }
                else if (hit.Kind == RibbonHitKind.Item && hit.Item.IsInteractive)
                {
                    // Down markiert nur "gedrückt" — die Ausführung passiert
                    // erst bei MouseUp über DEMSELBEN Item (OnMouseUp), damit
                    // ein Herausziehen vor dem Loslassen den Klick abbricht.
                    _pressedItem = hit.Item;
                    Invalidate();
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _pressedItem != null)
            {
                var pressedItem = _pressedItem;
                _pressedItem = null;
                Invalidate();

                // Nur ausführen, wenn die Maus über DEMSELBEN Item losgelassen
                // wird — woanders (oder daneben) bricht den Klick ab, ohne
                // etwas auszulösen (SkinnedControl-Muster: Verlassen räumt).
                var hit = HitTest(e.Location);
                // Klick-Zeit-Recheck: Enabled kann sich zwischen Down und Up
                // ändern (App-Timer/Async-Callback — das Ribbon ist EIN
                // Control, kein Sub-Control pro Item), darum hier erneut
                // prüfen statt dem Zustand von Down blind zu vertrauen —
                // dieselbe Konvention wie MenuContent.HandleMouseClick
                // ("if (!entry.IsSelectable) return; // Separator/Disabled/daneben: nichts").
                if (hit.Kind == RibbonHitKind.Item && ReferenceEquals(hit.Item, pressedItem)
                    && pressedItem.IsInteractive)
                    Execute(pressedItem);
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            // F2-Lehre aus 4a (MenuBar): OnMouseLeave räumt ALLES auf —
            // Tab-Hover, Item-Hover UND einen laufenden Press — sonst bliebe
            // ein Element für immer hervorgehoben bzw. "gedrückt", obwohl die
            // Maus das Ribbon längst verlassen hat.
            if (_hoverTabIndex != -1 || _hoverItem != null || _pressedItem != null)
            {
                _hoverTabIndex = -1;
                _hoverItem = null;
                _pressedItem = null;
                Invalidate();
            }

            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Führt EIN Item aus — Mapping exakt nach Art:
        ///  - Button: PerformClick.
        ///  - ToggleButton: ERST Checked umschalten, DANN PerformClick (4a-
        ///    Politik — vgl. MenuController.ExecuteEntry mit CheckOnClick VOR
        ///    dem Click-Ereignis: der Handler sieht so bereits den neuen
        ///    Zustand).
        ///  - DropDownButton: öffnet sein Menü unterhalb der Item-Bounds
        ///    (Bildschirmkoordinaten); ein fehlendes Menü (null) tut nichts.
        ///  - Separator/Disabled kommen hier nie an — OnMouseDown lässt nur
        ///    interaktive Items überhaupt zu _pressedItem werden.
        /// App-Ausnahmen aus Click-Handlern schlagen bewusst durch (keine
        /// Kapselung hier) — dieselbe Politik wie MenuController.ExecuteEntry.
        /// </summary>
        private void Execute(RibbonItem item)
        {
            switch (item.Kind)
            {
                case RibbonItemKind.Button:
                    item.PerformClick();
                    break;
                case RibbonItemKind.ToggleButton:
                    item.Checked = !item.Checked;
                    item.PerformClick();
                    break;
                case RibbonItemKind.DropDownButton:
                    if (item.Menu != null)
                        item.Menu.ShowBelow(this, RectangleToScreen(BoundsOf(item)));
                    break;
            }
        }

        /// <summary>Bounds des Items unter _placedItems — der Aufrufer
        /// garantiert per HitTest, dass das Item dort tatsächlich steht.</summary>
        private Rectangle BoundsOf(RibbonItem item)
        {
            for (int i = 0; i < _placedItems.Length; i++)
                if (ReferenceEquals(_placedItems[i].Item, item))
                    return _placedItems[i].Bounds;
            return Rectangle.Empty;
        }

        /// <summary>
        /// Rein geometrischer Treffertest (Muster MenuBar.HitTest): ob ein
        /// Treffer wirkt (Enabled-Prüfung, Klick-Ausführung), entscheidet der
        /// Aufrufer. Erst gegen ClientRectangle geprüft — was rechts über die
        /// Client-Breite hinausragt, wird nicht mehr gemalt (GDI clippt) und
        /// darf deshalb auch nicht mehr treffen.
        /// </summary>
        internal RibbonHitInfo HitTest(Point location)
        {
            if (!ClientRectangle.Contains(location))
                return new RibbonHitInfo { Kind = RibbonHitKind.None, TabIndex = -1, Item = null };

            for (int i = 0; i < _tabHeaderBounds.Length; i++)
            {
                if (_tabHeaderBounds[i].Contains(location))
                    return new RibbonHitInfo { Kind = RibbonHitKind.TabHeader, TabIndex = i, Item = null };
            }

            for (int i = 0; i < _placedItems.Length; i++)
            {
                if (_placedItems[i].Bounds.Contains(location))
                    return new RibbonHitInfo { Kind = RibbonHitKind.Item, TabIndex = -1, Item = _placedItems[i].Item };
            }

            return new RibbonHitInfo { Kind = RibbonHitKind.None, TabIndex = -1, Item = null };
        }

        // ---- Nur für Tests --------------------------------------------------

        internal Rectangle[] TabHeaderBoundsForTests()
        {
            return _tabHeaderBounds;
        }

        internal RibbonPlacedItem[] PlacedItemsForTests()
        {
            return _placedItems;
        }

        /// <summary>
        /// Regressionshaken für Fund 1 (Abschluss-Review): liefert dieselbe
        /// Innenzone, die PaintItem für <paramref name="item"/> tatsächlich
        /// zum Malen verwendet (ruft denselben InnerZone-Helfer) — NICHT eine
        /// separate Neuberechnung, sonst würde der Test eine künftige
        /// Regression in PaintItem nicht mehr bemerken.
        /// </summary>
        internal Rectangle InnerZoneForTests(RibbonItem item)
        {
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var metrics = ComputeMetrics(g);
                return InnerZone(BoundsOf(item), metrics, DeviceDpi);
            }
        }

        internal RibbonHitInfo HitTestForTests(Point location)
        {
            return HitTest(location);
        }

        internal void PerformMouseDownForTests(Point location)
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0));
        }

        internal void PerformMouseUpForTests(Point location)
        {
            OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0));
        }

        internal bool HasPressForTests
        {
            get { return _pressedItem != null; }
        }

        internal void PerformMouseMoveForTests(Point location)
        {
            OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, location.X, location.Y, 0));
        }

        internal void PerformMouseLeaveForTests()
        {
            OnMouseLeave(EventArgs.Empty);
        }

        internal bool HasHoverForTests
        {
            get { return _hoverTabIndex != -1 || _hoverItem != null; }
        }
    }
}
