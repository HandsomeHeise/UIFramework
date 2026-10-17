using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using UIFramework.Controls.Editing;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Zahleneingabe mit Auf/Ab-Knöpfen, Pfeiltasten und Mausrad. Die gesamte
    /// Wertlogik (Klemmen, Parsen mit stillem Rückfall, Tastenfilter) liegt in
    /// SpinBehavior — dieses Control verdrahtet sie nur.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("ValueChanged")]
    public class SpinEdit : ButtonEditBase
    {
        private decimal _value;
        private decimal _minValue;
        private decimal _maxValue = 100;
        private decimal _increment = 1;

        public SpinEdit()
        {
            AddButton(EditorGlyph.ArrowUp, () => Step(+1));     // Knopf 0: ganz rechts
            AddButton(EditorGlyph.ArrowDown, () => Step(-1));

            InnerTextBox.Text = FormatValue(_value);
            InnerTextBox.KeyDown += HandleInnerKeyDown;
            InnerTextBox.MouseWheel += HandleInnerMouseWheel;
        }

        protected override string ElementKey
        {
            get { return ElementKeys.TextBox; }
        }

        [Category("Behavior")]
        public decimal Value
        {
            get { return _value; }
            set
            {
                decimal clamped = SpinBehavior.Clamp(value, _minValue, _maxValue);
                InnerTextBox.Text = FormatValue(clamped);   // Text folgt IMMER, auch bei gleichem Wert
                if (_value == clamped) return;

                _value = clamped;
                OnValueChanged(EventArgs.Empty);
            }
        }

        [Category("Behavior")]
        [DefaultValue(typeof(decimal), "0")]
        public decimal MinValue
        {
            get { return _minValue; }
            set
            {
                // Wurf statt stillem Nachziehen: Eine App, die Min über Max setzt,
                // hat einen Fehler — den zu verstecken hieße, ihn zu verewigen.
                if (value > _maxValue)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "MinValue (" + value + ") darf MaxValue (" + _maxValue + ") nicht übersteigen.");
                _minValue = value;
                Value = _value;
            }
        }

        [Category("Behavior")]
        [DefaultValue(typeof(decimal), "100")]
        public decimal MaxValue
        {
            get { return _maxValue; }
            set
            {
                if (value < _minValue)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "MaxValue (" + value + ") darf MinValue (" + _minValue + ") nicht unterschreiten.");
                _maxValue = value;
                Value = _value;
            }
        }

        [Category("Behavior")]
        [DefaultValue(typeof(decimal), "1")]
        public decimal Increment
        {
            get { return _increment; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Increment muss positiv sein; " + value + " würde die Knöpfe lahmlegen oder umkehren.");
                _increment = value;
            }
        }

        public event EventHandler ValueChanged;

        protected virtual void OnValueChanged(EventArgs e)
        {
            var handler = ValueChanged;
            if (handler != null) handler(this, e);
        }

        private void Step(int direction)
        {
            // Erst den gerade getippten, noch unbestätigten Text in einen Wert
            // umrechnen — sonst rechnet der Schritt mit dem alten _value und
            // die Eingabe verschwindet kommentarlos (wer "50" tippt und dann
            // klickt, statt vom getippten Wert aus zu steppen). Bewusst NUR
            // EINE Zuweisung an Value: zwei sequenzielle Writes (erst der
            // Commit, dann der Schritt) würden ValueChanged zweimal feuern,
            // einmal mit dem nie angeforderten Zwischenwert. Value klemmt den
            // fertigen Endwert wie bisher selbst.
            decimal confirmed = SpinBehavior.ParseOrFallback(
                InnerTextBox.Text, _value, _minValue, _maxValue, CultureInfo.CurrentCulture);
            Value = confirmed + direction * _increment;
        }

        private void HandleInnerKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up) { Step(+1); e.Handled = true; }
            else if (e.KeyCode == Keys.Down) { Step(-1); e.Handled = true; }
        }

        private void HandleInnerMouseWheel(object sender, MouseEventArgs e)
        {
            Step(e.Delta > 0 ? +1 : -1);
            ((HandledMouseEventArgs)e).Handled = true;   // Rad soll nicht zusätzlich scrollen
        }

        protected override bool IsCharAllowed(char c)
        {
            return SpinBehavior.IsCharAllowed(c, CultureInfo.CurrentCulture);
        }

        protected override void OnEditConfirmed()
        {
            Value = SpinBehavior.ParseOrFallback(
                InnerTextBox.Text, _value, _minValue, _maxValue, CultureInfo.CurrentCulture);
        }

        /// <summary>Liest parse-auf-Sicht: der getippte, noch unbestätigte Text
        /// zählt, der Control-Zustand bleibt unangetastet (Entscheidung 8 des
        /// 3b-Plans — der Zwangs-Commit liest ohne Handshake).</summary>
        protected override object GetEditValue()
        {
            return SpinBehavior.ParseOrFallback(
                InnerTextBox.Text, _value, _minValue, _maxValue, CultureInfo.CurrentCulture);
        }

        protected override void SetEditValue(object value)
        {
            Value = value is decimal d
                ? d
                : SpinBehavior.ParseOrFallback(
                    value == null ? "" : value.ToString(), _value, _minValue, _maxValue, CultureInfo.CurrentCulture);
        }

        private static string FormatValue(decimal value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        // ---- Nur für Tests --------------------------------------------------

        internal void SetTextForTests(string text)
        {
            InnerTextBox.Text = text;
        }

        internal string TextForTests()
        {
            return InnerTextBox.Text;
        }

        /// <summary>Simuliert eine Pfeiltaste im nativen Kern, ohne echten
        /// Tastatur-/Fokus-Umweg (Muster: CheckEdit.PerformKey).</summary>
        internal void PressArrowKeyForTests(Keys key)
        {
            var e = new KeyEventArgs(key);
            HandleInnerKeyDown(InnerTextBox, e);
        }

        /// <summary>Simuliert Mausrad im nativen Kern; delta &gt; 0 = hoch.</summary>
        internal void SpinByWheelForTests(int delta)
        {
            var e = new HandledMouseEventArgs(MouseButtons.None, 0, 0, 0, delta);
            HandleInnerMouseWheel(InnerTextBox, e);
        }
    }
}
