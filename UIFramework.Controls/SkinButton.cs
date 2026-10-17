using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Schaltfläche. Prüfstand für die Zustandsmaschine der Basisklasse.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("Click")]
    public class SkinButton : SkinnedControl
    {
        private ContentAlignment _textAlignment = ContentAlignment.MiddleCenter;

        public SkinButton()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(96, 30);
            AutoSize = true;
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Button; }
        }

        /// <summary>
        /// Wenn gesetzt (Standard, wie bei <see cref="SkinLabel.AutoSize"/>),
        /// richtet sich die Größe nach dem tatsächlichen Textbedarf (siehe
        /// <see cref="GetPreferredSize"/>) statt der festen 96×30-Vorgabe aus dem
        /// Konstruktor. Ohne das schneidet der Text mit TextFormatFlags.EndEllipsis
        /// ab, sobald er breiter ist als die feste Größe — insbesondere oberhalb von
        /// 96 dpi, wo die Schrift wächst, aber eine feste Größe nicht mitwüchse. Wer
        /// bewusst eine feste Größe braucht, schaltet AutoSize gezielt aus.
        /// </summary>
        [Category("Layout")]
        [DefaultValue(true)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set
            {
                if (base.AutoSize == value) return;
                base.AutoSize = value;
                if (value) AdjustSize();
            }
        }

        [Category("Appearance")]
        [DefaultValue(ContentAlignment.MiddleCenter)]
        public ContentAlignment TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                if (_textAlignment == value) return;
                _textAlignment = value;
                Invalidate();
            }
        }

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            if (string.IsNullOrEmpty(Text)) return;

            // Padding wird nicht hier gerechnet, sondern im Painter über DpiScale:
            // Controls dürfen selbst nicht skalieren.
            SkinPainter.DrawPaddedText(g, Text, ClientRectangle, appearance, DeviceDpi, _textAlignment);
        }

        /// <summary>
        /// Spiegelt <see cref="SkinLabel.GetPreferredSize"/>: misst den Text über
        /// den Painter und polstert ihn um das (DPI-skalierte) Padding des
        /// Appearance auf. Die DpiScale-Arithmetik bleibt dabei im Painter — hier
        /// wird nur DeviceDpi durchgereicht (siehe SkinPainter.InflateByPadding).
        /// </summary>
        public override Size GetPreferredSize(Size proposedSize)
        {
            var appearance = CurrentAppearance;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                string measured = string.IsNullOrEmpty(Text) ? "Xg" : Text;
                var textSize = SkinPainter.MeasureText(g, measured, appearance, DeviceDpi);

                if (string.IsNullOrEmpty(Text)) textSize.Width = 0;

                return SkinPainter.InflateByPadding(textSize, appearance, DeviceDpi);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            if (AutoSize) AdjustSize();
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            if (AutoSize) AdjustSize();
            base.OnDpiChangedAfterParent(e);
        }

        private void AdjustSize()
        {
            Size = GetPreferredSize(Size.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && CanFocus) Focus();
            base.OnMouseDown(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                PerformClick();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        public void PerformClick()
        {
            if (!Enabled) return;
            OnClick(EventArgs.Empty);
        }
    }
}
