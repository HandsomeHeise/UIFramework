using System;
using System.Windows.Forms;
using UIFramework.Core.Skinning;

namespace UIFramework.Core.Dpi
{
    /// <summary>
    /// Rechnet logische Maße (96-DPI-Basis) in physische Pixel um.
    /// Der Skin ist durchgängig in logischen Einheiten formuliert; genau deshalb
    /// überlebt er einen Monitorwechsel unverändert.
    /// </summary>
    public static class DpiScale
    {
        public const int BaseDpi = 96;

        /// <summary>
        /// Kaufmännisches Runden ist hier Absicht: ein 1px-Rahmen wird bei 150 %
        /// zu 2px statt zu 1px. Abrunden würde Rahmen bei krummen Skalierungen
        /// gelegentlich verschwinden lassen.
        /// </summary>
        public static int Scale(int logical, int dpi)
        {
            if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi), "DPI muss positiv sein.");
            if (dpi == BaseDpi) return logical;

            return (int)Math.Round(logical * (double)dpi / BaseDpi, MidpointRounding.AwayFromZero);
        }

        public static float ScaleF(float logical, int dpi)
        {
            if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi), "DPI muss positiv sein.");
            if (dpi == BaseDpi) return logical;

            return logical * dpi / BaseDpi;
        }

        public static Padding Scale(Padding logical, int dpi)
        {
            return new Padding(
                Scale(logical.Left, dpi),
                Scale(logical.Top, dpi),
                Scale(logical.Right, dpi),
                Scale(logical.Bottom, dpi));
        }

        public static CornerRadius Scale(CornerRadius logical, int dpi)
        {
            return new CornerRadius(
                Scale(logical.TopLeft, dpi),
                Scale(logical.TopRight, dpi),
                Scale(logical.BottomRight, dpi),
                Scale(logical.BottomLeft, dpi));
        }

        /// <summary>
        /// Ein Punkt ist 1/72 Zoll — also über 72 umrechnen, nicht über 96.
        /// Der so berechnete Wert wird mit GraphicsUnit.Pixel an den Font gegeben,
        /// damit GDI+ nicht ein zweites Mal skaliert.
        /// </summary>
        public static float PointsToPixels(float points, int dpi)
        {
            if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi), "DPI muss positiv sein.");

            return points * dpi / 72f;
        }
    }
}
