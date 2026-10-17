using System;
using System.Drawing;

namespace UIFramework.Controls
{
    /// <summary>
    /// Wo ein Menü-Popup hingehört — reine Geometrie, Bildschirmkoordinaten.
    /// Umklappen statt Abschneiden: Ein Kontextmenü am unteren Rand öffnet
    /// nach oben, ein Untermenü an der rechten Kante nach links (klassisches
    /// Windows-Verhalten). Wenn auch Umklappen nicht hilft, wird an die
    /// Arbeitsbereich-Kante geklemmt — Menüs scrollen nicht (v1-Grenze).
    /// </summary>
    internal static class MenuPlacement
    {
        public static Rectangle PlaceDropdown(Rectangle barItemScreen, Size popupSize, Rectangle workArea)
        {
            int x = Clamp(barItemScreen.Left, workArea.Left, workArea.Right - popupSize.Width);
            int y = barItemScreen.Bottom;
            if (y + popupSize.Height > workArea.Bottom) y = barItemScreen.Top - popupSize.Height;
            y = Math.Max(workArea.Top, y);
            return new Rectangle(new Point(x, y), popupSize);
        }

        public static Rectangle PlaceSubmenu(Rectangle parentItemScreen, Size popupSize, Rectangle workArea)
        {
            int x = parentItemScreen.Right;
            if (x + popupSize.Width > workArea.Right) x = parentItemScreen.Left - popupSize.Width;
            x = Math.Max(workArea.Left, x);
            int y = Clamp(parentItemScreen.Top, workArea.Top, workArea.Bottom - popupSize.Height);
            return new Rectangle(new Point(x, y), popupSize);
        }

        public static Rectangle PlaceContextMenu(Point location, Size popupSize, Rectangle workArea)
        {
            int x = location.X;
            if (x + popupSize.Width > workArea.Right) x = location.X - popupSize.Width;
            int y = location.Y;
            if (y + popupSize.Height > workArea.Bottom) y = location.Y - popupSize.Height;
            return new Rectangle(
                new Point(Math.Max(workArea.Left, x), Math.Max(workArea.Top, y)), popupSize);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
