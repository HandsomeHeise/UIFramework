using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Controls;
using UIFramework.Core.Dpi;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Tree
{
    /// <summary>
    /// Ein skin-gezeichneter Ordnerbaum mit Laden beim Aufklappen. Rechnet
    /// selbst nichts: TreeFlattener rollt die sichtbaren Zeilen aus, TreeLayout
    /// kennt die Geometrie — beide ohne Fenster prüfbar. Gezeichnet werden nur
    /// die Zeilen im Sichtfenster (Muster GridControl), die Zeilenhöhe folgt
    /// der Schrift des Skins.
    /// </summary>
    [ToolboxItem(true)]
    public class TreeControl : SkinnedControl
    {
        private readonly SkinScrollBar _vertical;
        private int _verticalOffset;
        private bool _syncing;

        private List<TreeRow> _rows = new List<TreeRow>();
        private bool _rowsDirty = true;

        private TreeNode _selected;
        private string _loadingText = "Wird geladen…";

        /// <summary>Zeile unter der Maus (-1: keine) und ob über ihrer Glyphe —
        /// gesetzt von der Maus-Verdrahtung (Task 12).</summary>
        private int _hotRow = -1;
        private bool _hotGlyph;

        private readonly TypeAheadBuffer _typeAhead = new TypeAheadBuffer();

        /// <summary>Millisekunden-Uhr für Tippen-zum-Springen — Tests setzen sie.</summary>
        internal Func<long> Clock = () => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

        /// <summary>Verweildauer eines Drop-Ziels, bevor der Baum es aufklappt.</summary>
        internal const int AutoExpandDelayMilliseconds = 1000;

        private readonly DragStartTracker _drag = new DragStartTracker();
        private readonly DragAutoScroller _autoScroller;
        private readonly Timer _expandTimer;
        private bool _expandPending;
        private TreeNode _dragNode;
        private TreeNode _dropHighlight;

        /// <summary>Aufgeschobene Auswahl bei AllowItemDrag — siehe
        /// TryDeferClickForDrag/CompleteDeferredClick.</summary>
        private TreeNode _deferredClickNode;

        /// <summary>Skin-/DPI-abhängiger Messwerte-Cache (Muster GalleryControl.EnsureMeasureCache).</summary>
        private ISkin _metricsSkin;
        private int _metricsDpi = -1;
        private int _rowHeight;
        private int _lineHeight;

        public TreeControl()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;

            // VOR Size: das Setzen von Size löst OnSizeChanged aus, und das
            // braucht Nodes und die Leiste.
            Nodes = new TreeNodeCollection(this);

            _vertical = new SkinScrollBar { Orientation = Orientation.Vertical, Visible = false };
            _vertical.Scroll += (s, e) => { if (!_syncing) VerticalOffset = _vertical.Value; };
            Controls.Add(_vertical);

            _autoScroller = new DragAutoScroller(direction => VerticalOffset += direction * RowHeight);
            _expandTimer = new Timer { Interval = AutoExpandDelayMilliseconds };
            _expandTimer.Tick += (s, e) => AutoExpandTick();

            Size = new Size(240, 300);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Tree; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        /// <summary>Die Leisten-Bounds setzt SyncScrollBar in physischen Pixeln —
        /// die Autoskalierung der Form darf sie nicht ein zweites Mal
        /// multiplizieren (Fehlerklasse der Grid-Leisten).</summary>
        protected override bool ScaleChildren
        {
            get { return false; }
        }

        [Category("Behavior")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TreeNodeCollection Nodes { get; }

        /// <summary>Text der Platzhalterzeile unter einem ladenden Knoten.</summary>
        [Category("Appearance")]
        [DefaultValue("Wird geladen…")]
        public string LoadingText
        {
            get { return _loadingText; }
            set
            {
                _loadingText = value ?? "";
                Invalidate();
            }
        }

        /// <summary>
        /// Die eine Auswahl des Baums (Explorer-Baum: keine Mehrfachauswahl).
        /// Setzen klappt die Vorfahren auf und scrollt den Knoten ins Bild.
        /// Ein Knoten aus einem anderen (oder keinem) Baum wirft.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TreeNode SelectedNode
        {
            get { return _selected; }
            set
            {
                if (value != null && !ReferenceEquals(value.Tree, this))
                    throw new ArgumentException("Der Knoten hängt nicht in diesem Baum.", nameof(value));

                if (value != null) ExpandAncestors(value);
                SetSelected(value);
                if (value != null) EnsureVisible(value);
            }
        }

        /// <summary>Ein aufklappbarer Knoten ohne Kinder wird aufgeklappt — die
        /// App lädt jetzt (BeginLoading, dann Children.ReplaceWith).</summary>
        public event EventHandler<TreeNodeEventArgs> NodeExpanding;

        public event EventHandler SelectedNodeChanged;

        /// <summary>Enter oder Doppelklick auf einen Knoten.</summary>
        public event EventHandler<TreeNodeEventArgs> NodeActivated;

        protected virtual void OnSelectedNodeChanged()
        {
            var handler = SelectedNodeChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnNodeActivated(TreeNode node)
        {
            var handler = NodeActivated;
            if (handler != null) handler(this, new TreeNodeEventArgs(node));
        }

        // ---- Zeilen und Maße ---------------------------------------------------

        internal SkinScrollBar VerticalScrollBar
        {
            get { return _vertical; }
        }

        /// <summary>Wie viele Zeilen der letzte Zeichendurchlauf gemalt hat — das
        /// Beweismittel der Virtualisierung (Muster CountingDataSource).</summary>
        internal int LastPaintedRowCountForTests { get; private set; }

        internal IList<TreeRow> Rows
        {
            get
            {
                EnsureRows();
                return _rows;
            }
        }

        private void EnsureRows()
        {
            if (!_rowsDirty) return;
            _rows = TreeFlattener.Flatten(Nodes);
            _rowsDirty = false;
        }

        /// <summary>Zeilenhöhe in Gerätepixeln: "Xg" in der Knotenschrift plus
        /// Padding — die Zeile folgt dem Skin, ohne dass hier eine Zahl steht.</summary>
        internal int RowHeight
        {
            get
            {
                EnsureMetrics();
                return _rowHeight;
            }
        }

        /// <summary>Rohe Textzeilenhöhe — Kante von Glyphe und Bild (TreeLayout.Parts).</summary>
        internal int LineHeight
        {
            get
            {
                EnsureMetrics();
                return _lineHeight;
            }
        }

        private void EnsureMetrics()
        {
            var skin = SkinManager.Current;
            int dpi = DeviceDpi;
            if (ReferenceEquals(skin, _metricsSkin) && dpi == _metricsDpi) return;

            var node = skin.GetAppearance(ElementKeys.TreeNode, ElementState.Normal);
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var text = SkinPainter.MeasureText(g, "Xg", node, dpi);
                _lineHeight = text.Height;
                _rowHeight = SkinPainter.InflateByPadding(text, node, dpi).Height;
            }

            _metricsSkin = skin;
            _metricsDpi = dpi;
        }

        private int ScrollBarThickness
        {
            get { return DpiScale.Scale(SkinScrollBar.ThicknessLogical, DeviceDpi); }
        }

        /// <summary>Die Fläche, in der Zeilen liegen: Innenrechteck des
        /// Baum-Rahmens, abzüglich einer sichtbaren Leiste.</summary>
        internal Rectangle Viewport
        {
            get
            {
                var appearance = SkinManager.Current.GetAppearance(ElementKeys.Tree, ElementState.Normal);
                var inner = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);

                if (_vertical != null && _vertical.Visible)
                    inner.Width = Math.Max(0, Math.Min(inner.Width, ClientSize.Width - ScrollBarThickness - inner.Left));

                return inner;
            }
        }

        internal TreeLayout CurrentLayout
        {
            get { return new TreeLayout(RowHeight, Viewport.Height, _verticalOffset, Rows.Count); }
        }

        internal int VerticalOffset
        {
            get { return _verticalOffset; }
            set
            {
                int clamped = ClampVertical(value);
                if (_verticalOffset == clamped) return;

                _verticalOffset = clamped;
                Invalidate();
                SyncScrollBar();
            }
        }

        private int ClampVertical(int value)
        {
            int max = CurrentLayout.MaxScrollOffset;
            if (value < 0) return 0;
            return value > max ? max : value;
        }

        private void ClampOffsets()
        {
            _verticalOffset = ClampVertical(_verticalOffset);
            SyncScrollBar();
        }

        /// <summary>Bringt die Leiste auf den Stand des Baums; _syncing schneidet
        /// die Rückkopplung Leiste → Versatz → Leiste ab (Muster GridControl).</summary>
        private void SyncScrollBar()
        {
            if (_vertical == null) return;   // während des Konstruktors

            var layout = CurrentLayout;
            _syncing = true;
            try
            {
                int thickness = ScrollBarThickness;
                _vertical.Visible = layout.ViewportHeight > 0 && layout.TotalHeight > layout.ViewportHeight;
                _vertical.Bounds = new Rectangle(ClientSize.Width - thickness, 0, thickness, Math.Max(1, ClientSize.Height));
                _vertical.Minimum = 0;
                _vertical.Maximum = layout.TotalHeight;
                _vertical.LargeChange = Math.Max(1, layout.ViewportHeight);
                _vertical.SmallChange = Math.Max(1, layout.RowHeight);
                _vertical.Value = _verticalOffset;
            }
            finally
            {
                _syncing = false;
            }
        }

        public void PerformWheel(int delta)
        {
            _vertical.PerformWheel(delta);
            VerticalOffset = _vertical.Value;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            PerformWheel(e.Delta);
            base.OnMouseWheel(e);
        }

        /// <summary>Klappt die Vorfahren auf und scrollt so wenig wie nötig,
        /// bis die Zeile ganz im Bild liegt (unten bündig, nicht oben — sonst
        /// spränge die Sicht bei jedem Pfeil-Schritt eine Seite).</summary>
        public void EnsureVisible(TreeNode node)
        {
            if (node == null || !ReferenceEquals(node.Tree, this)) return;

            ExpandAncestors(node);
            int index = IndexOfNodeRow(node);
            if (index < 0) return;

            EnsureRowVisible(index);
        }

        private void EnsureRowVisible(int index)
        {
            var layout = CurrentLayout;
            if (layout.ViewportHeight <= 0) return;

            int top = index * layout.RowHeight;
            int bottom = top + layout.RowHeight;

            if (top < _verticalOffset) VerticalOffset = top;
            else if (bottom > _verticalOffset + layout.ViewportHeight) VerticalOffset = bottom - layout.ViewportHeight;
        }

        internal int IndexOfNodeRow(TreeNode node)
        {
            if (node == null) return -1;

            var rows = Rows;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Kind == TreeRowKind.Node && ReferenceEquals(rows[i].Node, node)) return i;
            return -1;
        }

        // ---- Treffer ---------------------------------------------------------------

        /// <summary>Der Knoten unter einem Client-Punkt; null daneben, unterhalb
        /// der letzten Zeile und auf Platzhaltern.</summary>
        public TreeNode HitTest(Point clientPoint)
        {
            int index = RowIndexAt(clientPoint);
            if (index < 0) return null;

            var row = Rows[index];
            return row.Kind == TreeRowKind.Node ? row.Node : null;
        }

        internal int RowIndexAt(Point point)
        {
            var viewport = Viewport;
            if (!viewport.Contains(point)) return -1;
            return CurrentLayout.RowAt(point.Y - viewport.Top);
        }

        internal Rectangle RowBounds(int rowIndex)
        {
            var viewport = Viewport;
            var layout = CurrentLayout;
            return new Rectangle(viewport.Left, viewport.Top + layout.RowTop(rowIndex), viewport.Width, layout.RowHeight);
        }

        /// <summary>Glyphe/Bild/Text einer Zeile — Geometrie IMMER aus
        /// TreeNode/Normal (Haus-Regel).</summary>
        internal TreeRowParts PartsOf(int rowIndex)
        {
            var normal = SkinManager.Current.GetAppearance(ElementKeys.TreeNode, ElementState.Normal);
            var content = SkinPainter.GetContentRectangle(RowBounds(rowIndex), normal, DeviceDpi);
            return TreeLayout.Parts(content, Rows[rowIndex].Level, LineHeight);
        }

        // ---- Zeichnen -------------------------------------------------------------

        /// <summary>Zeichnet Fläche, Rahmen und Zeilen — öffentlich als Test-Naht
        /// (Muster GridControl.DrawTo); die kopflose Suite beweist damit die
        /// Virtualisierung.</summary>
        public void DrawTo(Graphics g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            var appearance = SkinManager.Current.GetAppearance(ElementKeys.Tree,
                Enabled ? ElementState.Normal : ElementState.Disabled);
            SkinPainter.DrawBackground(g, ClientRectangle, appearance, DeviceDpi);
            SkinPainter.DrawBorder(g, ClientRectangle, appearance, DeviceDpi);
            DrawRows(g);
        }

        /// <summary>Fläche und Rahmen hat SkinnedControl.OnPaint schon gemalt —
        /// hier nur die Zeilen (Muster GalleryControl.PaintContent: so feuert
        /// auch das Paint-Ereignis).</summary>
        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            DrawRows(g);
        }

        private void DrawRows(Graphics g)
        {
            int dpi = DeviceDpi;
            var rows = Rows;
            var layout = CurrentLayout;
            var viewport = Viewport;
            int lineHeight = LineHeight;

            // EINMAL je Bild holen, nicht je Zeile (Muster GridControl.DrawRows).
            var normal = SkinManager.Current.GetAppearance(ElementKeys.TreeNode, ElementState.Normal);
            var muted = SkinManager.Current.GetAppearance(ElementKeys.TreeNode, ElementState.Disabled);
            var glyph = SkinManager.Current.GetAppearance(ElementKeys.TreeGlyph, ElementState.Normal);
            var glyphHot = SkinManager.Current.GetAppearance(ElementKeys.TreeGlyph, ElementState.Hovered);

            int painted = 0;
            var saved = g.Save();
            try
            {
                g.IntersectClip(viewport);

                for (int i = 0; i < layout.VisibleRowCount; i++)
                {
                    int index = layout.FirstVisibleRow + i;
                    var row = rows[index];
                    var bounds = new Rectangle(viewport.Left, viewport.Top + layout.RowTop(index),
                        viewport.Width, layout.RowHeight);

                    var content = SkinPainter.GetContentRectangle(bounds, normal, dpi);
                    var parts = TreeLayout.Parts(content, row.Level, lineHeight);

                    if (row.Kind == TreeRowKind.Node)
                    {
                        var appearance = NodeAppearance(row.Node, index);
                        SkinPainter.DrawBackground(g, bounds, appearance, dpi);
                        SkinPainter.DrawBorder(g, bounds, appearance, dpi);

                        if (row.Node.IsExpandable)
                            SkinPainter.DrawDisclosureGlyph(g, parts.Glyph, row.Node.IsExpanded,
                                index == _hotRow && _hotGlyph ? glyphHot : glyph);

                        if (row.Node.Image != null)
                            SkinPainter.DrawScaledImage(g, row.Node.Image, parts.Image, Enabled);

                        SkinPainter.DrawText(g, row.Node.Text, parts.Text, appearance, dpi, ContentAlignment.MiddleLeft);
                    }
                    else
                    {
                        // Platzhalter stehen, wo ein Kind-Text stünde — gedämpft.
                        string text = row.Kind == TreeRowKind.Loading ? _loadingText : row.Node.LoadError;
                        SkinPainter.DrawText(g, text, parts.Text, muted, dpi, ContentAlignment.MiddleLeft);
                    }

                    painted++;
                }
            }
            finally
            {
                g.Restore(saved);
            }

            LastPaintedRowCountForTests = painted;
        }

        /// <summary>Rangfolge einer Knotenzeile: Disabled vor Auswahl vor Hover.
        /// Auswahl ohne Fokus = Pressed (siehe ElementKeys.TreeNode).</summary>
        private ElementAppearance NodeAppearance(TreeNode node, int rowIndex)
        {
            // Das Drop-Ziel schlägt Auswahl und Hover (Muster Grid/Gallery).
            if (Enabled && ReferenceEquals(node, _dropHighlight))
                return SkinManager.Current.GetAppearance(ElementKeys.DropTarget, ElementState.Normal);

            ElementState state;
            if (!Enabled) state = ElementState.Disabled;
            else if (ReferenceEquals(node, _selected)) state = Focused ? ElementState.Selected : ElementState.Pressed;
            else if (rowIndex == _hotRow) state = ElementState.Hovered;
            else state = ElementState.Normal;

            return SkinManager.Current.GetAppearance(ElementKeys.TreeNode, state);
        }

        // ---- Maus ------------------------------------------------------------------

        internal int HotRowForTests
        {
            get { return _hotRow; }
        }

        private bool IsGlyphHit(int rowIndex, Point point)
        {
            var row = Rows[rowIndex];
            return row.Kind == TreeRowKind.Node && row.Node.IsExpandable && PartsOf(rowIndex).Glyph.Contains(point);
        }

        internal void HandleMouseDown(Point point, MouseButtons button, Keys modifiers)
        {
            int index = RowIndexAt(point);
            if (index < 0) return;

            var row = Rows[index];
            if (row.Kind != TreeRowKind.Node) return;

            if (button == MouseButtons.Left)
            {
                if (IsGlyphHit(index, point))
                {
                    row.Node.Toggle();
                    return;
                }

                if (!TryDeferClickForDrag(row.Node)) SetSelected(row.Node);
            }

            // Rechts wählt nie selbst — weder beim Drücken noch beim Zugbeginn
            // (Befund I2, siehe StartItemDrag); die App liest den Knoten per
            // HitTest fürs Kontextmenü.
            if (button == MouseButtons.Left || button == MouseButtons.Right) ArmItemDrag(point, button, row.Node);
        }

        /// <summary>
        /// Bei AllowItemDrag wählt ein Druck NICHT sofort — sonst würde ein
        /// bloßes Ziehen im Baum schon selbst auswählen, und im Explorer
        /// bedeutet Auswahl Navigation (Befund I2 der Abschlussprüfung, gültig
        /// nur für den Baum — siehe AllowItemDrag): ein Zug in einen Ordner
        /// navigierte die Liste sofort hinein, noch bevor DoDragDrop beginnt.
        /// Die Auswahl wandert stattdessen aufs Loslassen (HandleMouseUp,
        /// CompleteDeferredClick), sofern kein Zug beginnt — StartItemDrag
        /// verwirft sie dann. Ohne AllowItemDrag bleibt es beim sofortigen
        /// Auswählen wie bisher.
        /// </summary>
        private bool TryDeferClickForDrag(TreeNode node)
        {
            if (!AllowItemDrag) return false;

            _deferredClickNode = node;
            return true;
        }

        internal void HandleMouseMove(Point point)
        {
            int index = RowIndexAt(point);
            bool glyph = index >= 0 && IsGlyphHit(index, point);
            if (index != _hotRow || glyph != _hotGlyph)
            {
                _hotRow = index;
                _hotGlyph = glyph;
                Invalidate();
            }

            if (_drag.IsArmed && _drag.ShouldStart(point, SystemInformation.DragSize)) StartItemDrag();
        }

        internal void HandleDoubleClick(Point point)
        {
            // Mirrors GridControl.PerformDoubleClick (Befund F6 dort): ein
            // NodeActivated-Handler kann die Baumstruktur ändern (etwa Navigate
            // im Demo), und ein noch aufgeschobener Klick darf das folgende
            // MouseUp dann nicht mehr falsch umwählen. Räumen VOR OnNodeActivated.
            _deferredClickNode = null;
            _drag.Disarm();

            int index = RowIndexAt(point);
            if (index < 0) return;

            var row = Rows[index];
            if (row.Kind != TreeRowKind.Node) return;

            // Auf der Glyphe hat schon das Drücken geklappt — ein zweites
            // Klappen machte den Doppelklick zum No-op.
            if (IsGlyphHit(index, point)) return;

            if (row.Node.IsExpandable) row.Node.Toggle();
            OnNodeActivated(row.Node);
        }

        internal void HandleMouseUp(Point point, MouseButtons button)
        {
            if (button == MouseButtons.Left) CompleteDeferredClick();

            _drag.Disarm();
            _dragNode = null;
        }

        /// <summary>Löst eine bei AllowItemDrag aufgeschobene Auswahl ein —
        /// Gegenstück zu TryDeferClickForDrag. Ein Knoten, der den Baum seit
        /// dem Druck verlassen hat, wird nicht mehr ausgewählt.</summary>
        private void CompleteDeferredClick()
        {
            var node = _deferredClickNode;
            _deferredClickNode = null;
            if (node == null || !ReferenceEquals(node.Tree, this)) return;

            SetSelected(node);
        }

        /// <summary>Ein linker Klick (Drücken und Loslassen) — Test-Naht wie
        /// GridControl.PerformClick.</summary>
        public void PerformClick(Point point)
        {
            HandleMouseDown(point, MouseButtons.Left, Keys.None);
            HandleMouseUp(point, MouseButtons.Left);
        }

        public void PerformDoubleClick(Point point)
        {
            HandleDoubleClick(point);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (CanFocus) Focus();

            // Der zweite Druck eines Doppelklicks (Clicks == 2) wählt nicht neu
            // und klappt nicht erneut — das erledigt HandleDoubleClick.
            if (e.Clicks == 1) HandleMouseDown(e.Location, e.Button, ModifierKeys);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            HandleMouseMove(e.Location);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            HandleMouseUp(e.Location, e.Button);
            base.OnMouseUp(e);
        }

        /// <summary>
        /// Die Maushalterung geht verloren, OHNE dass das passende MouseUp je
        /// ankam — ein modaler Dialog aus einem SelectedNodeChanged-Handler, ein
        /// Kontextmenü, das aus MouseDown heraus aufgeht. Ohne dieses Aufräumen
        /// bliebe der Zug-Tracker scharf und der nächste bloße Move löste ein
        /// ItemDrag aus (Muster GridControl.OnMouseCaptureChanged, Task 13
        /// Fix-Runde 1).
        /// </summary>
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            _drag.Disarm();
            _dragNode = null;
            _deferredClickNode = null;
            base.OnMouseCaptureChanged(e);
        }

        /// <summary>Simuliert den Verlust der Maushalterung mitten im Druck —
        /// kopflos löst nichts von selbst OnMouseCaptureChanged aus.</summary>
        internal void CaptureLostForTests()
        {
            OnMouseCaptureChanged(EventArgs.Empty);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hotRow != -1 || _hotGlyph)
            {
                _hotRow = -1;
                _hotGlyph = false;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) HandleDoubleClick(e.Location);
            base.OnMouseDoubleClick(e);
        }

        // ---- Tastatur ----------------------------------------------------------------

        /// <summary>
        /// Eine Taste; true = behandelt. Öffentlich als Test-Naht wie
        /// GridControl.PerformKey. Alt/Strg-Kombinationen gehören der App
        /// (Alt+← ist im Explorer "Zurück").
        /// </summary>
        public bool PerformKey(Keys keyData)
        {
            if ((keyData & (Keys.Alt | Keys.Control)) != 0) return false;

            var rows = Rows;
            if (rows.Count == 0) return false;

            int current = IndexOfNodeRow(_selected);
            var key = keyData & Keys.KeyCode;

            switch (key)
            {
                case Keys.Enter:
                    if (_selected == null) return false;
                    OnNodeActivated(_selected);
                    return true;

                case Keys.Down:
                    SelectRow(current < 0 ? NodeRowFrom(0, 1) : NodeRowFrom(current + 1, 1));
                    return true;

                case Keys.Up:
                    SelectRow(current < 0 ? NodeRowFrom(0, 1) : NodeRowFrom(current - 1, -1));
                    return true;

                case Keys.Home:
                    SelectRow(NodeRowFrom(0, 1));
                    return true;

                case Keys.End:
                    SelectRow(NodeRowFrom(rows.Count - 1, -1));
                    return true;

                case Keys.PageDown:
                case Keys.PageUp:
                {
                    int page = Math.Max(1, CurrentLayout.ViewportHeight / Math.Max(1, RowHeight) - 1);
                    int target = key == Keys.PageDown
                        ? Math.Min(rows.Count - 1, Math.Max(0, current) + page)
                        : Math.Max(0, current - page);
                    int snapped = key == Keys.PageDown ? NodeRowFrom(target, -1) : NodeRowFrom(target, 1);
                    if (snapped < 0) snapped = key == Keys.PageDown ? NodeRowFrom(target, 1) : NodeRowFrom(target, -1);
                    SelectRow(snapped);
                    return true;
                }

                case Keys.Right:
                    if (_selected == null) { SelectRow(NodeRowFrom(0, 1)); return true; }
                    if (_selected.IsExpandable && !_selected.IsExpanded) _selected.Expand();
                    else if (_selected.IsExpanded && _selected.Children.Count > 0) SelectRow(IndexOfNodeRow(_selected.Children[0]));
                    return true;

                case Keys.Left:
                    if (_selected == null) { SelectRow(NodeRowFrom(0, 1)); return true; }
                    if (_selected.IsExpanded) _selected.Collapse();
                    else if (_selected.Parent != null) SelectRow(IndexOfNodeRow(_selected.Parent));
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Tippen zum Springen über die SICHTBAREN Knoten (Platzhalter
        /// zählen nicht) — Semantik siehe TypeAheadBuffer.</summary>
        public void PerformTyping(char c)
        {
            if (char.IsControl(c)) return;

            var rows = Rows;
            if (rows.Count == 0) return;

            _typeAhead.Append(c, Clock());
            int match = _typeAhead.FindMatch(rows.Count, IndexOfNodeRow(_selected),
                i => rows[i].Kind == TreeRowKind.Node ? rows[i].Node.Text : null);

            if (match >= 0) SelectRow(match);
        }

        /// <summary>Die erste Knotenzeile ab index in Richtung step; -1 ohne.</summary>
        private int NodeRowFrom(int index, int step)
        {
            var rows = Rows;
            for (int i = index; i >= 0 && i < rows.Count; i += step)
                if (rows[i].Kind == TreeRowKind.Node) return i;
            return -1;
        }

        private void SelectRow(int index)
        {
            if (index < 0) return;

            SetSelected(Rows[index].Node);
            EnsureRowVisible(index);
        }

        /// <summary>Pfeile, Pos1/Ende, Enter kommen hier an (Muster
        /// GalleryControl.ProcessDialogKey); nicht Behandeltes läuft weiter
        /// (Tab wechselt den Fokus, Buchstaben werden zu OnKeyPress).</summary>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (PerformKey(keyData)) return true;
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            PerformTyping(e.KeyChar);
            base.OnKeyPress(e);
        }

        // ---- Drag & Drop -------------------------------------------------------------

        /// <summary>Schaltet die Zieh-Geste ein (Standard aus). Muster
        /// GridControl.AllowItemDrag.</summary>
        /// <remarks>
        /// Abweichung von der Spezifikation, NUR für den Baum (Befund I2 der
        /// Abschlussprüfung): anders als bei Grid/Gallery wählt ein Druck bei
        /// AllowItemDrag nicht mehr sofort aus — er wählt erst beim Loslassen,
        /// und ein Zugbeginn ändert die Auswahl überhaupt nicht mehr (siehe
        /// TryDeferClickForDrag/StartItemDrag). Grund: Im Explorer-Baum löst
        /// eine Auswahl Navigation aus (SelectedNodeChanged navigiert die
        /// Liste in den Ordner) — würde ein Druck weiterhin sofort auswählen,
        /// navigierte schon das bloße Ziehen eines Ordners in ihn hinein, noch
        /// bevor DoDragDrop beginnt. Grid und Gallery behalten die
        /// Spezifikation (Vorauswahl beim Zugbeginn), weil ihre Auswahl keine
        /// Navigation auslöst.
        /// </remarks>
        [Category("Behavior")]
        [DefaultValue(false)]
        public bool AllowItemDrag { get; set; }

        /// <summary>Ein Zug beginnt; die App ruft DoDragDrop mit ihren Daten.</summary>
        public event EventHandler<TreeNodeDragEventArgs> ItemDrag;

        protected virtual void OnItemDrag(TreeNodeDragEventArgs e)
        {
            var handler = ItemDrag;
            if (handler != null) handler(this, e);
        }

        /// <summary>Der Knoten unter einem Client-Punkt, sonst null (auch auf
        /// Platzhaltern) — für DragOver/DragDrop der App.</summary>
        public TreeNode GetDropTarget(Point clientPoint)
        {
            return HitTest(clientPoint);
        }

        /// <summary>
        /// Der Knoten, der als Drop-Ziel hervorgehoben wird (null: keiner). Die
        /// App setzt und löscht ihn. Verweilt er auf einem zugeklappten,
        /// aufklappbaren Knoten, klappt der Baum ihn nach
        /// AutoExpandDelayMilliseconds auf (Explorer). Ein fremder Knoten wird
        /// still zu null — ein Wurf mitten in DragOver wäre unverhältnismäßig.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TreeNode DropHighlight
        {
            get { return _dropHighlight; }
            set
            {
                if (value != null && !ReferenceEquals(value.Tree, this)) value = null;
                if (ReferenceEquals(_dropHighlight, value)) return;

                _dropHighlight = value;
                RestartAutoExpand();
                Invalidate();
            }
        }

        private void RestartAutoExpand()
        {
            _expandTimer.Stop();
            _expandPending = _dropHighlight != null && _dropHighlight.IsExpandable && !_dropHighlight.IsExpanded;
            if (_expandPending) _expandTimer.Start();
        }

        private void AutoExpandTick()
        {
            _expandTimer.Stop();
            _expandPending = false;
            if (_dropHighlight != null && !_dropHighlight.IsExpanded) _dropHighlight.Expand();
        }

        private void ArmItemDrag(Point point, MouseButtons button, TreeNode node)
        {
            if (!AllowItemDrag) return;

            _dragNode = node;
            _drag.Arm(point, button);
        }

        /// <summary>
        /// Der Zug beginnt — die Auswahl bleibt UNVERÄNDERT (Befund I2 der
        /// Abschlussprüfung): TreeNodeDragEventArgs.Node nennt den gezogenen
        /// Knoten unabhängig von SelectedNode, ein Zug ist im Explorer keine
        /// Navigation. Eine noch aufgeschobene Auswahl (TryDeferClickForDrag)
        /// verfällt hier ebenfalls. DoDragDrop blockiert im Handler bis zum
        /// Loslassen — der Tracker ist bereits entschärft.
        /// </summary>
        private void StartItemDrag()
        {
            _deferredClickNode = null;

            var node = _dragNode;
            _dragNode = null;
            if (node == null || !ReferenceEquals(node.Tree, this)) return;

            OnItemDrag(new TreeNodeDragEventArgs(node, _drag.Button));
        }

        internal void DragOverAt(Point clientPoint)
        {
            var viewport = Viewport;
            _autoScroller.Update(clientPoint.Y, viewport.Top, viewport.Bottom, RowHeight);
        }

        internal bool IsAutoScrollingForTests
        {
            get { return _autoScroller.IsRunning; }
        }

        internal void AutoScrollTickForTests()
        {
            _autoScroller.Tick();
        }

        internal void DragLeaveForTests()
        {
            _autoScroller.Stop();
        }

        internal bool IsAutoExpandPendingForTests
        {
            get { return _expandPending; }
        }

        internal void AutoExpandTickForTests()
        {
            AutoExpandTick();
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);
            DragOverAt(PointToClient(new Point(drgevent.X, drgevent.Y)));
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            base.OnDragOver(drgevent);
            DragOverAt(PointToClient(new Point(drgevent.X, drgevent.Y)));
        }

        protected override void OnDragLeave(EventArgs e)
        {
            _autoScroller.Stop();
            base.OnDragLeave(e);
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            _autoScroller.Stop();
            base.OnDragDrop(drgevent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoScroller.Dispose();
                _expandTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        // ---- Rückmeldungen aus dem Modell -------------------------------------

        internal void OnNodeStructureChanged()
        {
            _rowsDirty = true;
            _hotRow = -1;
            _hotGlyph = false;

            if (_selected != null && !ReferenceEquals(_selected.Tree, this))
            {
                _selected = null;
                OnSelectedNodeChanged();
            }

            if (_dropHighlight != null && !ReferenceEquals(_dropHighlight.Tree, this)) DropHighlight = null;
            if (_dragNode != null && !ReferenceEquals(_dragNode.Tree, this))
            {
                _dragNode = null;
                _drag.Disarm();
            }

            ClampOffsets();
            Invalidate();
        }

        internal void OnNodeVisualChanged(TreeNode node)
        {
            Invalidate();
        }

        /// <summary>Explorer: klappt man einen Vorfahren der Auswahl zu, wandert
        /// die Auswahl auf ihn — sonst stünde sie unsichtbar im Verborgenen.</summary>
        internal void OnNodeCollapsed(TreeNode node)
        {
            if (_selected != null && _selected.IsDescendantOf(node)) SetSelected(node);
        }

        internal void RaiseNodeExpanding(TreeNode node)
        {
            var handler = NodeExpanding;
            if (handler != null) handler(this, new TreeNodeEventArgs(node));
        }

        private void SetSelected(TreeNode node)
        {
            if (ReferenceEquals(_selected, node)) return;

            _selected = node;
            Invalidate();
            OnSelectedNodeChanged();
        }

        private static void ExpandAncestors(TreeNode node)
        {
            for (var p = node.Parent; p != null; p = p.Parent) p.Expand();
        }

        // ---- Größe, DPI, Fokus -------------------------------------------------------

        protected override void OnSizeChanged(EventArgs e)
        {
            if (_vertical != null) ClampOffsets();
            base.OnSizeChanged(e);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            // Zeilenhöhe folgt der Schrift; der Versatz ist physisch und muss
            // neu geklemmt werden (Muster GridControl).
            ClampOffsets();
            base.OnDpiChangedAfterParent(e);
        }

        // Fokuswechsel: die Auswahl wechselt ihr Aussehen (Selected ↔ Pressed);
        // SkinnedControl invalidiert bei GotFocus/LostFocus bereits selbst.
    }
}
