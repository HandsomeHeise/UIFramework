using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Dpi;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Eine geskinnte Bildlaufleiste. Die Windows-eigene VScrollBar folgt dem
    /// System-Theme und lässt sich nicht einfärben — neben einem dunklen Grid
    /// stünde eine helle Systemleiste.
    ///
    /// Rechnet nichts selbst: die gesamte Arithmetik liegt in ScrollBarGeometry,
    /// die ohne Fenster prüfbar ist. Dieses Control legt nur die Achse fest,
    /// zeichnet und nimmt Eingaben entgegen.
    ///
    /// Bewusst ohne Pfeilknöpfe (siehe Plan, Task 3): Rinne, Daumen, Klick in
    /// die Rinne, Mausrad. Moderne Leisten haben keine Pfeile mehr.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("Scroll")]
    public class SkinScrollBar : SkinnedControl
    {
        /// <summary>
        /// Mindestlänge des Daumens in logischen Einheiten. Bei einer Million
        /// Zeilen wäre der proportionale Daumen 0px hoch und nicht greifbar.
        /// </summary>
        private const int MinThumbLengthLogical = 16;

        /// <summary>
        /// Dicke der Leiste quer zur Achse, logisch. Öffentlich, weil
        /// GridControl mit demselben Maß den Platz reserviert, den seine
        /// Leisten dem Inhalt wegnehmen — zwei unabhängige Zahlen würden
        /// unbemerkt auseinanderlaufen.
        /// </summary>
        public const int ThicknessLogical = 12;

        private Orientation _orientation = Orientation.Vertical;
        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private int _largeChange = 10;
        private int _smallChange = 1;

        private bool _thumbHovered;
        private bool _thumbPressed;
        private int _dragGrabOffset;   // Abstand Mauszeiger -> Daumenanfang beim Greifen

        public SkinScrollBar()
        {
            // Eine Bildlaufleiste nimmt nie den Fokus — sonst stiehlt sie ihn
            // dem Grid, dessen Tastaturnavigation daran hängt.
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Width = ThicknessLogical;
            Height = 100;
        }

        public event EventHandler Scroll;

        protected override string ElementKey
        {
            get { return ElementKeys.ScrollBar; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        [Category("Behavior")]
        [DefaultValue(Orientation.Vertical)]
        public Orientation Orientation
        {
            get { return _orientation; }
            set
            {
                if (_orientation == value) return;
                _orientation = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                if (_minimum == value) return;
                _minimum = value;
                Value = _value;     // klemmt neu
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(100)]
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                if (_maximum == value) return;
                _maximum = value;
                Value = _value;     // ein jetzt unerreichbarer Wert wird zurückgeholt
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(10)]
        public int LargeChange
        {
            get { return _largeChange; }
            set
            {
                int wanted = value < 1 ? 1 : value;
                if (_largeChange == wanted) return;
                _largeChange = wanted;
                Value = _value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(1)]
        public int SmallChange
        {
            get { return _smallChange; }
            set { _smallChange = value < 1 ? 1 : value; }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = ScrollBarGeometry.ClampValue(value, _minimum, _maximum, _largeChange);

                // Auf den GEKLEMMTEN Wert vergleichen, nicht auf den gewünschten:
                // Sonst feuerte am Ende der Liste jede weitere Mausbewegung ein
                // Ereignis, obwohl sich nichts bewegt.
                if (_value == clamped) return;

                _value = clamped;
                Invalidate();
                OnScroll(EventArgs.Empty);
            }
        }

        /// <summary>Die aktuelle Rechnung — für Tests und für GridControl.</summary>
        internal ScrollBarGeometry Geometry
        {
            get
            {
                return new ScrollBarGeometry(
                    TrackLength, _minimum, _maximum, _value, _largeChange,
                    DpiScale.Scale(MinThumbLengthLogical, DeviceDpi));
            }
        }

        /// <summary>
        /// Ein Mausrad-Schritt. Öffentlich, damit das Grid seine Radereignisse
        /// hierher weiterreichen kann — der Zeiger steht dabei über dem Grid,
        /// nicht über der Leiste, also erreicht OnMouseWheel diese nie.
        /// </summary>
        public void PerformWheel(int delta)
        {
            int steps = delta / SystemInformation.MouseWheelScrollDelta;

            // Der Fallback ist fuer kleine, aber echte Deltas gedacht (hochaufloesende
            // Raeder), nicht fuer ein Delta von exakt 0 — sonst scrollte ein
            // weitergereichtes Nulldelta (z. B. von GridControl.PerformWheel) um einen
            // SmallChange, obwohl nichts passiert ist.
            if (steps == 0 && delta != 0) steps = delta > 0 ? 1 : -1;

            // Rad nach oben (positives Delta) bedeutet kleinerer Wert.
            Value = _value - steps * _smallChange;
        }

        protected virtual void OnScroll(EventArgs e)
        {
            var handler = Scroll;
            if (handler != null) handler(this, e);
        }

        private int TrackLength
        {
            get
            {
                int length = _orientation == Orientation.Vertical ? Height : Width;

                // ScrollBarGeometry verlangt eine positive Rinne. Ein Control mit
                // Höhe 0 gibt es kurz vor dem ersten Layout wirklich.
                return length > 0 ? length : 1;
            }
        }

        /// <summary>
        /// Das Rechteck des Daumens zu einer bereits vorliegenden Geometrie.
        ///
        /// Nimmt die Geometrie bewusst als Parameter, statt sie selbst zu holen:
        /// Jeder Aufrufer (PaintContent, OnMouseMove, OnMouseDown) braucht sie
        /// ohnehin noch für anderes und liest sie einmal in eine lokale Variable.
        /// Ein parameterloser Zugriff hier hätte dieselbe Klemm- und
        /// Daumenlängenrechnung ein zweites Mal je Methode aufgebaut.
        /// </summary>
        private Rectangle ThumbRectangleFor(ScrollBarGeometry geometry)
        {
            return _orientation == Orientation.Vertical
                ? new Rectangle(0, geometry.ThumbOffset, Width, geometry.ThumbLength)
                : new Rectangle(geometry.ThumbOffset, 0, geometry.ThumbLength, Height);
        }

        private int AlongAxis(Point p)
        {
            return _orientation == Orientation.Vertical ? p.Y : p.X;
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            var geometry = Geometry;
            if (!geometry.IsScrollable) return;

            var state = _thumbPressed
                ? ElementState.Pressed
                : _thumbHovered ? ElementState.Hovered : ElementState.Normal;

            var thumb = SkinManager.Current.GetAppearance(ElementKeys.ScrollBarThumb, state);
            var bounds = ThumbRectangleFor(geometry);

            SkinPainter.DrawBackground(g, bounds, thumb, DeviceDpi);
            SkinPainter.DrawBorder(g, bounds, thumb, DeviceDpi);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var geometry = Geometry;

            if (_thumbPressed)
            {
                Value = geometry.ValueAt(AlongAxis(e.Location) - _dragGrabOffset);
            }
            else
            {
                bool over = ThumbRectangleFor(geometry).Contains(e.Location);
                if (over != _thumbHovered)
                {
                    _thumbHovered = over;
                    Invalidate();
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var geometry = Geometry;

            if (e.Button == MouseButtons.Left && geometry.IsScrollable)
            {
                var thumb = ThumbRectangleFor(geometry);

                if (thumb.Contains(e.Location))
                {
                    _thumbPressed = true;
                    // Den Griffpunkt merken, sonst springt der Daumen unter dem
                    // Zeiger auf seinen Anfang, sobald man ihn anfasst.
                    _dragGrabOffset = AlongAxis(e.Location) - geometry.ThumbOffset;
                    Invalidate();
                }
                else
                {
                    // Klick in die Rinne: eine Seite weit springen.
                    int direction = AlongAxis(e.Location) < geometry.ThumbOffset ? -1 : 1;
                    Value = _value + direction * _largeChange;
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_thumbPressed)
            {
                _thumbPressed = false;
                Invalidate();
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_thumbHovered)
            {
                _thumbHovered = false;
                Invalidate();
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            PerformWheel(e.Delta);
            base.OnMouseWheel(e);
        }
    }
}
