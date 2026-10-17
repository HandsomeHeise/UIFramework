using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Dpi;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Zwei Bereiche mit einer ziehbaren, geskinnten Trennlinie — der
    /// Explorer-Splitter zwischen Baum und Liste. Rechnet selbst nichts
    /// (SplitLayout); die Dicke der Linie ist das Padding von
    /// ElementKeys.Splitter.
    ///
    /// Ausnahme von der Haus-Regel „kein DpiScale in dieser Assembly" (Muster
    /// SkinScrollBar.MinThumbLengthLogical): SplitterDistance und die
    /// Mindestgrößen sind LOGISCH (Spec), damit eine gespeicherte Breite einen
    /// Monitorwechsel überlebt und MainForm.OnLoads Scale() sie nicht ein
    /// zweites Mal multipliziert. Die Umrechnung läuft ausschließlich über
    /// ToPhysical/ToLogical unten — sonst nirgends DPI-Arithmetik hier.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SplitterMoved")]
    public class SkinSplitContainer : SkinnedControl
    {
        private Orientation _orientation = Orientation.Vertical;
        private FixedPanel _fixedPanel = FixedPanel.None;
        private int _distance = 200;
        private int _panel1MinSize = 25;
        private int _panel2MinSize = 25;

        /// <summary>Letzte Gesamtgröße entlang der Achse (physisch) — Basis
        /// für FixedPanel bei der nächsten Größenänderung.</summary>
        private int _lastTotal = -1;

        /// <summary>
        /// Während eines Scale()-Durchlaufs gesetzt (Ruling F5). Scale()
        /// vergrößert den Client rein wegen der DPI-Autoskalierung — die
        /// Entfernung ist aber logisch und folgt bereits DeviceDpi (siehe
        /// ToPhysical). Liefe die FixedPanel-Regel aus OnSizeChanged hier
        /// zusätzlich mit, würde sie ein zweites Mal auf denselben Zuwachs
        /// reagieren: bei FixedPanel.Panel1 unmerklich (die Regel greift dort
        /// gar nicht), bei None/Panel2 aber doppelt so stark wie beabsichtigt.
        /// </summary>
        private bool _scaling;

        private bool _hot;
        private bool _dragging;
        private int _dragGrab;
        private int _dragStartDistance;

        public SkinSplitContainer()
        {
            SetStyle(ControlStyles.ContainerControl, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            // VOR Size: Das Setzen von Size löst synchron Layout aus, und das
            // setzt die Bounds beider Bereiche.
            Panel1 = new SkinSplitterPanel();
            Panel2 = new SkinSplitterPanel();
            Controls.Add(Panel1);
            Controls.Add(Panel2);

            Size = new Size(400, 300);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Splitter; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        [Browsable(false)]
        public SkinSplitterPanel Panel1 { get; }

        [Browsable(false)]
        public SkinSplitterPanel Panel2 { get; }

        /// <summary>Vertical (Standard) = Bereiche nebeneinander, senkrechte
        /// Linie — dieselbe Semantik wie WinForms' SplitContainer.</summary>
        [Category("Layout")]
        [DefaultValue(Orientation.Vertical)]
        public Orientation Orientation
        {
            get { return _orientation; }
            set
            {
                if (_orientation == value) return;
                _orientation = value;
                _lastTotal = -1;
                ApplyLayout();
            }
        }

        /// <summary>Gewünschte Größe von Panel1 in LOGISCHEN Pixeln. Geklemmt
        /// wird erst beim Anordnen — der Wunsch bleibt stehen, damit ein
        /// kurzzeitig zu kleines Fenster ihn nicht dauerhaft verkürzt.</summary>
        [Category("Layout")]
        [DefaultValue(200)]
        public int SplitterDistance
        {
            get { return _distance; }
            set
            {
                int wanted = Math.Max(0, value);
                if (_distance == wanted) return;
                _distance = wanted;
                ApplyLayout();
            }
        }

        [Category("Layout")]
        [DefaultValue(25)]
        public int Panel1MinSize
        {
            get { return _panel1MinSize; }
            set
            {
                _panel1MinSize = Math.Max(0, value);
                ApplyLayout();
            }
        }

        [Category("Layout")]
        [DefaultValue(25)]
        public int Panel2MinSize
        {
            get { return _panel2MinSize; }
            set
            {
                _panel2MinSize = Math.Max(0, value);
                ApplyLayout();
            }
        }

        /// <summary>Welcher Bereich beim Vergrößern seine Größe behält —
        /// Explorer: Panel1 (der Baum).</summary>
        [Category("Layout")]
        [DefaultValue(FixedPanel.None)]
        public FixedPanel FixedPanel
        {
            get { return _fixedPanel; }
            set { _fixedPanel = value; }
        }

        /// <summary>Der Anwender hat die Linie gezogen und dabei die Entfernung
        /// wirklich geändert. Programmatisches Setzen meldet nichts — sonst
        /// liefe eine App, die ihre gespeicherte Breite zurückschreibt, in
        /// eine Schleife.</summary>
        public event EventHandler SplitterMoved;

        internal int SplitterThickness
        {
            get
            {
                var appearance = SkinManager.Current.GetAppearance(ElementKeys.Splitter, ElementState.Normal);
                var size = SkinPainter.InflateByPadding(Size.Empty, appearance, DeviceDpi);
                return Math.Max(1, _orientation == Orientation.Vertical ? size.Width : size.Height);
            }
        }

        internal bool IsDraggingSplitter
        {
            get { return _dragging; }
        }

        private int Total
        {
            get { return _orientation == Orientation.Vertical ? ClientSize.Width : ClientSize.Height; }
        }

        internal SplitGeometry CurrentGeometry
        {
            get
            {
                int thickness = SplitterThickness;
                int distance = SplitLayout.ClampDistance(ToPhysical(_distance), Total, thickness,
                    ToPhysical(_panel1MinSize), ToPhysical(_panel2MinSize));
                return SplitLayout.Arrange(ClientSize, _orientation, distance, thickness);
            }
        }

        private int ToPhysical(int logical)
        {
            return DpiScale.Scale(logical, DeviceDpi);
        }

        private int ToLogical(int physical)
        {
            float factor = DpiScale.ScaleF(1f, DeviceDpi);
            return (int)Math.Round(physical / factor, MidpointRounding.AwayFromZero);
        }

        private void ApplyLayout()
        {
            if (Panel1 == null || Panel2 == null) return;   // während des Basis-Konstruktors

            var geometry = CurrentGeometry;
            Panel1.Bounds = geometry.Panel1;
            Panel2.Bounds = geometry.Panel2;
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            ApplyLayout();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            int total = Total;
            if (!_scaling && _lastTotal > 0 && total > 0 && total != _lastTotal && _fixedPanel != FixedPanel.Panel1)
            {
                int physical = SplitLayout.DistanceAfterResize(ToPhysical(_distance), _lastTotal, total, _fixedPanel);
                _distance = Math.Max(0, ToLogical(physical));
            }
            if (!_scaling && total > 0) _lastTotal = total;

            base.OnSizeChanged(e);
            ApplyLayout();
        }

        /// <summary>
        /// Ruling F5: während einer Scale()-Runde (MainForm.OnLoads
        /// Autoskalierung, hier bei jeder DPI aufrufbar) darf OnSizeChanged
        /// die FixedPanel-Regel nicht anwenden — die Entfernung ist logisch
        /// und folgt DeviceDpi bereits von selbst (ToPhysical). _lastTotal
        /// wird erst NACH dem Skalieren neu gesetzt, auf die dann gültige
        /// physische Gesamtgröße, damit der nächste echte Resize wieder vom
        /// richtigen Ausgangswert rechnet.
        /// </summary>
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            _scaling = true;
            try
            {
                base.ScaleControl(factor, specified);
            }
            finally
            {
                _scaling = false;
            }
            _lastTotal = Total;
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            ApplyLayout();
            base.OnDpiChangedAfterParent(e);
        }

        // ---- Ziehen ---------------------------------------------------------

        internal void BeginSplitterDrag(Point point)
        {
            var splitter = CurrentGeometry.Splitter;
            if (!splitter.Contains(point)) return;

            _dragging = true;
            _dragGrab = _orientation == Orientation.Vertical ? point.X - splitter.Left : point.Y - splitter.Top;
            _dragStartDistance = _distance;
            Invalidate();
        }

        internal void DragSplitter(Point point)
        {
            if (!_dragging) return;

            int wanted = (_orientation == Orientation.Vertical ? point.X : point.Y) - _dragGrab;
            int clamped = SplitLayout.ClampDistance(wanted, Total, SplitterThickness,
                ToPhysical(_panel1MinSize), ToPhysical(_panel2MinSize));
            int logical = ToLogical(clamped);
            if (logical == _distance) return;

            _distance = logical;
            ApplyLayout();
        }

        internal void EndSplitterDrag()
        {
            if (!_dragging) return;

            _dragging = false;
            // Ohne dies bliebe der Größenänderungs-Cursor stehen, sobald die
            // Maus nach dem Loslassen nicht mehr über der Trennlinie steht —
            // OnMouseMove sieht sie ja u. U. nie wieder dort (Panel1/Panel2
            // fangen die Mausereignisse außerhalb der Trennlinie ab, siehe
            // ClearHover).
            Cursor = Cursors.Default;
            Invalidate();

            if (_distance == _dragStartDistance) return;
            var handler = SplitterMoved;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Trägt den Hover-Zustand der Trennlinie ein: ob sie den
        /// Größenänderungs-Cursor zeigt. Aus OnMouseMove herausgezogen, damit
        /// Tests sie ohne erfundene MouseEventArgs anstoßen können (Muster
        /// BeginSplitterDrag/DragSplitter).
        /// </summary>
        internal void HoverAt(Point point)
        {
            bool hot = CurrentGeometry.Splitter.Contains(point);
            if (hot != _hot)
            {
                _hot = hot;
                Invalidate();
            }
            Cursor = hot
                ? (_orientation == Orientation.Vertical ? Cursors.VSplit : Cursors.HSplit)
                : Cursors.Default;
        }

        /// <summary>
        /// Verlässt die Maus den Container, während er (oder die Trennlinie)
        /// noch als „hot" gilt, bekämen Panel1/Panel2 sonst dauerhaft den
        /// Größenänderungs-Cursor vererbt: Sie liegen über der ganzen Fläche
        /// außer der Trennlinie und setzen selbst nie einen eigenen Cursor,
        /// sodass WinForms ihren Cursor von diesem Container erbt — und weil
        /// sie die Mausereignisse dort abfangen, erreicht OnMouseMove diesen
        /// Container praktisch nie wieder, um den nicht-hot-Zweig nachzuholen.
        /// Während eines Zugs bleibt der Cursor bewusst stehen — das
        /// übernimmt EndSplitterDrag, sobald der Zug endet.
        /// </summary>
        internal void ClearHover()
        {
            if (_hot)
            {
                _hot = false;
                Invalidate();
            }
            if (!_dragging) Cursor = Cursors.Default;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) BeginSplitterDrag(e.Location);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
                DragSplitter(e.Location);
            else
                HoverAt(e.Location);

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) EndSplitterDrag();
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            ClearHover();
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Die Maushalterung geht verloren, OHNE dass das passende MouseUp je
        /// ankam — ein modaler Dialog, ein Kontextmenü aus MouseDown heraus
        /// (Muster GridControl.OnMouseCaptureChanged). Ohne dieses Aufräumen
        /// bliebe der Zug aktiv und der Größenänderungs-Cursor hinge fest,
        /// obwohl die Maus die Trennlinie längst nicht mehr drückt.
        /// EndSplitterDrag setzt den Cursor bereits zurück; ClearHover holt
        /// das für den (seltenen) Fall nach, dass die Halterung verloren geht,
        /// OHNE dass gerade gezogen wurde, die Linie aber noch als „hot" galt.
        /// </summary>
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            EndSplitterDrag();
            ClearHover();
            base.OnMouseCaptureChanged(e);
        }

        /// <summary>Simuliert den Verlust der Maushalterung mitten im Zug —
        /// kopflos löst nichts von selbst OnMouseCaptureChanged aus.</summary>
        internal void CaptureLostForTests()
        {
            OnMouseCaptureChanged(EventArgs.Empty);
        }

        // ---- Zeichnen -------------------------------------------------------

        /// <summary>
        /// Malt die Fläche (Panel-Erscheinung — durch die transparenten
        /// Bereiche sichtbar, wo ihr Inhalt sie freilässt) und die Linie.
        /// Öffentlich als Test-Naht (Muster GridControl.DrawTo).
        /// </summary>
        public void DrawTo(Graphics g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            int dpi = DeviceDpi;
            var surface = SkinManager.Current.GetAppearance(ElementKeys.Panel,
                Enabled ? ElementState.Normal : ElementState.Disabled);
            SkinPainter.DrawBackground(g, ClientRectangle, surface, dpi);

            ElementState state;
            if (!Enabled) state = ElementState.Disabled;
            else if (_dragging) state = ElementState.Pressed;
            else if (_hot) state = ElementState.Hovered;
            else state = ElementState.Normal;

            SkinPainter.DrawBackground(g, CurrentGeometry.Splitter,
                SkinManager.Current.GetAppearance(ElementKeys.Splitter, state), dpi);
        }

        /// <summary>Ruft bewusst nicht base.OnPaint (Muster GridControl):
        /// SkinnedControl malte dort die Splitter-Erscheinung über die ganze
        /// Fläche.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            DrawTo(e.Graphics);
        }
    }
}
