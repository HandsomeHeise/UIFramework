using System;
using System.Drawing;
using System.Globalization;
using UIFramework.Controls.Editing;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Der Monatskalender-Gast für das Popup eines DateEdit: Kopf mit
    /// Vor/Zurück, Wochentagszeile, 6×7 Tageszellen (Nachbarmonat = Disabled,
    /// nicht klickbar), Heute-Zeile. Die Datumsrechnung liegt in MonthGrid;
    /// hier steht nur Malen und Treffer.
    /// </summary>
    internal sealed class CalendarContent : IPopupContent
    {
        private readonly CultureInfo _culture;
        private readonly DateTime? _selected;
        private MonthGrid _grid;
        private int _hoverRow = -1, _hoverColumn = -1;
        private bool _hoverPrev, _hoverNext, _hoverToday;
        private int _rowHeight = 1;
        private int _cellWidth = 1;
        private Size _size;

        public event Action<DateTime> DateChosen;
        public event EventHandler VisualChanged;
        public event EventHandler CloseRequested;

        public CalendarContent(DateTime monthToShow, DateTime? selected, CultureInfo culture)
        {
            _culture = culture;
            _selected = selected;
            _grid = new MonthGrid(monthToShow.Year, monthToShow.Month,
                culture.DateTimeFormat.FirstDayOfWeek);
        }

        public Size Measure(Graphics g, int dpi, int anchorWidth)
        {
            var day = SkinManager.Current.GetAppearance(ElementKeys.CalendarDay, ElementState.Normal);
            var probe = SkinPainter.MeasureText(g, "00", day, dpi);
            var padded = SkinPainter.InflateByPadding(probe, day, dpi);

            _rowHeight = padded.Height;
            _cellWidth = Math.Max(padded.Width, _rowHeight);

            int width = Math.Max(anchorWidth, 7 * _cellWidth);
            _cellWidth = (width + 6) / 7;       // aufrunden: sonst fiele die Breite unter anchorWidth
            width = 7 * _cellWidth;

            _size = new Size(width, 9 * _rowHeight);
            return _size;
        }

        // ---- Geometrie (eine Quelle für Paint UND Treffer) ------------------

        private Rectangle HeaderRow()    { return new Rectangle(0, 0, _size.Width, _rowHeight); }
        private Rectangle PrevArrow()    { return new Rectangle(0, 0, _rowHeight, _rowHeight); }
        private Rectangle NextArrow()    { return new Rectangle(_size.Width - _rowHeight, 0, _rowHeight, _rowHeight); }
        private Rectangle DayNamesRow()  { return new Rectangle(0, _rowHeight, _size.Width, _rowHeight); }
        private Rectangle TodayRow()     { return new Rectangle(0, 8 * _rowHeight, _size.Width, _rowHeight); }

        private Rectangle DayCell(int row, int column)
        {
            return new Rectangle(column * _cellWidth, (2 + row) * _rowHeight, _cellWidth, _rowHeight);
        }

        public void Paint(Graphics g, Rectangle bounds, int dpi)
        {
            var header = SkinManager.Current.GetAppearance(ElementKeys.CalendarHeader, ElementState.Normal);
            var headerHover = SkinManager.Current.GetAppearance(ElementKeys.CalendarHeader, ElementState.Hovered);

            SkinPainter.DrawBackground(g, bounds, header, dpi);

            // Kopf: ‹ Monat Jahr ›
            SkinPainter.DrawBackground(g, PrevArrow(), _hoverPrev ? headerHover : header, dpi);
            SkinPainter.DrawText(g, "‹", PrevArrow(), _hoverPrev ? headerHover : header, dpi, ContentAlignment.MiddleCenter);
            SkinPainter.DrawBackground(g, NextArrow(), _hoverNext ? headerHover : header, dpi);
            SkinPainter.DrawText(g, "›", NextArrow(), _hoverNext ? headerHover : header, dpi, ContentAlignment.MiddleCenter);

            string title = new DateTime(_grid.Year, _grid.Month, 1).ToString("MMMM yyyy", _culture);
            SkinPainter.DrawText(g, title, HeaderRow(), header, dpi, ContentAlignment.MiddleCenter);

            // Wochentagszeile ab FirstDayOfWeek der Culture — Spalten sind
            // Scheiben von DayNamesRow(), nicht nochmal eigene Geometrie.
            var dayNamesRow = DayNamesRow();
            for (int c = 0; c < 7; c++)
            {
                var dayOfWeek = (DayOfWeek)(((int)_grid.FirstDayOfWeek + c) % 7);
                string name = _culture.DateTimeFormat.GetAbbreviatedDayName(dayOfWeek);
                var cell = new Rectangle(dayNamesRow.X + c * _cellWidth, dayNamesRow.Y, _cellWidth, dayNamesRow.Height);
                SkinPainter.DrawText(g, name, cell, header, dpi, ContentAlignment.MiddleCenter);
            }

            // 42 Tageszellen
            for (int r = 0; r < 6; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    var date = _grid.CellAt(r, c);

                    ElementState state;
                    if (!_grid.IsInMonth(date)) state = ElementState.Disabled;
                    else if (_selected.HasValue && date == _selected.Value.Date) state = ElementState.Selected;
                    else if (r == _hoverRow && c == _hoverColumn) state = ElementState.Hovered;
                    else state = ElementState.Normal;

                    var appearance = SkinManager.Current.GetAppearance(ElementKeys.CalendarDay, state);
                    var cell = DayCell(r, c);

                    SkinPainter.DrawBackground(g, cell, appearance, dpi);
                    SkinPainter.DrawText(g, date.Day.ToString(_culture), cell, appearance, dpi,
                        ContentAlignment.MiddleCenter);
                }
            }

            // Heute-Zeile
            var todayState = _hoverToday ? ElementState.Hovered : ElementState.Normal;
            var today = SkinManager.Current.GetAppearance(ElementKeys.CalendarToday, todayState);
            SkinPainter.DrawBackground(g, TodayRow(), today, dpi);
            SkinPainter.DrawPaddedText(g,
                "Heute: " + DateTime.Today.ToString("d", _culture),
                TodayRow(), today, dpi, ContentAlignment.MiddleLeft);
        }

        public void HandleMouseMove(Point location)
        {
            bool prev = PrevArrow().Contains(location);
            bool next = NextArrow().Contains(location);
            bool todayRow = TodayRow().Contains(location);

            int row = -1, column = -1;
            for (int r = 0; r < 6 && row < 0; r++)
                for (int c = 0; c < 7; c++)
                    if (DayCell(r, c).Contains(location)) { row = r; column = c; break; }

            if (prev != _hoverPrev || next != _hoverNext || todayRow != _hoverToday ||
                row != _hoverRow || column != _hoverColumn)
            {
                _hoverPrev = prev; _hoverNext = next; _hoverToday = todayRow;
                _hoverRow = row; _hoverColumn = column;
                RaiseVisualChanged();
            }
        }

        public void HandleMouseClick(Point location)
        {
            HandleMouseMove(location);

            if (_hoverPrev) { _grid = _grid.PreviousMonth(); RaiseVisualChanged(); return; }
            if (_hoverNext) { _grid = _grid.NextMonth(); RaiseVisualChanged(); return; }
            if (_hoverToday) { Choose(DateTime.Today); return; }

            if (_hoverRow >= 0)
            {
                var date = _grid.CellAt(_hoverRow, _hoverColumn);
                if (_grid.IsInMonth(date)) Choose(date);   // Nachbarmonat: Disabled, kein Klick
            }
        }

        public bool HandleKey(System.Windows.Forms.Keys key)
        {
            if (key == System.Windows.Forms.Keys.Escape)
            {
                RaiseCloseRequested();
                return true;
            }
            return false;
        }

        private void Choose(DateTime date)
        {
            var chosen = DateChosen;
            if (chosen != null) chosen(date);
            RaiseCloseRequested();
        }

        private void RaiseVisualChanged()
        {
            var handler = VisualChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void RaiseCloseRequested()
        {
            var handler = CloseRequested;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        // ---- Nur für Tests --------------------------------------------------

        internal Rectangle DayCellForTests(int row, int column) { return DayCell(row, column); }
        internal Rectangle NextArrowForTests() { return NextArrow(); }
        internal Rectangle TodayRowForTests() { return TodayRow(); }
    }
}
