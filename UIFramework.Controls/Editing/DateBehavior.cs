using System;
using System.Globalization;

namespace UIFramework.Controls.Editing
{
    /// <summary>
    /// Wert↔Text des DateEdit: Kurzdatum der Culture, leer ist ein erlaubter
    /// Wert (null), Unparsbares fällt still auf den letzten gültigen Wert
    /// zurück (Spec-Entscheidung: kein Fehlerzustand, keine Eingabemaske).
    /// </summary>
    public static class DateBehavior
    {
        public static DateTime? ParseOrFallback(string text, DateTime? fallback, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            DateTime parsed;
            if (DateTime.TryParse(text, culture, DateTimeStyles.None, out parsed))
                return parsed.Date;

            return fallback;
        }

        public static string Format(DateTime? value, CultureInfo culture)
        {
            return value.HasValue ? value.Value.ToString("d", culture) : "";
        }
    }
}
