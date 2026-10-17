using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Core.Controls
{
    /// <summary>
    /// Basisklasse aller Controls des Frameworks.
    ///
    /// Kennt keinen einzigen Farbwert: sie errechnet den Zustand, holt das
    /// Erscheinungsbild beim aktiven Skin und reicht es an den Painter weiter.
    ///
    /// Designer-tauglich: parameterloser Konstruktor, nichts Werfendes darin,
    /// und der SkinManager liefert auch im Designmodus einen Skin.
    /// </summary>
    public abstract class SkinnedControl : Control
    {
        private bool _hovered;
        private bool _pressed;

        protected SkinnedControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            // Runde Ecken lassen vier Eckenquadrate frei, die der Painter nie
            // füllt. Ohne dies würde Control.OnPaintBackground sie mit einer
            // undurchsichtigen BackColor überstreichen (Systemgrau). Transparent
            // reicht das Malen des Hintergrunds an den Elternteil weiter, sodass
            // dort der wirkliche Elternhintergrund erscheint.
            BackColor = Color.Transparent;

            SkinManager.Register(this);
        }

        /// <summary>Der Schlüssel, unter dem dieses Control im Skin steht.</summary>
        protected abstract string ElementKey { get; }

        /// <summary>Überschreiben, wenn das Control einen Auswahlzustand kennt.</summary>
        protected virtual bool IsSelected
        {
            get { return false; }
        }

        protected virtual bool ShowFocusRing
        {
            get { return true; }
        }

        /// <summary>
        /// Die Rangfolge lebt hier und nirgends sonst.
        /// Der Fokus gehört bewusst nicht dazu — er wird überlagert gezeichnet.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ElementState State
        {
            get
            {
                if (!Enabled) return ElementState.Disabled;
                if (_pressed) return ElementState.Pressed;
                if (_hovered) return ElementState.Hovered;
                if (IsSelected) return ElementState.Selected;
                return ElementState.Normal;
            }
        }

        protected ElementAppearance CurrentAppearance
        {
            get { return SkinManager.Current.GetAppearance(ElementKey, State); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var appearance = CurrentAppearance;
            int dpi = DeviceDpi;

            SkinPainter.DrawBackground(e.Graphics, ClientRectangle, appearance, dpi);
            SkinPainter.DrawBorder(e.Graphics, ClientRectangle, appearance, dpi);

            PaintContent(e.Graphics, appearance);

            if (ShowFocusRing && Focused)
            {
                var focus = SkinManager.Current.GetAppearance(ElementKeys.Focus, ElementState.Normal);
                SkinPainter.DrawFocus(e.Graphics, ClientRectangle, focus, dpi);
            }

            base.OnPaint(e);
        }

        /// <summary>
        /// Hintergrund und Rahmen sind schon gezeichnet. Hier kommt der Inhalt hin.
        /// </summary>
        protected virtual void PaintContent(Graphics g, ElementAppearance appearance)
        {
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;

            // Ohne das bliebe das Control für immer gedrückt, wenn der Anwender
            // mit gehaltener Taste hinauszieht und außerhalb loslässt.
            _pressed = false;

            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = false;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            if (!Enabled)
            {
                _hovered = false;
                _pressed = false;
            }
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            // Der Skin ist in logischen Einheiten formuliert und bleibt gültig —
            // nur die gezeichneten Pixel müssen neu entstehen.
            Invalidate();
            base.OnDpiChangedAfterParent(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SkinManager.Unregister(this);
            }
            base.Dispose(disposing);
        }
    }
}
