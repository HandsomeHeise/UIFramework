using System;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Scrollt während eines Drag &amp; Drop, solange die Maus in einem
    /// Randstreifen verweilt — getaktet über einen Timer, der nur läuft,
    /// solange eine Richtung gilt. Die Richtung ist reine Rechnung
    /// (DirectionAt); was ein Schritt bedeutet, sagt der Aufrufer (scrollBy
    /// bekommt -1 oder +1). Tick ist öffentlich, damit Tests ohne
    /// Nachrichtenschleife takten können.
    /// </summary>
    public sealed class DragAutoScroller : IDisposable
    {
        public const int IntervalMilliseconds = 50;

        private readonly Action<int> _scrollBy;
        private readonly Timer _timer;

        public DragAutoScroller(Action<int> scrollBy)
        {
            if (scrollBy == null) throw new ArgumentNullException(nameof(scrollBy));

            _scrollBy = scrollBy;
            _timer = new Timer { Interval = IntervalMilliseconds };
            _timer.Tick += (s, e) => Tick();
        }

        /// <summary>-1 = nach oben, +1 = nach unten, 0 = steht.</summary>
        public int Direction { get; private set; }

        public bool IsRunning
        {
            get { return Direction != 0; }
        }

        /// <summary>-1 im oberen Streifen (und darüber, etwa über dem Grid-Kopf),
        /// +1 im unteren (und darunter), sonst 0.</summary>
        public static int DirectionAt(int y, int top, int bottom, int band)
        {
            if (band <= 0 || bottom <= top) return 0;
            if (y < top + band) return -1;
            if (y >= bottom - band) return 1;
            return 0;
        }

        public void Update(int y, int top, int bottom, int band)
        {
            Direction = DirectionAt(y, top, bottom, band);

            if (Direction != 0)
            {
                if (!_timer.Enabled) _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        public void Stop()
        {
            Direction = 0;
            _timer.Stop();
        }

        public void Tick()
        {
            if (Direction != 0) _scrollBy(Direction);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
