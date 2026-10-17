using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>Glyph eines Editor-Knopfs — gezeichnet, keine Schrift.</summary>
    public enum EditorGlyph
    {
        ArrowDown,
        ArrowUp,
        Calendar
    }

    /// <summary>
    /// Die gemeinsame Basis aller texttragenden Editoren: Textzone links
    /// (der EINE native TextBox-Kern des Frameworks — Caret, Selektion, IME
    /// und Zwischenablage kommen vom System), Knopfzone rechts (0…n gemalte
    /// Knöpfe, ElementKeys.EditorButton, kein Control pro Knopf — dieselbe
    /// Painter-Technik wie Grid-Zellen), Popup-Anker unten (PopupHost).
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    public abstract class ButtonEditBase : SkinnedControl, IGridCellEditor
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private sealed class EditorButtonSlot
        {
            public EditorGlyph Glyph;
            public Action Click;
        }

        private readonly List<EditorButtonSlot> _buttons = new List<EditorButtonSlot>();
        private readonly TextBox _inner;
        private int _hoverButton = -1;
        private int _pressedButton = -1;
        private bool _popupWasOpenAtMouseDown;
        private PopupHost _popup;

        protected ButtonEditBase()
        {
            Size = new Size(120, 24);

            if (!HasNativeTextCore)
            {
                // Ohne Kern nimmt das Control selbst Fokus und Tastatur
                // (SkinComboBox-Verhalten).
                SetStyle(ControlStyles.Selectable, true);
                TabStop = true;
                return;
            }

            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            _inner = new TextBox();
            _inner.BorderStyle = BorderStyle.None;
            _inner.GotFocus += (s, e) => Invalidate();
            _inner.LostFocus += (s, e) =>
            {
                // Fokusverlust an das EIGENE Popup ist kein Bestätigen: Beim Öffnen
                // des Kalenders in einer Grid-Zelle würde der Wirt sonst sofort
                // committen und den Editor unter dem Popup wegreißen.
                if (!IsPopupOpen) RaiseEditConfirmed();
                Invalidate();
            };
            // Tab & Co. entstehen im Kern — der Wirt (Grid) hängt aber am ÄUSSEREN
            // Control. Weiterleiten, damit PreviewKeyDown dort ankommt.
            _inner.PreviewKeyDown += (s, e) => OnPreviewKeyDown(e);
            _inner.MouseEnter += (s, e) => OnMouseEnter(EventArgs.Empty);
            _inner.MouseLeave += (s, e) => OnMouseLeave(EventArgs.Empty);
            _inner.TextChanged += (s, e) => OnTextChanged(EventArgs.Empty);
            _inner.KeyPress += (s, e) =>
            {
                if (FilterBlocks(e.KeyChar)) e.Handled = true;
            };
            _inner.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !_inner.Multiline)
                {
                    RaiseEditConfirmed();
                    e.Handled = true;
                    e.SuppressKeyPress = true;   // kein Ding-Geräusch
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    RaiseEditCancelled();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            _inner.HandleCreated += (s, e) =>
            {
                OnInnerHandleCreated();

                // Das native Fenster existiert jetzt erst — nur ab hier lässt sich
                // die per SetBoundsCore erzwungene Einzeilen-Höhe per SetWindowPos
                // überschreiben (siehe LayoutInner). Ein bereits vor der
                // Handle-Erzeugung gelaufener Layout-Durchlauf hätte diese
                // Korrektur sonst verpasst.
                LayoutInner(CurrentAppearance);
            };

            Controls.Add(_inner);
        }

        /// <summary>False = keine Textzone mit nativem Kern; die abgeleitete
        /// Klasse malt die Zone selbst (PaintTextZone) und übernimmt Fokus und
        /// Tastatur. Wird im Konstruktor gelesen — Ableitungen müssen den Wert
        /// als Konstante liefern.</summary>
        protected virtual bool HasNativeTextCore
        {
            get { return true; }
        }

        /// <summary>Der native Kern; null ohne Kern (HasNativeTextCore == false).</summary>
        protected TextBox InnerTextBox
        {
            get { return _inner; }
        }

        /// <summary>Haken für Ableitungen, die am nativen Handle arbeiten
        /// (SkinTextBox: Platzhaltertext per EM_SETCUEBANNER).</summary>
        protected virtual void OnInnerHandleCreated()
        {
        }

        protected override bool IsSelected
        {
            get { return _inner != null && _inner.Focused; }
        }

        protected override bool ShowFocusRing
        {
            get { return !HasNativeTextCore; }
        }

        /// <summary>Bestätigen bei Fokusverlust (Befund F1) — NUR im kernlosen Fall:
        /// Mit Kern übernimmt das bereits _inner.LostFocus im Konstruktor; ein
        /// zweites Bestätigen hier wäre doppelt. Derselbe !IsPopupOpen-Guard wie
        /// dort: Sonst risse das Öffnen der eigenen Liste (SkinComboBox) den
        /// Editor unter dem Popup weg.</summary>
        protected override void OnLostFocus(EventArgs e)
        {
            if (InnerTextBox == null && !IsPopupOpen) RaiseEditConfirmed();
            base.OnLostFocus(e);
        }

        /// <summary>True, solange ein Knopf gedrückt gehalten wird (Maustaste
        /// unten, noch nicht losgelassen) — für Ableitungen, die währenddessen
        /// kein Popup öffnen wollen (SkinComboBox).</summary>
        protected bool IsButtonPressed
        {
            get { return _pressedButton >= 0; }
        }

        /// <summary>Ob beim letzten MouseDown ein Popup offen war. Ein
        /// AddButton-Callback feuert erst bei MouseUp (siehe OnMouseUp) —
        /// dazwischen kann ein wegen Deaktivierung aufgeschobenes, per
        /// BeginInvoke verzögertes PopupHost-Close (siehe
        /// PopupHost.OnDeactivate) bereits durchgelaufen sein und IsPopupOpen
        /// auf false gesetzt haben. Ein Toggle-Callback, der nur IsPopupOpen
        /// bei Up liest, würde dann fälschlich neu öffnen statt zu bleiben —
        /// er muss stattdessen auch diesen Schnappschuss prüfen (siehe
        /// SkinComboBox.Toggle, DateEdit.ToggleCalendar). Gesetzt in
        /// OnMouseDown, zurückgesetzt in OnMouseUp und OnMouseLeave — ein dort
        /// abgebrochener Klick darf keinen stale Schnappschuss hinterlassen.</summary>
        protected bool PopupWasOpenAtMouseDown
        {
            get { return _popupWasOpenAtMouseDown; }
        }

        /// <summary>Tastenfilter der Ableitung; Steuerzeichen passieren immer
        /// (Backspace, Strg+C — sonst wäre die Zwischenablage tot).</summary>
        protected virtual bool IsCharAllowed(char c)
        {
            return true;
        }

        private bool FilterBlocks(char c)
        {
            return !char.IsControl(c) && !IsCharAllowed(c);
        }

        /// <summary>Bestätigen: Enter im Kern oder Fokusverlust des Kerns.
        /// Ableitungen parsen hier ihren Text (mit stillem Rückfall).</summary>
        protected virtual void OnEditConfirmed()
        {
        }

        public event EventHandler EditConfirmed;

        protected void RaiseEditConfirmed()
        {
            OnEditConfirmed();
            var handler = EditConfirmed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>Verwerfen erbeten: Escape im Kern oder in der Ableitung.
        /// Anders als EditConfirmed ändert das den Wert NICHT — was der Wirt
        /// (Grid) daraus macht, ist seine Sache; solo verpufft es.</summary>
        public event EventHandler EditCancelled;

        protected void RaiseEditCancelled()
        {
            var handler = EditCancelled;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected void AddButton(EditorGlyph glyph, Action click)
        {
            if (click == null) throw new ArgumentNullException(nameof(click));
            _buttons.Add(new EditorButtonSlot { Glyph = glyph, Click = click });
            Invalidate();
        }

        /// <summary>Knopf i von rechts: Knopf 0 sitzt ganz rechts — dieselbe
        /// Geometrie, mit der SkinComboBox seinen Pfeil immer gemalt hat.</summary>
        private Rectangle ButtonBounds(int index, Rectangle content)
        {
            int side = content.Height;
            return new Rectangle(content.Right - side * (index + 1), content.Top, side, side);
        }

        private Rectangle TextZone(Rectangle content)
        {
            int side = content.Height;
            return new Rectangle(content.Left, content.Top,
                Math.Max(0, content.Width - side * _buttons.Count), content.Height);
        }

        private int HitButton(Point location)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, CurrentAppearance, DeviceDpi);
            for (int i = 0; i < _buttons.Count; i++)
                if (ButtonBounds(i, content).Contains(location)) return i;
            return -1;
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);

            if (_inner != null)
            {
                _inner.BackColor = appearance.Background;
                _inner.ForeColor = appearance.ForeColor;
                _inner.Font = ResourceCache.Shared.GetFont(appearance.Font, DeviceDpi);
                LayoutInner(appearance);
            }
            else
            {
                PaintTextZone(g, TextZone(content), appearance);
            }

            for (int i = 0; i < _buttons.Count; i++)
            {
                ElementState state;
                if (!Enabled) state = ElementState.Disabled;
                else if (i == _pressedButton) state = ElementState.Pressed;
                else if (i == _hoverButton) state = ElementState.Hovered;
                else state = ElementState.Normal;

                var button = SkinManager.Current.GetAppearance(ElementKeys.EditorButton, state);
                var bounds = ButtonBounds(i, content);

                SkinPainter.DrawBackground(g, bounds, button, DeviceDpi);
                SkinPainter.DrawBorder(g, bounds, button, DeviceDpi);
                DrawGlyph(g, _buttons[i].Glyph, bounds, button.ForeColor);
            }
        }

        /// <summary>Nur ohne nativen Kern gerufen: die Ableitung malt ihre
        /// Textzone selbst (SkinComboBox: den gewählten Eintrag).</summary>
        protected virtual void PaintTextZone(Graphics g, Rectangle bounds, ElementAppearance appearance)
        {
        }

        private static void DrawGlyph(Graphics g, EditorGlyph glyph, Rectangle bounds, Color color)
        {
            int size = Math.Min(bounds.Width, bounds.Height) / 3;
            if (size < 2) return;

            int cx = bounds.Left + bounds.Width / 2;
            int cy = bounds.Top + bounds.Height / 2;
            var brush = ResourceCache.Shared.GetBrush(color);

            switch (glyph)
            {
                case EditorGlyph.ArrowDown:
                    g.FillPolygon(brush, new[]
                    {
                        new Point(cx - size, cy - size / 2),
                        new Point(cx + size, cy - size / 2),
                        new Point(cx, cy + size / 2)
                    });
                    break;

                case EditorGlyph.ArrowUp:
                    g.FillPolygon(brush, new[]
                    {
                        new Point(cx - size, cy + size / 2),
                        new Point(cx + size, cy + size / 2),
                        new Point(cx, cy - size / 2)
                    });
                    break;

                case EditorGlyph.Calendar:
                    // Blatt mit Kopfzeile — bewusst schlicht, ein Piktogramm,
                    // keine Miniatur.
                    var sheet = new Rectangle(cx - size, cy - size, size * 2, size * 2);
                    var pen = ResourceCache.Shared.GetPen(color, 1);
                    g.DrawRectangle(pen, sheet);
                    g.FillRectangle(brush, new Rectangle(sheet.X, sheet.Y, sheet.Width, Math.Max(2, size / 2)));
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hit = HitButton(e.Location);
            if (hit != _hoverButton)
            {
                _hoverButton = hit;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoverButton != -1 || _pressedButton != -1)
            {
                _hoverButton = -1;
                _pressedButton = -1;

                // Ein hier abgebrochener Klick (Down auf dem Knopf, dann raus
                // gezogen) darf keinen Schnappschuss stehen lassen — sonst
                // verbiegt er den NÄCHSTEN, ganz unabhängigen Klick auf
                // denselben Knopf (siehe OnMouseUp).
                _popupWasOpenAtMouseDown = false;

                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                // Schnappschuss VOR dem Hit-Test — siehe PopupWasOpenAtMouseDown.
                _popupWasOpenAtMouseDown = IsPopupOpen;

                int hit = HitButton(e.Location);
                if (hit >= 0)
                {
                    _pressedButton = hit;
                    Invalidate();
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_pressedButton >= 0)
            {
                int pressed = _pressedButton;
                _pressedButton = -1;
                Invalidate();

                if (HitButton(e.Location) == pressed)
                    _buttons[pressed].Click();
            }

            // Unbedingt zurücksetzen, auch wenn kein Knopf mehr gedrückt war
            // (z. B. Down auf dem Knopf, dann OnMouseLeave, dann Up
            // außerhalb) — sonst bliebe der Schnappschuss stale stehen und
            // verböge den NÄCHSTEN, unabhängigen Klick auf denselben Knopf.
            _popupWasOpenAtMouseDown = false;
            base.OnMouseUp(e);
        }

        // ---- Popup-Anker ----------------------------------------------------

        protected bool IsPopupOpen
        {
            get { return _popup != null; }
        }

        protected void OpenPopup(IPopupContent content)
        {
            if (_popup != null) return;

            _popup = new PopupHost(content);
            _popup.FormClosed += (s, e) =>
            {
                _popup = null;
                OnPopupClosed();
                Invalidate();
            };

            var screenLocation = Parent != null
                ? Parent.PointToScreen(new Point(Left, Bottom))
                : PointToScreen(new Point(0, Height));

            _popup.ShowPopup(FindForm(), screenLocation, Width);
            Invalidate();
        }

        protected void ClosePopup()
        {
            if (_popup == null) return;

            var popup = _popup;
            _popup = null;
            popup.ClosePopup();
            Invalidate();
        }

        /// <summary>Nach jedem Schließen (Wahl, Escape, Deaktivierung).</summary>
        protected virtual void OnPopupClosed()
        {
        }

        // ---- Layout des Kerns (unverändert aus SkinTextBox verschoben) ------

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_inner != null) LayoutInner(CurrentAppearance);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            if (_inner != null) LayoutInner(CurrentAppearance);
        }

        private void LayoutInner(ElementAppearance appearance)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);
            var zone = TextZone(content);
            _inner.SetBounds(zone.X, zone.Y, zone.Width, zone.Height);

            // Ein einzeiliges natives TextBox zwingt seine Höhe in SetBoundsCore
            // immer auf PreferredHeight (dokumentiertes WinForms-Verhalten,
            // Multiline == false) — SetBounds allein reicht darum nicht, um es in
            // den Inhaltsbereich einzupassen. SetWindowPos setzt die native
            // Fenstergröße direkt und umgeht diese Zwangsanpassung; WinForms
            // übernimmt die neue Größe anschließend selbst über WM_WINDOWPOSCHANGED
            // in sein eigenes Bounds-Feld, ohne SetBoundsCore erneut zu durchlaufen.
            if (_inner.IsHandleCreated && _inner.Height != zone.Height)
            {
                SetWindowPos(
                    _inner.Handle, IntPtr.Zero, zone.X, zone.Y, zone.Width, zone.Height,
                    SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            if (_inner != null) _inner.Enabled = Enabled;
            base.OnEnabledChanged(e);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var appearance = CurrentAppearance;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var textSize = SkinPainter.MeasureText(g, "Xg", appearance, DeviceDpi);
                return SkinPainter.InflateByPadding(textSize, appearance, DeviceDpi);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ClosePopup();
            base.Dispose(disposing);
        }

        // ---- IGridCellEditor --------------------------------------------------

        /// <summary>Wert der Zellbearbeitung — Standard: der Text des Kerns.
        /// Ableitungen mit typisiertem Wert überschreiben beide Seiten.</summary>
        protected virtual object GetEditValue()
        {
            return InnerTextBox != null ? InnerTextBox.Text : Text;
        }

        protected virtual void SetEditValue(object value)
        {
            string text = value == null ? "" : value.ToString();
            if (InnerTextBox != null) InnerTextBox.Text = text; else Text = text;
        }

        Control IGridCellEditor.EditorControl
        {
            get { return this; }
        }

        object IGridCellEditor.EditValue
        {
            get { return GetEditValue(); }
            set { SetEditValue(value); }
        }

        void IGridCellEditor.BeginWith(string text)
        {
            if (InnerTextBox == null) return;
            InnerTextBox.Text = text ?? "";
            InnerTextBox.SelectionStart = InnerTextBox.TextLength;
        }

        void IGridCellEditor.FocusEditor()
        {
            FocusEditorCore();
        }

        /// <summary>Der Kern nimmt den Fokus, wenn es einen gibt (Text, Caret,
        /// Enter/Escape hängen dort); ohne Kern ist das Control selbst
        /// fokussierbar (SkinComboBox: Selectable=true, TabStop=true).</summary>
        protected virtual void FocusEditorCore()
        {
            if (InnerTextBox != null) InnerTextBox.Focus();
            else Focus();
        }

        event EventHandler IGridCellEditor.ConfirmRequested
        {
            add { EditConfirmed += value; }
            remove { EditConfirmed -= value; }
        }

        event EventHandler IGridCellEditor.CancelRequested
        {
            add { EditCancelled += value; }
            remove { EditCancelled -= value; }
        }

        // ---- Nur für Tests --------------------------------------------------

        internal Rectangle ButtonBoundsForTests(int index)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, CurrentAppearance, DeviceDpi);
            return ButtonBounds(index, content);
        }

        internal void ClickButtonForTests(int index)
        {
            _buttons[index].Click();
        }

        /// <summary>Simuliert MouseDown auf der Mitte des Knopfs — anders als
        /// ClickButtonForTests durchläuft das echte OnMouseDown, markiert also
        /// den Knopf als gedrückt und erfasst PopupWasOpenAtMouseDown.</summary>
        internal void PressButtonForTests(int index)
        {
            var center = CenterOf(ButtonBoundsForTests(index));
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, center.X, center.Y, 0));
        }

        /// <summary>Simuliert MouseUp auf der Mitte des Knopfs — löst damit
        /// (bei weiterhin getroffenem Knopf) denselben Click-Callback wie ein
        /// echter Loslasser aus.</summary>
        internal void ReleaseButtonForTests(int index)
        {
            var center = CenterOf(ButtonBoundsForTests(index));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, center.X, center.Y, 0));
        }

        private static Point CenterOf(Rectangle bounds)
        {
            return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }

        /// <summary>Simuliert die Maus, die den Editor verlässt, während ein
        /// Knopf noch gedrückt ist (Down auf dem Knopf, dann rausgezogen).</summary>
        internal void LeaveForTests()
        {
            OnMouseLeave(EventArgs.Empty);
        }

        /// <summary>Löst die Deaktivierung des gerade offenen Popups aus (falls
        /// eines offen ist) — für Tests, die den Zeitablauf zwischen MouseDown
        /// und MouseUp nachstellen, ohne ein echtes zweites Fenster zu
        /// aktivieren.</summary>
        internal void RaisePopupDeactivateForTests()
        {
            if (_popup != null) _popup.RaiseDeactivateForTests();
        }

        internal bool IsPopupOpenForTests
        {
            get { return IsPopupOpen; }
        }

        internal void ConfirmForTests()
        {
            RaiseEditConfirmed();
        }

        /// <summary>Simuliert Fokusverlust — Fokus selbst ist kopflos nicht
        /// auslösbar (Muster: RaisePopupDeactivateForTests).</summary>
        internal void RaiseLostFocusForTests()
        {
            OnLostFocus(EventArgs.Empty);
        }

        internal void CancelForTests()
        {
            RaiseEditCancelled();
        }

        internal bool FilterBlocksForTests(char c)
        {
            return FilterBlocks(c);
        }

        internal Rectangle InnerTextBoxForTestsBounds()
        {
            return _inner.Bounds;
        }

        /// <summary>Nur für Tests: das Control, das FocusEditorCore ansprechen
        /// würde — Fokus selbst ist kopflos nicht prüfbar.</summary>
        internal Control FocusTargetForTests()
        {
            return InnerTextBox != null ? (Control)InnerTextBox : this;
        }
    }
}
