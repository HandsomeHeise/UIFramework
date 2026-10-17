using System;

namespace UIFramework.Controls.Editing
{
    /// <summary>
    /// Das Monatsblatt eines Kalenders als reine Rechnung: 6 Wochen × 7 Tage =
    /// 42 Zellen, beginnend am Wochenstart vor (oder auf) dem Monatsersten.
    /// 6 Zeilen decken jeden Monat (31 Tage + höchstens 6 Vorlauftage = 37 ≤ 42);
    /// die feste Höhe verhindert, dass das Popup beim Blättern springt.
    ///
    /// Kein Graphics, kein Control — kopflos prüfbar (wie RowViewport im Grid).
    /// </summary>
    public sealed class MonthGrid
    {
        private readonly DateTime _firstCell;

        public MonthGrid(int year, int month, DayOfWeek firstDayOfWeek)
        {
            Year = year;
            Month = month;
            FirstDayOfWeek = firstDayOfWeek;

            var firstOfMonth = new DateTime(year, month, 1);
            int lead = ((int)firstOfMonth.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
            _firstCell = firstOfMonth.AddDays(-lead);
        }

        public int Year { get; }
        public int Month { get; }
        public DayOfWeek FirstDayOfWeek { get; }

        public DateTime CellAt(int row, int column)
        {
            if (row < 0 || row > 5) throw new ArgumentOutOfRangeException(nameof(row));
            if (column < 0 || column > 6) throw new ArgumentOutOfRangeException(nameof(column));

            return _firstCell.AddDays(row * 7 + column);
        }

        public bool IsInMonth(DateTime day)
        {
            return day.Year == Year && day.Month == Month;
        }

        public MonthGrid PreviousMonth()
        {
            var previous = new DateTime(Year, Month, 1).AddMonths(-1);
            return new MonthGrid(previous.Year, previous.Month, FirstDayOfWeek);
        }

        public MonthGrid NextMonth()
        {
            var next = new DateTime(Year, Month, 1).AddMonths(1);
            return new MonthGrid(next.Year, next.Month, FirstDayOfWeek);
        }
    }
}
