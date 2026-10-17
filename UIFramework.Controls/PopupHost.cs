using System;
using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Der Gast eines PopupHost: misst sich, zeichnet sich und nimmt Eingaben
    /// entgegen — mehr weiß der Host nicht über seinen Inhalt. Combo-Liste und
    /// Monatskalender sind die ersten Gäste; Teilprojekt 4 (Menüs, Ribbons)
    /// erbt den Mechanismus.
    /// </summary>
    public interface IPopupContent
    {
        Size Measure(Graphics g, int dpi, int anchorWidth);
        void Paint(Graphics g, Rectangle bounds, int dpi);
        void HandleMouseMove(Point location);
        void HandleMouseClick(Point location);
        bool HandleKey(Keys key);
        event EventHandler VisualChanged;
        event EventHandler CloseRequested;
    }

    /// <summary>
    /// Antworten auf WM_MOUSEACTIVATE (winuser.h). Nur die eine hier gebrauchte
    /// Konstante — die übrigen kämen ungenutzt ins Framework.
    /// </summary>
    internal static class MouseActivate
    {
        public const int NoActivate = 3;      // MA_NOACTIVATE
    }

    /// <summary>
    /// Das rahmenlose, nicht in der Taskleiste sichtbare Popup-Fenster unter
    /// einem Editor. Aus ComboPopup verallgemeinert: Positionierung, Öffnen,
    /// Schließen bei Deaktivierung — der Inhalt ist ein zeichnender Gast.
    /// Ein Popup, das über sein Elternfenster hinausragt, kann kein
    /// Kind-Control sein — deshalb ein eigenes Form.
    /// </summary>
    public sealed class PopupHost : Form
    {
        private readonly IPopupContent _content;
        private readonly bool _nonActivating;

        public PopupHost(IPopupContent content) : this(content, false)
        {
        }

        /// <summary>
        /// nonActivating: Für Menüs. Das Popup nimmt NIE den Fokus (WS_EX_NOACTIVATE,
        /// ShowWithoutActivation) — das Besitzerfenster bleibt aktiv, seine Titelleiste
        /// malt sich nicht "inaktiv", und der WM_ACTIVATE-Tanz, aus dem die härtesten
        /// 3a-Fehler kamen, findet gar nicht erst statt. Der Deaktivierungs-Schließpfad
        /// unten ist in diesem Modus bewusst tot: Ein nie aktives Fenster deaktiviert
        /// nie — geschlossen wird ausschließlich von außen (MenuController).
        /// </summary>
        public PopupHost(IPopupContent content, bool nonActivating)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            _content = content;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer,
                true);

            _content.VisualChanged += (s, e) => Invalidate();
            _content.CloseRequested += (s, e) => Close();
            _nonActivating = nonActivating;
        }

        protected override bool ShowWithoutActivation
        {
            get { return _nonActivating; }
        }

        /// <summary>
        /// Die zweite Hälfte von WS_EX_NOACTIVATE — ohne sie ist der Stil eine
        /// halbe Aussage. Auf WM_MOUSEACTIVATE antwortet ein Form von Haus aus
        /// mit MA_ACTIVATE, verlangt also seine eigene Aktivierung. Ein Fenster
        /// mit diesem Stil kann sie nie bekommen: Windows schluckt bei dem
        /// Versuch die auslösende Maustaste, und das Besitzerfenster meldet
        /// Deaktivierung — worauf der MenuController die Kette schließt. Für den
        /// Anwender sieht das so aus, als ginge das Menü beim Anklicken eines
        /// Eintrags nur zu, ohne ihn auszuführen.
        ///
        /// Am 2026-08-15 am Fensterverfahren nachgemessen: auf WM_MOUSEACTIVATE
        /// folgte unmittelbar WM_CLOSE, ein WM_LBUTTONDOWN traf nie ein. Mit
        /// MA_NOACTIVATE kommen Runter und Hoch normal an und der Eintrag läuft.
        ///
        /// Nur im nicht-aktivierenden Modus: Combo-Liste und Kalender aktivieren
        /// bewusst und behalten die Standardantwort.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEACTIVATE = 0x0021;

            if (_nonActivating && m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MouseActivate.NoActivate;
                return;
            }

            base.WndProc(ref m);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // WS_EX_NOACTIVATE: Mausklicks kommen an, aktivieren aber nicht.
                if (_nonActivating) cp.ExStyle |= 0x08000000;
                return cp;
            }
        }

        public void ShowPopup(IWin32Window owner, Point screenLocation, int minWidth)
        {
            Size size;
            using (var g = CreateGraphics())
                size = _content.Measure(g, DeviceDpi, minWidth);

            Bounds = new Rectangle(screenLocation, size);

            if (owner != null) Show(owner); else Show();
            Activate();
        }

        /// <summary>
        /// Zeigt an vorab berechneter Position und Größe — die Vermessung macht der
        /// Aufrufer selbst (Menü-Controller: die Platzierung braucht die Größe VOR
        /// dem Zeigen, und sie misst mit der DPI des Besitzer-Controls statt der
        /// des ungezeigten Popups — dieselbe dokumentierte Mixed-DPI-Grenze wie
        /// ShowPopup).
        /// </summary>
        public void ShowPopupAt(IWin32Window owner, Rectangle screenBounds)
        {
            // Handle ERST erzeugen, DANN Bounds setzen: Existiert das Handle noch
            // nicht, wandert die Breite in CreateWindowEx, und Windows klemmt dort
            // beim Erzeugen randlose, schmale Fenster auf seine System-Mindestbreite
            // (SM_CXMIN, gemessen ~136px bei 96 dpi) — unabhängig vom Aktivierungs-
            // Modus. Mit lebendem Handle läuft die Zuweisung stattdessen über
            // SetWindowPos, das diese Klemme nicht kennt, und die vorab berechneten
            // Bounds landen unverändert.
            _ = Handle;
            Bounds = screenBounds;
            if (owner != null) Show(owner); else Show();
            if (!_nonActivating) Activate();
        }

        public void ClosePopup()
        {
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            _content.Paint(e.Graphics, ClientRectangle, DeviceDpi);
            base.OnPaint(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _content.HandleMouseMove(e.Location);
            base.OnMouseMove(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            _content.HandleMouseClick(e.Location);
            base.OnMouseClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_content.HandleKey(e.KeyCode)) e.Handled = true;
            base.OnKeyDown(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Arrow keys are dialog-navigation keys; route them before Form consumes them.
            if (_content.HandleKey(keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            if (_nonActivating) { base.OnDeactivate(e); return; }

            base.OnDeactivate(e);

            // Close() HIER synchron aufzurufen zerreißt den WM_ACTIVATE-Handshake:
            // Windows aktiviert danach ein "falsches" Fenster (beobachtet: die
            // Besitzerform statt der tatsächlich angeklickten) und der auslösende
            // Klick geht verloren. Darum erst NACH dem Handshake schließen —
            // BeginInvoke braucht ein lebendes Handle.
            if (IsHandleCreated)
                BeginInvoke((MethodInvoker)DeferredClose);
            else
                DeferredClose();
        }

        private void DeferredClose()
        {
            // Zwischen dem Aufschieben und seiner Ausführung kann das Popup
            // längst anderweitig geschlossen/entsorgt worden sein (CloseRequested,
            // ClosePopup(), oder ein zweiter Deaktivierungs-Aufschub) — dann nicht
            // nochmal zugreifen.
            if (IsDisposed || Disposing) return;
            Close();
        }

        // ---- Nur für Tests --------------------------------------------------

        internal void RaiseDeactivateForTests()
        {
            OnDeactivate(EventArgs.Empty);
        }

        internal int ExStyleForTests
        {
            get { return CreateParams.ExStyle; }
        }

        /// <summary>Schickt ein WM_MOUSEACTIVATE durch das Fensterverfahren und
        /// liefert die Antwort — der Klick selbst lässt sich headless nicht
        /// nachstellen, die Antwort darauf schon.</summary>
        internal int MouseActivateResultForTests()
        {
            var m = Message.Create(Handle, 0x0021, IntPtr.Zero, IntPtr.Zero);
            WndProc(ref m);
            return m.Result.ToInt32();
        }
    }
}
