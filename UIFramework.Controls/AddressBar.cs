using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    public sealed class AddressPathEventArgs : EventArgs
    {
        public AddressPathEventArgs(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }

    /// <summary>Ein Segment der Adressleiste: sein Text und der Teilpfad bis zu ihm.</summary>
    internal struct AddressSegmentInfo
    {
        public AddressSegmentInfo(string text, string path)
        {
            Text = text;
            Path = path;
        }

        public string Text { get; }
        public string Path { get; }
    }

    /// <summary>
    /// Die Explorer-Adressleiste: Pfadteile als anklickbare Segmente, ein Klick
    /// auf die freie Fläche (oder BeginEdit, von der App auf Strg+L gelegt)
    /// macht daraus ein Textfeld. Rechnet selbst nichts (AddressBarLayout);
    /// alle Maße kommen aus SkinPainter-Messungen — kein DpiScale in dieser
    /// Assembly. Navigieren ist App-Sache: die Leiste meldet nur.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SegmentClicked")]
    public class AddressBar : SkinnedControl
    {
        private const string SeparatorText = "›";   // ›
        private const string OverflowText = "«";    // «

        private readonly AddressEditBox _editor;
        private string _rootText = "";
        private string _path = "/";
        private bool _editing;
        private bool _closing;
        private bool _enterPending;
        private int _hotBox = -1;
        private int _pressedBox = -1;

        public AddressBar()
        {
            // Den Fokus bekommt nur der Texteditor — die Segmentleiste ist eine
            // Mausfläche (Muster MenuBar).
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            _editor = new AddressEditBox { Visible = false };

            // Enter und Fokusverlust melden beide EditConfirmed (ButtonEditBase).
            // PreviewKeyDown kommt VOR dem KeyDown, der bestätigt — so lässt sich
            // "mit Enter bestätigt" von "weggeklickt" unterscheiden.
            _editor.PreviewKeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _enterPending = true; };
            _editor.EditConfirmed += (s, e) => { if (_enterPending) CommitEdit(); else CancelEdit(); };
            _editor.EditCancelled += (s, e) => CancelEdit();
            Controls.Add(_editor);

            Size = new Size(300, 24);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.AddressBar; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        /// <summary>Text des ersten Segments (Pfad "/"); leer → "/".</summary>
        [Category("Appearance")]
        [DefaultValue("")]
        public string RootText
        {
            get { return _rootText; }
            set
            {
                string next = value ?? "";
                if (_rootText == next) return;
                _rootText = next;
                Invalidate();
            }
        }

        /// <summary>Der angezeigte Unix-Pfad; null oder leer → "/".</summary>
        [Category("Behavior")]
        [DefaultValue("/")]
        public string Path
        {
            get { return _path; }
            set
            {
                string next = string.IsNullOrEmpty(value) ? "/" : value;
                if (_path == next) return;
                _path = next;
                _hotBox = -1;
                _pressedBox = -1;
                Invalidate();
            }
        }

        [Browsable(false)]
        public bool IsEditing
        {
            get { return _editing; }
        }

        /// <summary>Ein Segment wurde angeklickt; Path = Teilpfad bis zu ihm.</summary>
        public event EventHandler<AddressPathEventArgs> SegmentClicked;

        /// <summary>Im Textmodus mit Enter bestätigt; Path = getrimmter Text.</summary>
        public event EventHandler<AddressPathEventArgs> PathSubmitted;

        internal static AddressSegmentInfo[] Segments(string rootText, string path)
        {
            var segments = new List<AddressSegmentInfo>
            {
                new AddressSegmentInfo(string.IsNullOrEmpty(rootText) ? "/" : rootText, "/")
            };

            string current = "";
            foreach (var part in (path ?? "").Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                current += "/" + part;
                segments.Add(new AddressSegmentInfo(part, current));
            }

            return segments.ToArray();
        }

        private AddressSegmentInfo[] CurrentSegments
        {
            get { return Segments(_rootText, _path); }
        }

        internal AddressArrangement CurrentArrangement
        {
            get
            {
                using (var bitmap = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bitmap))
                {
                    return Arrange(g);
                }
            }
        }

        /// <summary>Misst IMMER mit der Normal-Erscheinung (Haus-Regel): sonst
        /// sprängen die Segmente beim Hovern.</summary>
        private AddressArrangement Arrange(Graphics g)
        {
            int dpi = DeviceDpi;
            var bar = SkinManager.Current.GetAppearance(ElementKeys.AddressBar, ElementState.Normal);
            var segment = SkinManager.Current.GetAppearance(ElementKeys.AddressSegment, ElementState.Normal);
            var content = SkinPainter.GetContentRectangle(ClientRectangle, bar, dpi);

            var segments = CurrentSegments;
            var widths = new int[segments.Length];
            for (int i = 0; i < segments.Length; i++)
            {
                widths[i] = SkinPainter.InflateByPadding(
                    SkinPainter.MeasureText(g, segments[i].Text, segment, dpi), segment, dpi).Width;
            }

            var separator = SkinPainter.MeasureText(g, SeparatorText, segment, dpi);
            int separatorWidth = separator.Width + separator.Height / 2;
            int overflowWidth = SkinPainter.InflateByPadding(
                SkinPainter.MeasureText(g, OverflowText, segment, dpi), segment, dpi).Width;

            return AddressBarLayout.Arrange(content, widths, separatorWidth, overflowWidth);
        }

        // ---- Zeichnen -------------------------------------------------------

        /// <summary>Test-Naht wie GridControl.DrawTo: Fläche, Rahmen, Inhalt.</summary>
        public void DrawTo(Graphics g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            var appearance = CurrentAppearance;
            SkinPainter.DrawBackground(g, ClientRectangle, appearance, DeviceDpi);
            SkinPainter.DrawBorder(g, ClientRectangle, appearance, DeviceDpi);
            PaintContent(g, appearance);
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            if (_editing) return;   // der Texteditor deckt die ganze Fläche ab

            int dpi = DeviceDpi;
            var arrangement = Arrange(g);
            var segments = CurrentSegments;
            var muted = SkinManager.Current.GetAppearance(ElementKeys.AddressSegment, ElementState.Disabled);

            for (int i = 0; i < arrangement.Boxes.Length; i++)
            {
                var box = arrangement.Boxes[i];

                ElementState state;
                if (!Enabled) state = ElementState.Disabled;
                else if (i == _pressedBox) state = ElementState.Pressed;
                else if (i == _hotBox) state = ElementState.Hovered;
                else state = ElementState.Normal;

                var boxAppearance = SkinManager.Current.GetAppearance(ElementKeys.AddressSegment, state);
                SkinPainter.DrawBackground(g, box.Bounds, boxAppearance, dpi);

                string text = box.SegmentIndex < 0 ? OverflowText : segments[box.SegmentIndex].Text;
                SkinPainter.DrawText(g, text, box.Bounds, boxAppearance, dpi, ContentAlignment.MiddleCenter);
            }

            foreach (var separator in arrangement.Separators)
                SkinPainter.DrawText(g, SeparatorText, separator, muted, dpi, ContentAlignment.MiddleCenter);
        }

        // ---- Maus -----------------------------------------------------------

        private static int BoxIndexAt(Point point, AddressArrangement arrangement)
        {
            for (int i = 0; i < arrangement.Boxes.Length; i++)
                if (arrangement.Boxes[i].Bounds.Contains(point)) return i;
            return -1;
        }

        internal void HandleMouseDown(Point point)
        {
            if (_editing) return;

            var arrangement = CurrentArrangement;
            int box = BoxIndexAt(point, arrangement);

            if (box >= 0 && arrangement.Boxes[box].SegmentIndex >= 0)
            {
                _pressedBox = box;
                Invalidate();
                return;
            }

            // Überlauf-Segment « oder freie Fläche: Textmodus (Explorer).
            // Ein Klick auf einen Trenner tut nichts — Aufklapplisten an den
            // Pfeilen sind bewusst nicht gebaut (Spec, Nicht-Ziele).
            if (box >= 0 || arrangement.FreeZone.Contains(point)) BeginEdit();
        }

        internal void HandleMouseUp(Point point)
        {
            if (_pressedBox < 0) return;

            int pressed = _pressedBox;
            _pressedBox = -1;
            Invalidate();

            var arrangement = CurrentArrangement;
            if (BoxIndexAt(point, arrangement) != pressed) return;   // hinausgezogen: kein Klick

            var handler = SegmentClicked;
            if (handler != null)
                handler(this, new AddressPathEventArgs(CurrentSegments[arrangement.Boxes[pressed].SegmentIndex].Path));
        }

        internal void HandleMouseMove(Point point)
        {
            int hot = _editing ? -1 : BoxIndexAt(point, CurrentArrangement);
            if (hot == _hotBox) return;
            _hotBox = hot;
            Invalidate();
        }

        /// <summary>Drücken und Loslassen an derselben Stelle — Test-Naht wie
        /// GridControl.PerformClick.</summary>
        public void PerformClick(Point point)
        {
            HandleMouseDown(point);
            HandleMouseUp(point);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) HandleMouseDown(e.Location);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) HandleMouseUp(e.Location);
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            HandleMouseMove(e.Location);
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hotBox != -1)
            {
                _hotBox = -1;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Die Maushalterung geht verloren, OHNE dass das passende MouseUp je
        /// ankam — ein modaler Dialog, ein Kontextmenü aus MouseDown heraus
        /// (Muster GridControl.OnMouseCaptureChanged). Ohne dieses Aufräumen
        /// bliebe das Segment optisch gedrückt, obwohl kein Klick mehr aussteht.
        /// </summary>
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            if (_pressedBox != -1)
            {
                _pressedBox = -1;
                Invalidate();
            }
            base.OnMouseCaptureChanged(e);
        }

        /// <summary>Simuliert den Verlust der Maushalterung mitten im Druck —
        /// kopflos löst nichts von selbst OnMouseCaptureChanged aus.</summary>
        internal void CaptureLostForTests()
        {
            OnMouseCaptureChanged(EventArgs.Empty);
        }

        // ---- Textmodus ------------------------------------------------------

        /// <summary>Macht die Leiste zum Textfeld mit dem vollen Pfad, markiert.</summary>
        public void BeginEdit()
        {
            if (_editing) return;

            _editing = true;
            _enterPending = false;
            _hotBox = -1;
            _pressedBox = -1;

            _editor.Text = _path;
            _editor.Bounds = ClientRectangle;
            _editor.Visible = true;
            _editor.BringToFront();
            _editor.SelectAllText();
            _editor.FocusText();
            Invalidate();
        }

        /// <summary>Zurück zu den Segmenten, ohne Ereignis.</summary>
        public void CancelEdit()
        {
            if (!_editing || _closing) return;
            EndEdit();
        }

        internal void CommitEdit()
        {
            if (!_editing || _closing) return;

            string text = (_editor.Text ?? "").Trim();
            EndEdit();
            if (text.Length == 0) return;   // leer = Abbruch

            var handler = PathSubmitted;
            if (handler != null) handler(this, new AddressPathEventArgs(text));
        }

        /// <summary>Das Verbergen des fokussierten Editors löst Fokusverlust
        /// und damit ein zweites EditConfirmed aus — _closing schneidet diese
        /// Rückkopplung ab (Muster GridControl.CloseEditor).</summary>
        private void EndEdit()
        {
            _closing = true;
            try
            {
                _editing = false;
                _enterPending = false;
                _editor.Visible = false;
                Invalidate();
            }
            finally
            {
                _closing = false;
            }
        }

        // ---- Höhe (Muster MenuBar) -------------------------------------------

        public override Size GetPreferredSize(Size proposedSize)
        {
            int dpi = DeviceDpi;
            var bar = SkinManager.Current.GetAppearance(ElementKeys.AddressBar, ElementState.Normal);
            var segment = SkinManager.Current.GetAppearance(ElementKeys.AddressSegment, ElementState.Normal);
            var textBox = SkinManager.Current.GetAppearance(ElementKeys.TextBox, ElementState.Normal);

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var segmentSize = SkinPainter.InflateByPadding(SkinPainter.MeasureText(g, "Xg", segment, dpi), segment, dpi);
                int barHeight = SkinPainter.InflateByPadding(segmentSize, bar, dpi).Height;

                // Der Textmodus legt eine SkinTextBox über die ganze Fläche —
                // auch die muss hineinpassen.
                int editorHeight = SkinPainter.InflateByPadding(
                    SkinPainter.MeasureText(g, "Xg", textBox, dpi), textBox, dpi).Height;

                return new Size(proposedSize.Width, Math.Max(barHeight, editorHeight));
            }
        }

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

        /// <summary>Selbstheilende Bremse gegen Doppel-Skalierung — Begründung
        /// ausführlich in MenuBar.OnLayout.</summary>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            ApplyPreferredHeight();
        }

        private void ApplyPreferredHeight()
        {
            int preferred = GetPreferredSize(Size.Empty).Height;
            if (Height != preferred) Height = preferred;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (_editing && _editor != null) _editor.Bounds = ClientRectangle;
            base.OnSizeChanged(e);
        }

        // ---- Nur für Tests ----------------------------------------------------

        internal string EditTextForTests
        {
            get { return _editor.Text; }
            set { _editor.Text = value; }
        }

        internal void PressEnterForTests()
        {
            _enterPending = true;
            _editor.ConfirmForTests();
        }

        internal void PressEscapeForTests()
        {
            _editor.CancelForTests();
        }

        /// <summary>Fokusverlust meldet EditConfirmed OHNE vorheriges Enter.</summary>
        internal void LoseFocusForTests()
        {
            _editor.ConfirmForTests();
        }

        /// <summary>Der Texteditor: eine SkinTextBox, die Markieren und Fokus
        /// über die geschützten Nähte der ButtonEditBase erreichbar macht.</summary>
        private sealed class AddressEditBox : SkinTextBox
        {
            public void SelectAllText()
            {
                InnerTextBox.SelectAll();
            }

            public void FocusText()
            {
                FocusEditorCore();
            }
        }
    }
}
