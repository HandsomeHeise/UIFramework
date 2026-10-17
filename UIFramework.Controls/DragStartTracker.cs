using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Wann aus einem Mausdruck ein Ziehen wird — Windows-Regel: sobald die
    /// Maus das um den Druckpunkt zentrierte Rechteck der Größe
    /// SystemInformation.DragSize verlässt. Reine Klasse, von Grid, Gallery
    /// und Baum geteilt; die Schwelle kommt als Parameter, damit Tests nicht
    /// von der Systemeinstellung abhängen.
    /// </summary>
    public sealed class DragStartTracker
    {
        private Point _origin;

        public bool IsArmed { get; private set; }

        /// <summary>Die Taste des letzten Scharfmachens — bleibt nach dem Start
        /// lesbar, damit der Aufrufer sie ins ItemDrag-Ereignis legen kann.</summary>
        public MouseButtons Button { get; private set; }

        public Point Origin
        {
            get { return _origin; }
        }

        public void Arm(Point origin, MouseButtons button)
        {
            _origin = origin;
            Button = button;
            IsArmed = true;
        }

        public void Disarm()
        {
            IsArmed = false;
        }

        /// <summary>True genau einmal: wenn point das Schwellenrechteck verlässt.
        /// Entschärft sich dabei — ein Druck ergibt höchstens einen Zug.</summary>
        public bool ShouldStart(Point point, Size dragSize)
        {
            if (!IsArmed) return false;

            var zone = new Rectangle(
                _origin.X - dragSize.Width / 2, _origin.Y - dragSize.Height / 2,
                dragSize.Width, dragSize.Height);
            if (zone.Contains(point)) return false;

            IsArmed = false;
            return true;
        }
    }
}
