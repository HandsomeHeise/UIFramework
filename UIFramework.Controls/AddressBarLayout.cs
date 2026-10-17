using System;
using System.Collections.Generic;
using System.Drawing;

namespace UIFramework.Controls
{
    /// <summary>Ein platzierter Kasten der Adressleiste. SegmentIndex -1 = das
    /// Überlauf-Segment «, das die vorderen Segmente vertritt.</summary>
    internal struct AddressBox
    {
        public int SegmentIndex;
        public Rectangle Bounds;
    }

    internal sealed class AddressArrangement
    {
        public AddressBox[] Boxes;

        /// <summary>Separators[i] liegt rechts von Boxes[i].</summary>
        public Rectangle[] Separators;

        /// <summary>Rechts vom letzten Kasten bis zum Inhaltsende — ein Klick
        /// dorthin schaltet in den Textmodus (Explorer-Verhalten).</summary>
        public Rectangle FreeZone;
    }

    /// <summary>
    /// Reine Geometrie der Adressleiste (Muster RibbonLayout): keine GDI-Aufrufe,
    /// kein DPI, kein Skin — die gemessenen Breiten kommen als Parameter.
    /// Wird es eng, klappen die VORDEREN Segmente zu «, denn die hinteren
    /// sagen, wo man ist.
    /// </summary>
    internal static class AddressBarLayout
    {
        public static AddressArrangement Arrange(Rectangle content, IList<int> segmentWidths,
            int separatorWidth, int overflowWidth)
        {
            if (segmentWidths == null) throw new ArgumentNullException(nameof(segmentWidths));

            int count = segmentWidths.Count;
            if (count == 0)
            {
                return new AddressArrangement
                {
                    Boxes = new AddressBox[0],
                    Separators = new Rectangle[0],
                    FreeZone = content
                };
            }

            int first = 0;
            bool overflow = false;

            if (WidthOf(segmentWidths, 0, separatorWidth) > content.Width)
            {
                overflow = true;
                first = count - 1;

                // So viele weitere vordere Segmente dazunehmen, wie mitsamt «
                // und Trenner noch passen. first erreicht 0 nie: Passten alle
                // samt «, passten sie erst recht ohne.
                while (first > 0 &&
                       overflowWidth + separatorWidth + WidthOf(segmentWidths, first - 1, separatorWidth) <= content.Width)
                {
                    first--;
                }
            }

            var boxes = new List<AddressBox>();
            var separators = new List<Rectangle>();
            int x = content.Left;

            if (overflow)
            {
                boxes.Add(new AddressBox
                {
                    SegmentIndex = -1,
                    Bounds = new Rectangle(x, content.Top, overflowWidth, content.Height)
                });
                x += overflowWidth;
            }

            for (int i = first; i < count; i++)
            {
                if (boxes.Count > 0)
                {
                    separators.Add(new Rectangle(x, content.Top, separatorWidth, content.Height));
                    x += separatorWidth;
                }

                // Nur das letzte Segment kann zu breit sein — dann wird es
                // abgeschnitten statt über den Rand zu ragen.
                int width = Math.Min(segmentWidths[i], Math.Max(0, content.Right - x));
                boxes.Add(new AddressBox
                {
                    SegmentIndex = i,
                    Bounds = new Rectangle(x, content.Top, width, content.Height)
                });
                x += width;
            }

            return new AddressArrangement
            {
                Boxes = boxes.ToArray(),
                Separators = separators.ToArray(),
                FreeZone = new Rectangle(x, content.Top, Math.Max(0, content.Right - x), content.Height)
            };
        }

        private static int WidthOf(IList<int> widths, int from, int separatorWidth)
        {
            int total = 0;
            for (int i = from; i < widths.Count; i++) total += widths[i];
            return total + separatorWidth * Math.Max(0, widths.Count - from - 1);
        }
    }
}
