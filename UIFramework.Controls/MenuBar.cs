using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Die Menüleiste einer Anwendung: eine Reihe von Top-Level-Einträgen
    /// ("&amp;Datei", "&amp;Ansicht", …), deren Untermenüs über einen
    /// <see cref="MenuController"/> laufen. Drei Wege führen zu einem
    /// Eintrag: ein Klick (<see cref="OnMouseDown"/> öffnet/toggelt sein
    /// Dropdown), Alt+Mnemonic (<see cref="ProcessMnemonic"/> öffnet das
    /// passende Dropdown mit dem ersten Eintrag vorausgewählt) und ein
    /// App-weites Tastenkürzel (<see cref="ProcessShortcut"/> — feuert auch
    /// bei geschlossenem Menü, siehe SkinnedForm.ProcessCmdKey). Die Leiste
    /// selbst nimmt dabei NIE den Fokus (SetStyle(ControlStyles.Selectable,
    /// false) im Konstruktor, dieselbe Technik wie die waagerechte Grid-
    /// Bildlaufleiste) — solange eine Kette offen ist, bedient ausschließlich
    /// der MenuController über seinen MessageFilter die Tastatur.
    /// </summary>
    public class MenuBar : SkinnedControl, IShortcutHandler, IBarHome
    {
        private readonly List<MenuEntry> _items = new List<MenuEntry>();
        private readonly MenuController _controller;
        private Rectangle[] _itemBounds = new Rectangle[0];
        private int _hoverIndex = -1;

        public MenuBar()
        {
            // Kerninvariante der Spec: die Leiste nimmt nie den Fokus.
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            _controller = new MenuController(this);
            _controller.Closed += (s, e) => Invalidate();
        }

        /// <summary>Die Top-Level-Einträge, von links nach rechts.</summary>
        public IList<MenuEntry> Items
        {
            get { return _items; }
        }

        protected override string ElementKey
        {
            get { return ElementKeys.MenuBar; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);

            // Messen IMMER mit Normal (Muster MenuContent.Measure): sonst
            // sprängen Item-Breiten je nach Hover/Auswahl auseinander, weil
            // Padding/Rahmen einer anderen Erscheinung leicht abweichen könnten.
            var measureAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuBarItem, ElementState.Normal);

            _itemBounds = new Rectangle[_items.Count];
            int x = content.Left;

            for (int i = 0; i < _items.Count; i++)
            {
                var entry = _items[i];
                var itemSize = SkinPainter.InflateByPadding(
                    SkinPainter.MeasureMnemonicText(g, entry.Text, measureAppearance, DeviceDpi),
                    measureAppearance, DeviceDpi);

                var bounds = new Rectangle(x, content.Top, itemSize.Width, content.Height);
                _itemBounds[i] = bounds;
                x = bounds.Right;

                ElementState state;
                if (!entry.Enabled) state = ElementState.Disabled;
                else if (i == _controller.BarIndex) state = ElementState.Selected;
                else if (i == _hoverIndex) state = ElementState.Hovered;
                else state = ElementState.Normal;

                var itemAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuBarItem, state);
                SkinPainter.DrawBackground(g, bounds, itemAppearance, DeviceDpi);
                SkinPainter.DrawMnemonicText(g, entry.Text, bounds, itemAppearance, DeviceDpi,
                    ContentAlignment.MiddleCenter);
            }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var itemAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuBarItem, ElementState.Normal);
            var barAppearance = CurrentAppearance;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var textSize = SkinPainter.MeasureMnemonicText(g, "Xg", itemAppearance, DeviceDpi);
                var itemSize = SkinPainter.InflateByPadding(textSize, itemAppearance, DeviceDpi);
                var barSize = SkinPainter.InflateByPadding(itemSize, barAppearance, DeviceDpi);

                // Breite: Elternvorgabe — die Leiste ist so breit wie ihr Host
                // (typischerweise Dock=Top), nicht wie ihr eigener Inhalt.
                return new Size(proposedSize.Width, barSize.Height);
            }
        }

        /// <summary>
        /// Fahrprobe-Befund F1: DockStyle.Top übernimmt beim Layout die aktuelle
        /// Height 1:1 — GetPreferredSize wird dabei NUR konsultiert, wenn das
        /// Control AutoSize=true trägt (das hier bewusst nicht gesetzt ist, siehe
        /// GetPreferredSize-Kommentar: die Breite bleibt Elternvorgabe, ein
        /// AutoSize-Control würde WinForms aber auch die Breite selbst bestimmen
        /// lassen wollen). Ohne eigene Höhenverwaltung bleibt Height auf
        /// Control.DefaultSize stehen, und Dock=Top zeigt einen 0 px hohen
        /// Streifen (am echten Fenster bei 120 dpi vermessen: 700×0). Die Leiste
        /// setzt ihre Höhe deshalb selbst — an drei Stellen, s.u.
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Frühestmöglicher zuverlässiger Zeitpunkt: DeviceDpi ist beim
            // Erzeugen des Fensterhandles bereits der echte Wert des
            // Zielmonitors (anders als im Konstruktor, wo er noch die
            // Design-/Thread-Vorgabe trägt).
            ApplyPreferredHeight();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            // Monitorwechsel zur Laufzeit ändert die Schriftgröße des Skins und
            // damit den Höhenbedarf — vor dem Invalidate() der Basisklasse
            // (SkinnedControl), damit das erste Repaint schon mit der neuen
            // Höhe zeichnet.
            ApplyPreferredHeight();
            base.OnDpiChangedAfterParent(e);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // Selbstheilende Bremse gegen Doppel-Skalierung: MainForm.OnLoad
            // ruft Scale(factor) auf ALLEN Kindern auf (siehe Kommentar dort),
            // was eine bereits bei echtem DeviceDpi korrekt gesetzte Height
            // (siehe OnHandleCreated) ein zweites Mal multiplizieren würde —
            // ein Skin-Wechsel mit anderer Schrift/anderem Padding braucht
            // ebenfalls ein Nachziehen. Empirisch nachvollzogen (Diagnose-Probe
            // gegen ein echtes Form): Control.Scale(SizeF) löst intern über
            // Suspend-/ResumeLayout selbst genau einen OnLayout-Durchlauf auf
            // dem skalierten Control aus — diese Bremse hier reicht deshalb
            // aus, ohne eigenen Scale()/ScaleControl()-Override. Der
            // Gleichheitstest in ApplyPreferredHeight ist zugleich die
            // Konvergenzbedingung: eine zweite, durch diese Zuweisung selbst
            // ausgelöste Runde trifft den Zielwert bereits und bleibt aus —
            // keine Endlosschleife (Muster: SkinnedForm.ApplyCaptionIfChanged).
            ApplyPreferredHeight();
        }

        /// <summary>
        /// Setzt NUR die Höhe neu (nie Size als Ganzes — die Breite bleibt
        /// Elternvorgabe, siehe GetPreferredSize) und nur, wenn sie vom
        /// aktuellen Bedarf abweicht. Diese Bedingung ist die Bremse gegen
        /// Endlosschleifen bei den Aufrufern und macht wiederholte Aufrufe
        /// (OnHandleCreated, OnDpiChangedAfterParent, OnLayout) no-op-sicher.
        /// </summary>
        private void ApplyPreferredHeight()
        {
            int preferred = GetPreferredSize(Size.Empty).Height;
            if (Height != preferred) Height = preferred;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hit = HitTest(e.Location);

            if (hit != _hoverIndex)
            {
                _hoverIndex = hit;
                Invalidate();

                // Hot-Tracking: bei offener Kette folgt das Dropdown der Maus
                // über die Leiste ohne erneuten Klick — nur beim tatsächlichen
                // Wechsel auf ein ANDERES selektierbares Item, sonst öffnete
                // jede Mausbewegung über demselben Item die Kette neu und würfe
                // die Hover-Auswahl im gerade offenen Dropdown weg.
                if (_controller.IsOpen && hit >= 0 && hit != _controller.BarIndex && IsBarItemSelectable(hit))
                    _controller.OpenBarDropdown(this, hit, false);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int hit = HitTest(e.Location);
                if (hit >= 0 && IsBarItemSelectable(hit))
                {
                    if (_controller.IsOpen && _controller.BarIndex == hit)
                        _controller.CloseAll();
                    else
                        _controller.OpenBarDropdown(this, hit, false);
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            // Hover-Highlight muss gelöscht werden, wenn die Maus die Leiste
            // verlässt — OnMouseMove setzt _hoverIndex, aber nur ein
            // OnMouseLeave-Override kann es zurücksetzen. Die no-op-Suppression
            // invalidiert nur, wenn sich tatsächlich etwas ändert.
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }

            base.OnMouseLeave(e);
        }

        protected override bool ProcessMnemonic(char charCode)
        {
            char wanted = char.ToUpperInvariant(charCode);
            for (int i = 0; i < _items.Count; i++)
            {
                if (Mnemonics.FromText(_items[i].Text) != wanted) continue;
                if (!IsBarItemSelectable(i)) continue;

                _controller.OpenBarDropdown(this, i, true);
                return true;
            }
            // Gleiche Bedingung wie Klick und Hot-Tracking: ein Top-Level-
            // Eintrag ohne Kinder oder disabled klappt auch per Alt nichts auf.
            return base.ProcessMnemonic(charCode);
        }

        /// <summary>App-weites Kürzel — feuert auch bei geschlossenem Menü
        /// (SkinnedForm.ProcessCmdKey ruft dies über alle IShortcutHandler
        /// seiner Hierarchie).</summary>
        public bool ProcessShortcut(Keys keyData)
        {
            var hit = MenuShortcuts.Find(_items, keyData);
            if (hit == null) return false;

            _controller.ExecuteEntry(hit);
            return true;
        }

        private int HitTest(Point location)
        {
            for (int i = 0; i < _itemBounds.Length; i++)
                if (_itemBounds[i].Contains(location)) return i;
            return -1;
        }

        /// <summary>Ein Top-Level-Eintrag ohne Kinder klappt nichts auf — weder
        /// per Klick noch per Alt+Mnemonic noch per Hot-Tracking.</summary>
        private bool IsBarItemSelectable(int index)
        {
            return _items[index].IsSelectable && _items[index].HasChildren;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _controller.Dispose();
            base.Dispose(disposing);
        }

        // ---- IBarHome (internal — nur der MenuController kennt dieses Interface) --

        int IBarHome.BarItemCount
        {
            get { return _items.Count; }
        }

        IList<MenuEntry> IBarHome.BarItems(int index)
        {
            return _items[index].Items;
        }

        Rectangle IBarHome.BarItemScreenBounds(int index)
        {
            return RectangleToScreen(_itemBounds[index]);
        }

        bool IBarHome.IsBarItemSelectable(int index)
        {
            return IsBarItemSelectable(index);
        }

        // ---- Nur für Tests --------------------------------------------------

        internal Rectangle[] ItemBoundsForTests()
        {
            return _itemBounds;
        }

        internal MenuController ControllerForTests
        {
            get { return _controller; }
        }

        /// <summary>ProcessMnemonic ist protected und sonst nicht erreichbar.</summary>
        internal bool PerformMnemonicForTests(char charCode)
        {
            return ProcessMnemonic(charCode);
        }

        /// <summary>Der aktuelle Hover-Index für Tests.</summary>
        internal int HoverIndexForTests
        {
            get { return _hoverIndex; }
        }

        /// <summary>OnMouseMove ist protected und sonst nicht erreichbar.</summary>
        internal void PerformMouseMoveForTests(Point location)
        {
            OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, location.X, location.Y, 0));
        }

        /// <summary>OnMouseLeave ist protected und sonst nicht erreichbar.</summary>
        internal void PerformMouseLeaveForTests()
        {
            OnMouseLeave(EventArgs.Empty);
        }
    }
}
