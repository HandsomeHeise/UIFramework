using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>Die drei Flächen eines geteilten Containers, in Client-Koordinaten.</summary>
    internal struct SplitGeometry
    {
        public Rectangle Panel1;
        public Rectangle Splitter;
        public Rectangle Panel2;
    }

    /// <summary>
    /// Reine Geometrie des SkinSplitContainer: keine GDI-Aufrufe, kein DPI,
    /// kein Skin — alle Werte kommen physisch herein (Muster RibbonLayout).
    /// "distance" ist immer die Größe von Panel1 entlang der Teilungsachse.
    /// </summary>
    internal static class SplitLayout
    {
        /// <summary>
        /// Klemmt die Entfernung zwischen die Mindestgrößen. Reicht der Platz
        /// für beide nicht, gewinnt Panel1 — im Explorer ist das der Baum, der
        /// sichtbar bleiben soll. Nie außerhalb des Clients, nie negativ.
        /// </summary>
        public static int ClampDistance(int distance, int total, int splitter, int min1, int min2)
        {
            int d = distance;
            int upper = total - splitter - min2;
            if (d > upper) d = upper;
            if (d < min1) d = min1;
            if (d > total - splitter) d = total - splitter;
            if (d < 0) d = 0;
            return d;
        }

        public static SplitGeometry Arrange(Size client, Orientation orientation, int distance, int splitter)
        {
            if (orientation == Orientation.Vertical)
            {
                return new SplitGeometry
                {
                    Panel1 = new Rectangle(0, 0, distance, client.Height),
                    Splitter = new Rectangle(distance, 0, splitter, client.Height),
                    Panel2 = new Rectangle(distance + splitter, 0,
                        System.Math.Max(0, client.Width - distance - splitter), client.Height)
                };
            }

            return new SplitGeometry
            {
                Panel1 = new Rectangle(0, 0, client.Width, distance),
                Splitter = new Rectangle(0, distance, client.Width, splitter),
                Panel2 = new Rectangle(0, distance + splitter, client.Width,
                    System.Math.Max(0, client.Height - distance - splitter))
            };
        }

        /// <summary>
        /// Wie sich die Entfernung bei einer Größenänderung verschiebt:
        /// Panel1 fest → gar nicht; Panel2 fest → um den Zuwachs; keins fest →
        /// verhältnisgleich. Ohne alte Gesamtgröße bleibt alles, wie es ist.
        /// </summary>
        public static int DistanceAfterResize(int distance, int oldTotal, int newTotal, FixedPanel fixedPanel)
        {
            if (oldTotal <= 0) return distance;

            switch (fixedPanel)
            {
                case FixedPanel.Panel1:
                    return distance;
                case FixedPanel.Panel2:
                    return distance + (newTotal - oldTotal);
                default:
                    return (int)System.Math.Round((double)distance * newTotal / oldTotal, System.MidpointRounding.AwayFromZero);
            }
        }
    }
}
