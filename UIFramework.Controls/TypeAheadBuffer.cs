using System;
using System.Text;

namespace UIFramework.Controls
{
    /// <summary>
    /// Tippen zum Springen wie im Windows Explorer — als reine Klasse, damit
    /// Grid und Baum dieselbe Semantik teilen und sie ohne Fenster und ohne
    /// Uhr prüfbar ist (die Zeit kommt als Parameter).
    ///
    /// Zwei Regeln: (1) Nach mehr als <see cref="ResetMilliseconds"/> Pause
    /// beginnt die Eingabe neu. (2) Lauter gleiche Zeichen ("b", "bbb") sind
    /// ein Zyklus über die Treffer dieses Buchstabens — gesucht wird AB der
    /// Zeile NACH der aktuellen; jedes andere Präfix verfeinert und sucht AB
    /// der aktuellen Zeile einschließlich, damit "be" auf "Beta" stehen bleibt.
    /// Ohne aktuelle Zeile (current &lt; 0) beginnt beides bei Zeile 0 — sonst
    /// übersprünge der erste Tastendruck den ersten Treffer.
    /// </summary>
    public sealed class TypeAheadBuffer
    {
        public const int ResetMilliseconds = 1000;

        private readonly StringBuilder _typed = new StringBuilder();
        private long _lastMilliseconds;

        /// <summary>Was seit dem letzten Neubeginn getippt wurde.</summary>
        public string Typed
        {
            get { return _typed.ToString(); }
        }

        public void Append(char c, long nowMilliseconds)
        {
            if (_typed.Length > 0 && nowMilliseconds - _lastMilliseconds > ResetMilliseconds)
                _typed.Clear();

            _typed.Append(c);
            _lastMilliseconds = nowMilliseconds;
        }

        public void Reset()
        {
            _typed.Clear();
        }

        /// <summary>
        /// Die nächste passende Zeile, mit Umlauf; -1 ohne Treffer. textAt darf
        /// null liefern (Zeilen ohne Text, etwa Platzhalter im Baum) — die
        /// werden übersprungen. Liest im schlimmsten Fall jede Zeile einmal:
        /// der Preis einer Suche, einmal je Tastendruck, nie je Bild.
        /// </summary>
        public int FindMatch(int count, int current, Func<int, string> textAt)
        {
            if (textAt == null) throw new ArgumentNullException(nameof(textAt));
            if (_typed.Length == 0 || count <= 0) return -1;

            string typed = _typed.ToString();
            bool cycling = AllSame(typed);
            string prefix = cycling ? typed.Substring(0, 1) : typed;

            int start;
            if (current < 0) start = 0;
            else start = cycling ? current + 1 : current;

            for (int n = 0; n < count; n++)
            {
                int index = (start + n) % count;
                string text = textAt(index);
                if (text != null && text.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
                    return index;
            }

            return -1;
        }

        private static bool AllSame(string text)
        {
            for (int i = 1; i < text.Length; i++)
                if (char.ToUpperInvariant(text[i]) != char.ToUpperInvariant(text[0])) return false;
            return true;
        }
    }
}
