using System.Globalization;

namespace UIFramework.Controls.Editing
{
    /// <summary>
    /// Die Wert↔Text-Logik des SpinEdit als reine Klasse — kein Control, kein
    /// Graphics, kopflos prüfbar und in 3b im Grid wiederverwendbar.
    ///
    /// Ungültige Eingabe fällt STILL auf den letzten gültigen Wert zurück
    /// (Spec-Entscheidung: kein Fehlerzustand, kein Validierungs-Ereignis).
    /// </summary>
    public static class SpinBehavior
    {
        public static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        public static decimal ParseOrFallback(string text, decimal fallback, decimal min, decimal max, CultureInfo culture)
        {
            decimal parsed;
            if (decimal.TryParse(text, NumberStyles.Number, culture, out parsed))
                return Clamp(parsed, min, max);

            // Auch der Rückfallwert wird geklemmt: MinValue kann seit der
            // letzten gültigen Eingabe angehoben worden sein.
            return Clamp(fallback, min, max);
        }

        public static bool IsCharAllowed(char c, CultureInfo culture)
        {
            if (char.IsDigit(c)) return true;
            if (c == '-' || c == '+') return true;
            return culture.NumberFormat.NumberDecimalSeparator.IndexOf(c) >= 0;
        }
    }
}
