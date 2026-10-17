using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Einzeilige (optional mehrzeilige) Texteingabe — die schlichteste
    /// Ausprägung der ButtonEditBase: null Knöpfe, nur der native Kern.
    /// Öffentliche API unverändert seit Teilprojekt 1.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("TextChanged")]
    public class SkinTextBox : ButtonEditBase
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private const int EM_SETCUEBANNER = 0x1501;

        private string _placeholderText = "";

        protected override string ElementKey
        {
            get { return ElementKeys.TextBox; }
        }

        /// <summary>Nur für Tests: pixelgenaue Prüfung der inneren Textbox
        /// ohne die Sichtbarkeit über DrawToBitmap zu erzwingen (deren Fläche
        /// wird vom nativen Kind-Fenster verdeckt und ist so nicht prüfbar).</summary>
        internal System.Windows.Forms.TextBox InnerTextBoxForTests
        {
            get { return InnerTextBox; }
        }

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return InnerTextBox.Text; }
            set { InnerTextBox.Text = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return InnerTextBox.ReadOnly; }
            set { InnerTextBox.ReadOnly = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool Multiline
        {
            get { return InnerTextBox.Multiline; }
            set { InnerTextBox.Multiline = value; }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get { return _placeholderText; }
            set
            {
                string next = value ?? "";
                if (_placeholderText == next) return;
                _placeholderText = next;
                ApplyPlaceholder();
            }
        }

        protected override void OnInnerHandleCreated()
        {
            ApplyPlaceholder();
        }

        private void ApplyPlaceholder()
        {
            if (InnerTextBox.IsHandleCreated)
                SendMessage(InnerTextBox.Handle, EM_SETCUEBANNER, IntPtr.Zero, _placeholderText);
        }
    }
}
