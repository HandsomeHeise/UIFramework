using System;

namespace UIFramework.Controls
{
    /// <summary>
    /// Die gesamte Rechnung einer Bildlaufleiste — ohne Fenster, ohne Graphics,
    /// ohne Richtung. Sie kennt nur Längen entlang einer Achse; welche Achse das
    /// ist, entscheidet SkinScrollBar. Genau deshalb ist sie kopflos prüfbar,
    /// und genau deshalb prüfen dieselben Tests beide Richtungen.
    ///
    /// Übernimmt die WinForms-Konvention: Der größte erreichbare Wert ist
    /// Maximum - LargeChange + 1, nicht Maximum. LargeChange ist die Größe des
    /// sichtbaren Ausschnitts.
    /// </summary>
    public struct ScrollBarGeometry
    {
        private readonly int _trackLength;
        private readonly int _minimum;
        private readonly int _range;        // erreichbare Spanne: max(0, Maximum - LargeChange + 1 - Minimum)
        private readonly int _value;
        private readonly int _thumbLength;

        public ScrollBarGeometry(int trackLength, int minimum, int maximum, int value,
                                 int largeChange, int minThumbLength)
        {
            if (trackLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(trackLength), "Die Rinne muss eine positive Länge haben.");
            if (largeChange <= 0)
                throw new ArgumentOutOfRangeException(nameof(largeChange), "LargeChange muss positiv sein.");
            if (minThumbLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(minThumbLength), "Die Mindestlänge des Daumens muss positiv sein.");

            _trackLength = trackLength;
            _minimum = minimum;

            int span = maximum - minimum;                       // Gesamtinhalt
            int reachable = maximum - largeChange + 1 - minimum; // erreichbare Werte
            _range = reachable > 0 ? reachable : 0;

            _value = ClampValue(value, minimum, maximum, largeChange);

            if (_range == 0)
            {
                // Alles sichtbar: der Daumen füllt die Rinne. Kein Sonderfall
                // im Zeichenpfad nötig, keine Division durch null.
                _thumbLength = trackLength;
                return;
            }

            // Anteil des Sichtbaren am Ganzen. span > 0 ist hier sicher, denn
            // _range > 0 bedeutet maximum - largeChange + 1 > minimum, also
            // span = maximum - minimum > largeChange - 1 >= 0.
            long proportional = (long)trackLength * largeChange / span;

            int length = (int)proportional;
            if (length < minThumbLength) length = minThumbLength;
            if (length > trackLength) length = trackLength;

            _thumbLength = length;
        }

        /// <summary>Ob es überhaupt etwas zu scrollen gibt.</summary>
        public bool IsScrollable
        {
            get { return _range > 0; }
        }

        public int ThumbLength
        {
            get { return _thumbLength; }
        }

        /// <summary>Abstand des Daumens vom Anfang der Rinne.</summary>
        public int ThumbOffset
        {
            get
            {
                int free = FreeTrack;
                if (free <= 0 || _range == 0) return 0;

                // Kaufmännisch runden wie DpiScale: sonst erreicht der Daumen am
                // Ende die Rinnenkante nicht ganz, und das sieht falsch aus.
                return (int)Math.Round((double)(_value - _minimum) * free / _range,
                                       MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>Eine Position in der Rinne zurück in einen Wert — für das Ziehen.</summary>
        public int ValueAt(int offset)
        {
            int free = FreeTrack;
            if (free <= 0 || _range == 0) return _minimum;

            if (offset <= 0) return _minimum;
            if (offset >= free) return _minimum + _range;

            return _minimum + (int)Math.Round((double)offset * _range / free,
                                              MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Die Strecke, die der Daumen zurücklegen kann. Nie negativ — bei einer
        /// Rinne kürzer als der Daumen wäre die Division in ValueAt sonst kaputt.
        /// </summary>
        private int FreeTrack
        {
            get
            {
                int free = _trackLength - _thumbLength;
                return free > 0 ? free : 0;
            }
        }

        /// <summary>
        /// Klemmt einen Wert auf den erreichbaren Bereich. Statisch, weil das
        /// Control es auch braucht, ohne eine Geometrie zu bauen.
        /// </summary>
        public static int ClampValue(int value, int minimum, int maximum, int largeChange)
        {
            int highest = maximum - largeChange + 1;
            if (highest < minimum) highest = minimum;

            if (value < minimum) return minimum;
            if (value > highest) return highest;
            return value;
        }
    }
}
