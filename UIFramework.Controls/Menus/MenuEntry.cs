using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Ein Menüeintrag — reines Datenobjekt, kein Control. Heißt bewusst nicht
    /// MenuItem: Konsumenten haben fast immer auch System.Windows.Forms im
    /// Using, und dessen MenuItem machte jeden Verweis mehrdeutig (CS0104).
    /// Text trägt Mnemonics per '&' ("&Datei"), '&&' ist ein
    /// echtes &. Ein Eintrag mit Kindern ist ein Untermenü-Elter: Click
    /// und Shortcut bleiben bei ihm wirkungslos.
    /// </summary>
    public sealed class MenuEntry
    {
        private readonly List<MenuEntry> _items = new List<MenuEntry>();

        public MenuEntry()
        {
            Text = string.Empty;
            Enabled = true;
        }

        public MenuEntry(string text) : this()
        {
            Text = text ?? string.Empty;
        }

        public string Text { get; set; }
        public bool Enabled { get; set; }
        public bool Checked { get; set; }

        /// <summary>Toggelt Checked beim Ausführen — VOR dem Click-Ereignis.</summary>
        public bool CheckOnClick { get; set; }

        /// <summary>App-weites Kürzel; feuert auch bei geschlossenem Menü
        /// (SkinnedForm.ProcessCmdKey bzw. MenuBar.ProcessShortcut).</summary>
        public Keys Shortcut { get; set; }

        public IList<MenuEntry> Items { get { return _items; } }
        public bool IsSeparator { get; private set; }

        public event EventHandler Click;

        public static MenuEntry Separator()
        {
            return new MenuEntry { IsSeparator = true, Enabled = false };
        }

        internal bool HasChildren { get { return _items.Count > 0; } }
        internal bool IsSelectable { get { return Enabled && !IsSeparator; } }

        internal void PerformClick()
        {
            var handler = Click;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
