using System;
using System.ComponentModel;
using System.Globalization;
using UIFramework.Controls.Editing;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Datumseingabe: Kurzdatum der aktiven Culture, leer erlaubt (null),
    /// Unparsbares fällt still zurück. Der Kalender-Knopf öffnet den
    /// Monatskalender im PopupHost.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("ValueChanged")]
    public class DateEdit : ButtonEditBase
    {
        private DateTime? _value;

        public DateEdit()
        {
            AddButton(EditorGlyph.Calendar, ToggleCalendar);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.TextBox; }
        }

        [Category("Behavior")]
        public DateTime? Value
        {
            get { return _value; }
            set
            {
                DateTime? next = value.HasValue ? value.Value.Date : (DateTime?)null;
                InnerTextBox.Text = DateBehavior.Format(next, CultureInfo.CurrentCulture);
                if (_value == next) return;

                _value = next;
                OnValueChanged(EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;

        protected virtual void OnValueChanged(EventArgs e)
        {
            var handler = ValueChanged;
            if (handler != null) handler(this, e);
        }

        protected override void OnEditConfirmed()
        {
            // Value schreibt den formatierten Text bereits selbst (auch bei
            // unverändertem Wert) — ein zweiter Write hier wäre nur Duplikat.
            Value = DateBehavior.ParseOrFallback(InnerTextBox.Text, _value, CultureInfo.CurrentCulture);
        }

        protected override object GetEditValue()
        {
            DateTime? parsed = DateBehavior.ParseOrFallback(InnerTextBox.Text, _value, CultureInfo.CurrentCulture);
            return parsed.HasValue ? (object)parsed.Value : null;
        }

        protected override void SetEditValue(object value)
        {
            if (value is DateTime dt) { Value = dt; return; }
            if (value == null) { Value = null; return; }
            Value = DateBehavior.ParseOrFallback(value.ToString(), _value, CultureInfo.CurrentCulture);
        }

        private void ToggleCalendar()
        {
            // Derselbe MouseUp-Schnappschuss wie SkinComboBox.Toggle — siehe
            // dortigen Kommentar.
            if (IsPopupOpen || PopupWasOpenAtMouseDown)
            {
                ClosePopup();
                return;
            }

            // Ungespeicherten Text zuerst bestätigen — sonst öffnet der
            // Kalender auf dem alten Monat, obwohl gerade ein anderes Datum
            // getippt wurde (dasselbe Muster wie SpinEdit.Step).
            Value = DateBehavior.ParseOrFallback(InnerTextBox.Text, _value, CultureInfo.CurrentCulture);

            var month = _value ?? DateTime.Today;
            var calendar = new CalendarContent(month, _value, CultureInfo.CurrentCulture);
            calendar.DateChosen += date => { Value = date; };
            OpenPopup(calendar);
        }

        // ---- Nur für Tests --------------------------------------------------

        internal void SetTextForTests(string text) { InnerTextBox.Text = text; }
        internal string TextForTests() { return InnerTextBox.Text; }
        internal string FormatForTests(DateTime? value)
        {
            return DateBehavior.Format(value, CultureInfo.CurrentCulture);
        }
    }
}
