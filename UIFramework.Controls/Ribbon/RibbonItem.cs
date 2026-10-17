using System;
using System.Drawing;

namespace UIFramework.Controls
{
    /// <summary>
    /// Art eines Ribbon-Elements.
    /// </summary>
    public enum RibbonItemKind
    {
        /// <summary>Normaler Schalter.</summary>
        Button,

        /// <summary>Umschalt-Schalter (hat Checked-Zustand).</summary>
        ToggleButton,

        /// <summary>Schalter mit Dropdown-Menü.</summary>
        DropDownButton,

        /// <summary>Trennlinie — nie interaktiv.</summary>
        Separator
    }

    /// <summary>
    /// Größe eines Ribbon-Elements.
    /// </summary>
    public enum RibbonItemSize
    {
        /// <summary>Großes Element.</summary>
        Large,

        /// <summary>Kleines Element.</summary>
        Small
    }

    /// <summary>
    /// Ein Element im Ribbon (Button, Toggle, Dropdown oder Separator) — reines
    /// Datenobjekt, kein Control. Eine Klasse statt Vererbungszoo: Kind und Size
    /// steuern das Layout. Das Image gehört der App und wird vom Framework nicht
    /// disposed. Menu wirkt nur bei DropDownButton.
    /// </summary>
    public sealed class RibbonItem
    {
        public RibbonItem()
        {
            Kind = RibbonItemKind.Button;
            Size = RibbonItemSize.Large;
            Text = string.Empty;
            Enabled = true;
            Checked = false;
        }

        public RibbonItem(string text) : this()
        {
            Text = text ?? string.Empty;
        }

        /// <summary>
        /// Art des Elements (Button, ToggleButton, DropDownButton oder Separator).
        /// </summary>
        public RibbonItemKind Kind { get; set; }

        /// <summary>
        /// Größe des Elements (Large oder Small).
        /// </summary>
        public RibbonItemSize Size { get; set; }

        /// <summary>
        /// Text des Elements — wird nie null, Standard ist leerer String.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Optionales Bild des Elements — gehört der App, wird nicht disposed.
        /// </summary>
        public Image Image { get; set; }

        /// <summary>
        /// Gibt an, ob das Element aktiviert ist. Standard ist true.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gibt an, ob das Element (als ToggleButton) aktiviert ist.
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// Optionales Popup-Menü — wirkt nur bei DropDownButton.
        /// </summary>
        public PopupMenu Menu { get; set; }

        /// <summary>
        /// Tritt auf, wenn der Benutzer das Element betätigt.
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Erstellt ein neues Separator-Element.
        /// </summary>
        public static RibbonItem Separator()
        {
            return new RibbonItem { Kind = RibbonItemKind.Separator };
        }

        /// <summary>
        /// Gibt an, ob das Element für Benutzerinteraktion verfügbar ist
        /// (Enabled && Kind != Separator).
        /// </summary>
        internal bool IsInteractive
        {
            get { return Enabled && Kind != RibbonItemKind.Separator; }
        }

        /// <summary>
        /// Löst das Click-Ereignis aus — Sender ist dieses Item.
        /// </summary>
        internal void PerformClick()
        {
            var handler = Click;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
