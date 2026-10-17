using System;
using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Eine Zelle der Skin-Tabelle: das Erscheinungsbild eines Elements in einem Zustand.
    /// Alle Maße sind logisch (96-DPI-Basis); die Skalierung passiert im Painter.
    ///
    /// Ein Skin baut seine Erscheinungen mit Objekt-Initialisierern auf; SkinBase.Define
    /// friert jede ein, sobald sie in die Tabelle wandert. Danach werfen die Setter.
    /// Der Grund: GetAppearance reicht die Tabelleneinträge direkt heraus, damit der
    /// ReferenceEquals-Merker in SkinnedForm greift und nichts pro Zeichenvorgang
    /// allokiert. Ohne das Einfrieren könnte ein Konsument mit einem beiläufigen
    /// appearance.Background = ... den Skin für jedes Control app-weit verändern.
    /// </summary>
    public sealed class ElementAppearance
    {
        private Color _background;
        private Color? _backgroundGradientEnd;
        private Color _borderColor;
        private int _borderWidth;
        private CornerRadius _corners;
        private Color _foreColor;
        private FontSpec _font;
        private Padding _padding;

        public Color Background
        {
            get { return _background; }
            set { ThrowIfFrozen(); _background = value; }
        }

        /// <summary>Endfarbe eines senkrechten Verlaufs. Null bedeutet einfarbig.</summary>
        public Color? BackgroundGradientEnd
        {
            get { return _backgroundGradientEnd; }
            set { ThrowIfFrozen(); _backgroundGradientEnd = value; }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { ThrowIfFrozen(); _borderColor = value; }
        }

        /// <summary>Rahmenbreite in logischen Einheiten. 0 bedeutet kein Rahmen.</summary>
        public int BorderWidth
        {
            get { return _borderWidth; }
            set { ThrowIfFrozen(); _borderWidth = value; }
        }

        /// <summary>Eckradien in logischen Einheiten.</summary>
        public CornerRadius Corners
        {
            get { return _corners; }
            set { ThrowIfFrozen(); _corners = value; }
        }

        public Color ForeColor
        {
            get { return _foreColor; }
            set { ThrowIfFrozen(); _foreColor = value; }
        }

        public FontSpec Font
        {
            get { return _font; }
            set { ThrowIfFrozen(); _font = value; }
        }

        /// <summary>Innenabstand in logischen Einheiten.</summary>
        public Padding Padding
        {
            get { return _padding; }
            set { ThrowIfFrozen(); _padding = value; }
        }

        public bool HasGradient
        {
            get { return _backgroundGradientEnd.HasValue; }
        }

        /// <summary>
        /// Ob diese Erscheinung festgezurrt ist. Eingefrorenes gehört einem Skin
        /// und wird nur noch gelesen.
        /// </summary>
        public bool IsFrozen { get; private set; }

        /// <summary>
        /// Zurrt die Erscheinung fest. Idempotent — eine Erscheinung darf für
        /// mehrere Element/Zustand-Paare gelten, und dann friert Define sie
        /// mehrfach ein.
        /// </summary>
        public void Freeze()
        {
            IsFrozen = true;
        }

        /// <summary>
        /// Baut eine unabhängige, uneingefrorene Kopie mit allen acht Werten.
        /// Existiert, weil die eingefrorene Original-Erscheinung nicht mehr
        /// bearbeitet werden kann und Hand-Kopieren der acht Eigenschaften
        /// eine leicht vergessene stillschweigend verlöre — ein sichtbarer
        /// Fehler ohne Compilerwarnung. Erster Kunde: Teilprojekt 2, das aus
        /// Panel/Normal einen abweichenden Zeilenhintergrund ableiten will.
        /// </summary>
        public ElementAppearance Clone()
        {
            return new ElementAppearance
            {
                Background = _background,
                BackgroundGradientEnd = _backgroundGradientEnd,
                BorderColor = _borderColor,
                BorderWidth = _borderWidth,
                Corners = _corners,
                ForeColor = _foreColor,
                Font = _font,
                Padding = _padding
            };
        }

        private void ThrowIfFrozen()
        {
            if (IsFrozen)
                throw new InvalidOperationException(
                    "Diese ElementAppearance ist eingefroren und gehört einem Skin. " +
                    "Sie zu ändern würde jedes Control app-weit betreffen. " +
                    "Wer eine Abwandlung braucht, ruft Clone() auf und ändert die Kopie.");
        }
    }
}
