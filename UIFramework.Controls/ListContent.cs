using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Die aufgeklappte Liste eines SkinComboBox als Popup-Gast. Zeilen werden
    /// wie Grid-Zellen elementweise gemalt (ElementKeys.GridCell), der Rahmen
    /// über ElementKeys.ComboBoxList — exakt die Technik des alten ComboPopup,
    /// nur ohne eigenes Fenster.
    /// </summary>
    internal sealed class ListContent : IPopupContent
    {
        private readonly IList<object> _items;
        private readonly Func<int> _getSelectedIndex;
        private int _hoverIndex = -1;
        private int _rowHeight = 1;   // von Measure gesetzt; Treffer teilen durch diese Zahl

        public event Action<int> ItemChosen;
        public event EventHandler VisualChanged;
        public event EventHandler CloseRequested;

        public ListContent(IList<object> items, Func<int> getSelectedIndex)
        {
            _items = items;
            _getSelectedIndex = getSelectedIndex;
        }

        public Size Measure(Graphics g, int dpi, int anchorWidth)
        {
            var appearance = SkinManager.Current.GetAppearance(ElementKeys.GridCell, ElementState.Normal);
            var textSize = SkinPainter.MeasureText(g, "Xg", appearance, dpi);
            _rowHeight = SkinPainter.InflateByPadding(textSize, appearance, dpi).Height;

            int height = Math.Max(_rowHeight, _rowHeight * _items.Count);
            return new Size(Math.Max(anchorWidth, 40), height);
        }

        public void Paint(Graphics g, Rectangle bounds, int dpi)
        {
            var container = SkinManager.Current.GetAppearance(ElementKeys.ComboBoxList, ElementState.Normal);
            SkinPainter.DrawBackground(g, bounds, container, dpi);
            SkinPainter.DrawBorder(g, bounds, container, dpi);

            int selectedIndex = _getSelectedIndex();

            for (int i = 0; i < _items.Count; i++)
            {
                ElementState rowState;
                if (i == _hoverIndex) rowState = ElementState.Hovered;
                else if (i == selectedIndex) rowState = ElementState.Selected;
                else rowState = ElementState.Normal;

                var rowAppearance = SkinManager.Current.GetAppearance(ElementKeys.GridCell, rowState);
                var rowBounds = new Rectangle(bounds.Left, bounds.Top + i * _rowHeight, bounds.Width, _rowHeight);

                SkinPainter.DrawBackground(g, rowBounds, rowAppearance, dpi);
                SkinPainter.DrawPaddedText(
                    g, _items[i].ToString(), rowBounds, rowAppearance, dpi, ContentAlignment.MiddleLeft);
            }
        }

        public void HandleMouseMove(Point location)
        {
            int index = _rowHeight > 0 ? location.Y / _rowHeight : -1;
            if (index < 0 || index >= _items.Count) index = -1;

            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                var handler = VisualChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        public void HandleMouseClick(Point location)
        {
            HandleMouseMove(location);
            if (_hoverIndex < 0) return;

            var chosen = ItemChosen;
            if (chosen != null) chosen(_hoverIndex);

            var close = CloseRequested;
            if (close != null) close(this, EventArgs.Empty);
        }

        public bool HandleKey(Keys key)
        {
            if (key == Keys.Escape)
            {
                var close = CloseRequested;
                if (close != null) close(this, EventArgs.Empty);
                return true;
            }
            if (_items.Count == 0) return false;
            int current = _hoverIndex >= 0 ? _hoverIndex : _getSelectedIndex();
            int next = current;
            switch (key)
            {
                case Keys.Down: next = Math.Min(_items.Count - 1, current + 1); break;
                case Keys.Up: next = Math.Max(0, current - 1); break;
                case Keys.Home: next = 0; break;
                case Keys.End: next = _items.Count - 1; break;
                case Keys.PageDown: next = Math.Min(_items.Count - 1, current + 8); break;
                case Keys.PageUp: next = Math.Max(0, current - 8); break;
                case Keys.Enter:
                    var chosen = ItemChosen;
                    if (chosen != null) chosen(Math.Max(0, current));
                    var close = CloseRequested;
                    if (close != null) close(this, EventArgs.Empty);
                    return true;
                default: return false;
            }
            _hoverIndex = next;
            var changed = VisualChanged;
            if (changed != null) changed(this, EventArgs.Empty);
            return true;
        }
    }
}
