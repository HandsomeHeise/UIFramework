using System;
using System.Drawing;

namespace UIFramework.Tree
{
    /// <summary>Die Teilflächen einer Baumzeile.</summary>
    internal struct TreeRowParts
    {
        public Rectangle Glyph;
        public Rectangle Image;
        public Rectangle Text;
    }

    /// <summary>
    /// Reine Geometrie des Baums (Muster RibbonLayout/RowViewport): keine
    /// GDI-Aufrufe, kein DPI, kein Skin — Zeilenhöhe und Textzeilenhöhe misst
    /// das Control und reicht sie herein. Vertikal: welche Zeilen im
    /// Sichtfenster liegen. Waagerecht: wo in einer Zeile Glyphe, Bild und
    /// Text sitzen — abgeleitet aus der Textzeilenhöhe statt aus Konstanten,
    /// damit alles mit der Schrift wächst.
    /// </summary>
    internal sealed class TreeLayout
    {
        public TreeLayout(int rowHeight, int viewportHeight, int scrollOffset, int rowCount)
        {
            RowHeight = rowHeight;
            ViewportHeight = viewportHeight;
            ScrollOffset = scrollOffset;
            RowCount = rowCount;
        }

        public int RowHeight { get; }
        public int ViewportHeight { get; }
        public int ScrollOffset { get; }
        public int RowCount { get; }

        public int TotalHeight
        {
            get { return RowHeight <= 0 ? 0 : RowCount * RowHeight; }
        }

        public int MaxScrollOffset
        {
            get { return Math.Max(0, TotalHeight - ViewportHeight); }
        }

        public int FirstVisibleRow
        {
            get
            {
                if (RowHeight <= 0 || RowCount == 0) return 0;
                return Math.Min(RowCount - 1, Math.Max(0, ScrollOffset) / RowHeight);
            }
        }

        /// <summary>Zeilen, die das Sichtfenster auch nur teilweise schneiden.</summary>
        public int VisibleRowCount
        {
            get
            {
                if (RowHeight <= 0 || RowCount == 0 || ViewportHeight <= 0) return 0;

                int first = FirstVisibleRow;
                int endExclusive = Math.Min(RowCount, (ScrollOffset + ViewportHeight + RowHeight - 1) / RowHeight);
                return Math.Max(0, endExclusive - first);
            }
        }

        /// <summary>Oberkante der Zeile relativ zur Viewport-Oberkante (Versatz abgezogen).</summary>
        public int RowTop(int rowIndex)
        {
            return rowIndex * RowHeight - ScrollOffset;
        }

        /// <summary>Zeile unter einem y relativ zur Viewport-Oberkante; -1 außerhalb.</summary>
        public int RowAt(int y)
        {
            if (y < 0 || RowHeight <= 0) return -1;

            int index = (y + ScrollOffset) / RowHeight;
            return index < RowCount ? index : -1;
        }

        /// <summary>
        /// Glyphe, Bild und Text einer Zeile im Innenrechteck content. Kante =
        /// Textzeilenhöhe (Muster kleiner Ribbon-Knopf), Lücke = ein Viertel
        /// davon, Einrückung je Ebene = Kante + Lücke: die Glyphe eines Kindes
        /// steht genau unter dem Bild seines Elternknotens.
        /// </summary>
        public static TreeRowParts Parts(Rectangle content, int level, int lineHeight)
        {
            int edge = lineHeight;
            int gap = lineHeight / 4;
            int indent = edge + gap;

            int glyphLeft = content.Left + level * indent;
            int top = content.Top + (content.Height - edge) / 2;

            var glyph = new Rectangle(glyphLeft, top, edge, edge);
            var image = new Rectangle(glyph.Right + gap, top, edge, edge);
            int textLeft = image.Right + gap;

            return new TreeRowParts
            {
                Glyph = glyph,
                Image = image,
                Text = new Rectangle(textLeft, content.Top, Math.Max(0, content.Right - textLeft), content.Height)
            };
        }
    }
}
