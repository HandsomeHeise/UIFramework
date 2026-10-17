using System;
using System.Collections.Generic;
using System.Drawing;

namespace UIFramework.Controls
{
    /// <summary>
    /// Ein Ribbon-Element mit seiner gemessenen Knopfgröße (Gerätepixel) —
    /// die Messung selbst gehört dem Control/SkinPainter, nicht dem Layout.
    /// </summary>
    internal struct RibbonBox
    {
        public RibbonItem Item;
        public Size Size;
    }

    /// <summary>Ein platziertes Element: das Item plus seine berechneten Bounds.</summary>
    internal struct RibbonPlacedItem
    {
        public RibbonItem Item;
        public Rectangle Bounds;
    }

    /// <summary>
    /// Fertig angeordnete Gruppe: Rahmen (inkl. Titelzeile), die Titelzeile
    /// selbst und die platzierten Items — alle Bounds bereits in
    /// Eltern-Koordinaten (durch <see cref="RibbonLayout.LayoutGroups"/> verschoben).
    /// </summary>
    internal sealed class RibbonGroupPane
    {
        public Rectangle Frame;
        public Rectangle TitleRow;
        public RibbonPlacedItem[] Items;
    }

    /// <summary>
    /// Reine Geometrie des Ribbons: keine GDI-Aufrufe, kein DPI, kein Messen —
    /// jede Größe kommt als Parameter herein (vom Control in Gerätepixeln
    /// ermittelt). Damit ist die Klasse headless testbar und unabhängig vom
    /// Skin. 4b2 erweitert sie um weitere Layout-Varianten (z. B. andere
    /// Spaltenregeln); die Trennung Geometrie/Messung bleibt dabei bestehen.
    /// </summary>
    internal static class RibbonLayout
    {
        /// <summary>
        /// Reiht Tab-Köpfe ab origin lückenlos von links auf; alle Köpfe
        /// bekommen die maximale Kopfhöhe (einheitliche Zeile).
        /// </summary>
        public static Rectangle[] LayoutTabHeaders(IList<Size> headerSizes, Point origin)
        {
            if (headerSizes == null || headerSizes.Count == 0) return new Rectangle[0];

            int height = 0;
            for (int i = 0; i < headerSizes.Count; i++)
                height = Math.Max(height, headerSizes[i].Height);

            var result = new Rectangle[headerSizes.Count];
            int x = origin.X;
            for (int i = 0; i < headerSizes.Count; i++)
            {
                var size = headerSizes[i];
                result[i] = new Rectangle(x, origin.Y, size.Width, height);
                x += size.Width;
            }
            return result;
        }

        /// <summary>
        /// Ordnet die Items einer Gruppe in Spalten an (Bounds relativ zum
        /// Gruppen-Inhaltsursprung 0,0):
        ///  - Large-Item oder Separator: eigene volle Spalte (Höhe = contentHeight,
        ///    Breite = Box-Breite) — geometrisch identisch, daher ein Zweig.
        ///  - Small-Items stapeln von oben, bis zu drei pro Spalte
        ///    (Zeilenhöhe = contentHeight/3, Ganzzahl); ein Large/Separator
        ///    beendet eine laufende Small-Spalte vorzeitig.
        ///  - columnGap Pixel zwischen Spalten (vom Aufrufer gemessen).
        /// </summary>
        public static RibbonPlacedItem[] ArrangeGroupItems(IList<RibbonBox> boxes, int contentHeight, int columnGap)
        {
            if (boxes == null || boxes.Count == 0) return new RibbonPlacedItem[0];

            var result = new RibbonPlacedItem[boxes.Count];
            int rowHeight = contentHeight / 3;
            int columnX = 0;

            bool inSmallColumn = false;
            int smallCount = 0;
            int smallColumnWidth = 0;

            for (int i = 0; i < boxes.Count; i++)
            {
                var box = boxes[i];
                bool ownsFullColumn = box.Item.Kind == RibbonItemKind.Separator
                    || box.Item.Size == RibbonItemSize.Large;

                if (ownsFullColumn)
                {
                    if (inSmallColumn)
                    {
                        columnX += smallColumnWidth + columnGap;
                        inSmallColumn = false;
                        smallCount = 0;
                        smallColumnWidth = 0;
                    }

                    result[i] = new RibbonPlacedItem
                    {
                        Item = box.Item,
                        Bounds = new Rectangle(columnX, 0, box.Size.Width, contentHeight)
                    };
                    columnX += box.Size.Width + columnGap;
                }
                else
                {
                    if (!inSmallColumn)
                    {
                        inSmallColumn = true;
                        smallCount = 0;
                        smallColumnWidth = 0;
                    }
                    else if (smallCount == 3)
                    {
                        columnX += smallColumnWidth + columnGap;
                        smallCount = 0;
                        smallColumnWidth = 0;
                    }

                    result[i] = new RibbonPlacedItem
                    {
                        Item = box.Item,
                        Bounds = new Rectangle(columnX, smallCount * rowHeight, box.Size.Width, rowHeight)
                    };
                    smallColumnWidth = Math.Max(smallColumnWidth, box.Size.Width);
                    smallCount++;
                }
            }

            return result;
        }

        /// <summary>
        /// Setzt fertige Gruppen nebeneinander: Frame = Inhalt + Padding
        /// ringsum + Titelzeile unten (titleHeight), groupGap zwischen den
        /// Gruppen. Die relativen Item-Bounds aus ArrangeGroupItems werden um
        /// den jeweiligen Inhaltsursprung (origin + Padding links/oben)
        /// verschoben. paddings sind fertige Pixel — kein Padding-Typ hier.
        /// </summary>
        public static RibbonGroupPane[] LayoutGroups(
            IList<Size> contentSizes, IList<RibbonPlacedItem[]> arrangedItems,
            Point origin, int titleHeight, int groupGap,
            int padLeft, int padTop, int padRight, int padBottom)
        {
            if (contentSizes == null || contentSizes.Count == 0) return new RibbonGroupPane[0];

            var panes = new RibbonGroupPane[contentSizes.Count];
            int x = origin.X;

            for (int i = 0; i < contentSizes.Count; i++)
            {
                var contentSize = contentSizes[i];
                int frameWidth = padLeft + contentSize.Width + padRight;
                int frameHeight = padTop + contentSize.Height + padBottom + titleHeight;
                var frame = new Rectangle(x, origin.Y, frameWidth, frameHeight);
                var titleRow = new Rectangle(frame.X, frame.Bottom - titleHeight, frame.Width, titleHeight);

                var sourceItems = arrangedItems[i];
                var shiftedItems = new RibbonPlacedItem[sourceItems.Length];
                int shiftX = x + padLeft;
                int shiftY = origin.Y + padTop;
                for (int j = 0; j < sourceItems.Length; j++)
                {
                    var bounds = sourceItems[j].Bounds;
                    bounds.X += shiftX;
                    bounds.Y += shiftY;
                    shiftedItems[j] = new RibbonPlacedItem { Item = sourceItems[j].Item, Bounds = bounds };
                }

                panes[i] = new RibbonGroupPane { Frame = frame, TitleRow = titleRow, Items = shiftedItems };
                x = frame.Right + groupGap;
            }

            return panes;
        }
    }
}
