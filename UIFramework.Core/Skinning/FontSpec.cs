using System;
using System.Drawing;

namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Beschreibt eine Schrift, ohne eine zu sein. Bewusst kein System.Drawing.Font:
    /// der ist IDisposable und hat eine feste Pixelgröße — in einer serialisierbaren
    /// Skin-Datenstruktur wäre er sowohl ein Leck als auch DPI-blind.
    /// Der echte Font entsteht erst im ResourceCache, DPI-korrekt.
    /// </summary>
    public struct FontSpec : IEquatable<FontSpec>
    {
        public string Family { get; }
        public float SizeInPoints { get; }
        public FontStyle Style { get; }

        public FontSpec(string family, float sizeInPoints, FontStyle style = FontStyle.Regular)
        {
            if (string.IsNullOrWhiteSpace(family))
                throw new ArgumentException("Die Schriftfamilie darf nicht leer sein.", nameof(family));
            // Negiert statt "<= 0f": NaN vergleicht mit jeder Zahl als falsch,
            // "sizeInPoints <= 0f" liesse float.NaN also unbemerkt durch.
            if (!(sizeInPoints > 0f))
                throw new ArgumentOutOfRangeException(nameof(sizeInPoints), "Die Schriftgröße muss positiv sein.");

            Family = family;
            SizeInPoints = sizeInPoints;
            Style = style;
        }

        public bool Equals(FontSpec other)
        {
            return string.Equals(Family, other.Family, StringComparison.Ordinal)
                && SizeInPoints.Equals(other.SizeInPoints)
                && Style == other.Style;
        }

        public override bool Equals(object obj)
        {
            return obj is FontSpec && Equals((FontSpec)obj);
        }

        /// <summary>
        /// Bei einem IEquatable-Wertetyp idiomatisch. Reicht auf Equals durch —
        /// ein zweites Vergleichsverhalten wäre eine Fehlerquelle, kein Dienst.
        /// </summary>
        public static bool operator ==(FontSpec left, FontSpec right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FontSpec left, FontSpec right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Family == null ? 0 : StringComparer.Ordinal.GetHashCode(Family);
                hash = (hash * 397) ^ SizeInPoints.GetHashCode();
                hash = (hash * 397) ^ (int)Style;
                return hash;
            }
        }

        public override string ToString()
        {
            return Family + " " + SizeInPoints.ToString("0.##") + "pt " + Style;
        }
    }
}
