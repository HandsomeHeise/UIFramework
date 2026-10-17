using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using UIFramework.Core.Skinning;

namespace UIFramework.Core.Rendering
{
    /// <summary>
    /// Baut den Pfad eines Rechtecks mit vier unabhängigen Eckradien.
    /// Die Radien sind hier bereits physisch (skaliert).
    /// </summary>
    internal static class RoundedRectangle
    {
        public static GraphicsPath Create(Rectangle bounds, CornerRadius radius)
        {
            var path = new GraphicsPath();

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return path;

            if (radius.IsZero)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // Ein Radius darf nie größer werden als die halbe kürzere Seite,
            // sonst überschlagen sich die Bögen und GDI+ zeichnet Unsinn.
            int limit = Math.Min(bounds.Width, bounds.Height) / 2;
            int topLeft = Math.Min(radius.TopLeft, limit);
            int topRight = Math.Min(radius.TopRight, limit);
            int bottomRight = Math.Min(radius.BottomRight, limit);
            int bottomLeft = Math.Min(radius.BottomLeft, limit);

            int left = bounds.Left;
            int top = bounds.Top;
            int right = bounds.Right - 1;
            int bottom = bounds.Bottom - 1;

            if (topLeft > 0)
                path.AddArc(left, top, topLeft * 2, topLeft * 2, 180f, 90f);
            else
                path.AddLine(left, top, left, top);

            if (topRight > 0)
                path.AddArc(right - topRight * 2, top, topRight * 2, topRight * 2, 270f, 90f);
            else
                path.AddLine(right, top, right, top);

            if (bottomRight > 0)
                path.AddArc(right - bottomRight * 2, bottom - bottomRight * 2, bottomRight * 2, bottomRight * 2, 0f, 90f);
            else
                path.AddLine(right, bottom, right, bottom);

            if (bottomLeft > 0)
                path.AddArc(left, bottom - bottomLeft * 2, bottomLeft * 2, bottomLeft * 2, 90f, 90f);
            else
                path.AddLine(left, bottom, left, bottom);

            path.CloseFigure();
            return path;
        }
    }
}
