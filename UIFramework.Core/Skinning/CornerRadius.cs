using System;

namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Vier Eckradien in logischen Einheiten (96-DPI-Basis).
    /// Die Skalierung passiert erst beim Zeichnen.
    /// </summary>
    public struct CornerRadius : IEquatable<CornerRadius>
    {
        public static readonly CornerRadius None = new CornerRadius(0);

        public int TopLeft { get; }
        public int TopRight { get; }
        public int BottomRight { get; }
        public int BottomLeft { get; }

        public CornerRadius(int all) : this(all, all, all, all)
        {
        }

        public CornerRadius(int topLeft, int topRight, int bottomRight, int bottomLeft)
        {
            if (topLeft < 0)
                throw new ArgumentOutOfRangeException(nameof(topLeft), "Eckradien dürfen nicht negativ sein.");
            if (topRight < 0)
                throw new ArgumentOutOfRangeException(nameof(topRight), "Eckradien dürfen nicht negativ sein.");
            if (bottomRight < 0)
                throw new ArgumentOutOfRangeException(nameof(bottomRight), "Eckradien dürfen nicht negativ sein.");
            if (bottomLeft < 0)
                throw new ArgumentOutOfRangeException(nameof(bottomLeft), "Eckradien dürfen nicht negativ sein.");

            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        public bool IsZero
        {
            get { return TopLeft == 0 && TopRight == 0 && BottomRight == 0 && BottomLeft == 0; }
        }

        public bool Equals(CornerRadius other)
        {
            return TopLeft == other.TopLeft
                && TopRight == other.TopRight
                && BottomRight == other.BottomRight
                && BottomLeft == other.BottomLeft;
        }

        public override bool Equals(object obj)
        {
            return obj is CornerRadius && Equals((CornerRadius)obj);
        }

        /// <summary>
        /// Bei einem IEquatable-Wertetyp idiomatisch. Reicht auf Equals durch —
        /// ein zweites Vergleichsverhalten wäre eine Fehlerquelle, kein Dienst.
        /// </summary>
        public static bool operator ==(CornerRadius left, CornerRadius right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CornerRadius left, CornerRadius right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = TopLeft;
                hash = (hash * 397) ^ TopRight;
                hash = (hash * 397) ^ BottomRight;
                hash = (hash * 397) ^ BottomLeft;
                return hash;
            }
        }
    }
}
