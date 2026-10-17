using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Dropdown mit fester Liste. Baut auf ButtonEditBase auf: keine native
    /// Textzone (HasNativeTextCore = false — reine Auswahl, kein Freitext,
    /// kein Caret), der Pfeil rechts ist der eine Knopf der Basis
    /// (AddButton), das aufgeklappte Popup kommt vom Popup-Anker der Basis
    /// (OpenPopup/ClosePopup) als ListContent.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public class SkinComboBox : ButtonEditBase
    {
        private readonly List<object> _items = new List<object>();
        private int _selectedIndex = -1;

        public SkinComboBox()
        {
            AddButton(EditorGlyph.ArrowDown, Toggle);
        }

        protected override bool HasNativeTextCore
        {
            get { return false; }   // reine Auswahlliste: kein Freitext, kein Caret
        }

        protected override string ElementKey
        {
            get { return ElementKeys.ComboBox; }
        }

        protected override bool IsSelected
        {
            get { return IsPopupOpen; }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public IList<object> Items
        {
            get { return _items; }
        }

        [Browsable(false)]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value < -1 || value >= _items.Count)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (_selectedIndex == value) return;

                _selectedIndex = value;
                Invalidate();
                OnSelectedIndexChanged(EventArgs.Empty);
            }
        }

        [Browsable(false)]
        public object SelectedItem
        {
            get { return _selectedIndex >= 0 ? _items[_selectedIndex] : null; }
            set { SelectedIndex = _items.IndexOf(value); }
        }

        public event EventHandler SelectedIndexChanged;

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            var handler = SelectedIndexChanged;
            if (handler != null) handler(this, e);
        }

        protected override void PaintTextZone(Graphics g, Rectangle bounds, ElementAppearance appearance)
        {
            string text = SelectedItem != null ? SelectedItem.ToString() : "";
            if (!string.IsNullOrEmpty(text))
                SkinPainter.DrawText(g, text, bounds, appearance, DeviceDpi, ContentAlignment.MiddleLeft);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);   // markiert ggf. einen Knopf als gedrückt

            // Ein Combo klappt bei Klick IRGENDWO auf, nicht nur auf dem Pfeil.
            // War der Klick auf dem Pfeilknopf, hat die Basis ihn bereits als
            // gedrückt markiert und löst Toggle über AddButton beim Loslassen
            // aus — hier nochmal togglen würde ihn doppelt schalten.
            if (e.Button == MouseButtons.Left && Enabled && !IsButtonPressed)
            {
                if (CanFocus) Focus();
                Toggle();
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Down || keyData == Keys.Up || keyData == Keys.Space ||
                keyData == Keys.Enter || keyData == Keys.Escape)
                return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && !IsPopupOpen)
            {
                OpenList();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && IsPopupOpen)
            {
                ClosePopup();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                RaiseEditConfirmed();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                RaiseEditCancelled();
                e.Handled = true;
            }
            else if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) && _items.Count > 0)
            {
                int next = SelectedIndex + (e.KeyCode == Keys.Down ? 1 : -1);
                if (next >= 0 && next < _items.Count) SelectedIndex = next;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override object GetEditValue()
        {
            return SelectedItem;
        }

        protected override void SetEditValue(object value)
        {
            SelectedItem = value;   // IndexOf: nicht gefunden => -1 = keine Auswahl
        }

        private void Toggle()
        {
            // Der Pfeilknopf togglet erst bei MouseUp (AddButton-Callback) —
            // dazwischen kann ein wegen Deaktivierung aufgeschobenes Close
            // (siehe PopupHost.OnDeactivate) das Popup schon geschlossen
            // haben. IsPopupOpen allein läse dann "zu" und öffnete hier
            // fälschlich neu; darum zählt auch der Schnappschuss vom
            // MouseDown (PopupWasOpenAtMouseDown). ClosePopup() bleibt in
            // beiden Fällen ein no-op, falls das Popup schon zu ist.
            if (IsPopupOpen || PopupWasOpenAtMouseDown) ClosePopup(); else OpenList();
        }

        private void OpenList()
        {
            if (_items.Count == 0) return;

            var list = new ListContent(_items, () => _selectedIndex);
            list.ItemChosen += index => { SelectedIndex = index; };
            OpenPopup(list);
        }
    }
}
