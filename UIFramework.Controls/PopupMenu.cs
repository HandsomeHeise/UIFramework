using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Das Kontextmenü — gleiche Mechanik wie die Menüleiste, nur ohne Leiste.
    /// Heißt bewusst nicht ContextMenu (CS0104 gegen System.Windows.Forms,
    /// siehe MenuEntry); der Name folgt dem verbreiteten PopupMenu-Idiom.
    /// Der owner in Show liefert Fokus-Kontext, DPI und Besitzerform — der
    /// Fokus WANDERT dabei nie: Das fokussierte Control behält ihn, sieht
    /// aber keine Tastatur, solange das Menü offen ist (MenuModeFilter).
    /// </summary>
    public sealed class PopupMenu : IDisposable
    {
        private readonly List<MenuEntry> _items = new List<MenuEntry>();
        private MenuController _controller;

        public IList<MenuEntry> Items { get { return _items; } }

        public void Show(Control owner, Point screenLocation)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (_items.Count == 0) return;

            if (_controller != null) _controller.Dispose();
            _controller = new MenuController(owner);
            _controller.OpenContext(_items, screenLocation);
        }

        /// <summary>
        /// Wie <see cref="Show"/> (gleiche Wächter, gleicher Controller-
        /// Tausch), platziert das Menü aber als Dropdown UNTERHALB von
        /// screenAnchor (MenuPlacement.PlaceDropdown) statt an einem Punkt —
        /// für DropDownButton-Items im Ribbon (Task 6).
        /// </summary>
        public void ShowBelow(Control owner, Rectangle screenAnchor)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (_items.Count == 0) return;

            if (_controller != null) _controller.Dispose();
            _controller = new MenuController(owner);
            _controller.OpenContext(_items, screenAnchor);
        }

        public void Dispose()
        {
            if (_controller != null)
            {
                _controller.Dispose();
                _controller = null;
            }
        }

        internal MenuController ControllerForTests { get { return _controller; } }
    }
}
