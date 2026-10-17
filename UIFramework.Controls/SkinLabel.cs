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
    /// Beschriftung. Prüfstand für die Textmetrik unter DPI-Skalierung.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Text")]
    public class SkinLabel : SkinnedControl
    {
        private ContentAlignment _textAlignment = ContentAlignment.MiddleLeft;

        public SkinLabel()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            AutoSize = true;
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Label; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        [Category("Appearance")]
        [DefaultValue(ContentAlignment.MiddleLeft)]
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

        [DefaultValue(true)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set { base.AutoSize = value; }
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            if (string.IsNullOrEmpty(Text)) return;

            // Padding wird nicht hier gerechnet, sondern im Painter über DpiScale:
            // Controls dürfen selbst nicht skalieren.
            SkinPainter.DrawPaddedText(g, Text, ClientRectangle, appearance, DeviceDpi, _textAlignment);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var appearance = CurrentAppearance;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                // Auch bei leerem Text eine Zeilenhöhe messen: sonst kollabiert das
                // Label auf null Höhe und das Layout springt, sobald Text hineinkommt.
                string measured = string.IsNullOrEmpty(Text) ? "Xg" : Text;
                var textSize = SkinPainter.MeasureText(g, measured, appearance, DeviceDpi);

                if (string.IsNullOrEmpty(Text)) textSize.Width = 0;

                // Das Padding wird im Painter addiert, nicht hier: Controls dürfen
                // selbst nicht mit DpiScale rechnen (siehe SkinPainter.InflateByPadding).
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
    }
}
