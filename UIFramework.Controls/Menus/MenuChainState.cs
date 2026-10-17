using System.Collections.Generic;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    internal enum MenuKeyActionKind
    {
        None, SelectionChanged, OpenSubmenu, CloseLevel, CloseAll, SwitchBar, Execute
    }

    /// <summary>Was der Controller nach einem Tastendruck TUN muss — die
    /// Maschine selbst fasst keine Fenster an.</summary>
    internal struct MenuKeyAction
    {
        public MenuKeyActionKind Kind;
        public int NewBarIndex;
        public MenuEntry Entry;

        public static MenuKeyAction Of(MenuKeyActionKind kind)
        {
            return new MenuKeyAction { Kind = kind, NewBarIndex = -1 };
        }
    }

    /// <summary>
    /// Ebenen- und Auswahlzustand der offenen Menü-Kette, pur und headless
    /// testbar. Vertrag mit dem Controller: Bei OpenSubmenu ist noch NICHT
    /// gepusht (erst öffnen, dann PushLevel), bei CloseLevel/CloseAll ist
    /// bereits gepoppt (die Maschine weiß es zuerst, das Fenster folgt).
    /// </summary>
    internal sealed class MenuChainState
    {
        private sealed class Level
        {
            public IList<MenuEntry> Entries;
            public int SelectedIndex = -1;
        }

        private readonly List<Level> _levels = new List<Level>();
        private readonly int _barCount;
        private readonly int _barIndex;

        public MenuChainState(int barCount, int barIndex)
        {
            _barCount = barCount;
            _barIndex = barIndex;
        }

        public int Depth { get { return _levels.Count; } }
        public int SelectedIndex(int level) { return _levels[level].SelectedIndex; }
        public IList<MenuEntry> EntriesAt(int level) { return _levels[level].Entries; }

        public void PushLevel(IList<MenuEntry> entries)
        {
            _levels.Add(new Level { Entries = entries });
        }

        public void PopLevel()
        {
            if (_levels.Count > 0) _levels.RemoveAt(_levels.Count - 1);
        }

        public void TruncateTo(int depth)
        {
            // Clamp: bei negativem depth wäre die Schleife sonst endlos, weil
            // PopLevel() bei leerer Liste ein No-op ist (Count sinkt nie unter 0).
            if (depth < 0) depth = 0;
            while (_levels.Count > depth) PopLevel();
        }

        public void SetSelection(int level, int index)
        {
            _levels[level].SelectedIndex = index;
        }

        public void SelectFirstInDeepest()
        {
            var level = Deepest();
            if (level != null) level.SelectedIndex = NextSelectable(level, -1, +1);
        }

        public MenuKeyAction HandleKey(Keys key)
        {
            var level = Deepest();
            if (level == null) return MenuKeyAction.Of(MenuKeyActionKind.None);

            switch (key)
            {
                case Keys.Down: return MoveSelection(level, +1, false);
                case Keys.Up: return MoveSelection(level, -1, false);
                case Keys.Home: return MoveSelection(level, +1, true);
                case Keys.End: return MoveSelection(level, -1, true);
                case Keys.Right: return HandleRight(level);
                case Keys.Left: return HandleLeft();
                case Keys.Enter: return HandleEnter(level);
                case Keys.Escape:
                    PopLevel();
                    return MenuKeyAction.Of(
                        Depth == 0 ? MenuKeyActionKind.CloseAll : MenuKeyActionKind.CloseLevel);
                default:
                    return HandlePossibleMnemonicKey(key);
            }
        }

        public MenuKeyAction HandleMnemonic(char c)
        {
            var level = Deepest();
            if (level == null) return MenuKeyAction.Of(MenuKeyActionKind.None);

            char wanted = char.ToUpperInvariant(c);
            for (int i = 0; i < level.Entries.Count; i++)
            {
                var entry = level.Entries[i];
                if (!entry.IsSelectable) continue;
                if (Mnemonics.FromText(entry.Text) != wanted) continue;

                level.SelectedIndex = i;
                if (entry.HasChildren)
                {
                    var open = MenuKeyAction.Of(MenuKeyActionKind.OpenSubmenu);
                    open.Entry = entry;
                    return open;
                }
                var execute = MenuKeyAction.Of(MenuKeyActionKind.Execute);
                execute.Entry = entry;
                return execute;
            }
            return MenuKeyAction.Of(MenuKeyActionKind.None);
        }

        /// <summary>Letztes (= tiefstes) Level oder null, wenn die Kette leer ist.</summary>
        private Level Deepest()
        {
            return _levels.Count == 0 ? null : _levels[_levels.Count - 1];
        }

        /// <summary>Down/Up (fromEdge=false: ab aktueller Auswahl weiter) bzw.
        /// Home/End (fromEdge=true: ab dem Rand, also -1). Keine Änderung
        /// möglich (kein selektierbarer Eintrag) -> None, sonst SelectionChanged.</summary>
        private MenuKeyAction MoveSelection(Level level, int direction, bool fromEdge)
        {
            int start = fromEdge ? -1 : level.SelectedIndex;
            int next = NextSelectable(level, start, direction);
            if (next == -1) return MenuKeyAction.Of(MenuKeyActionKind.None);

            level.SelectedIndex = next;
            return MenuKeyAction.Of(MenuKeyActionKind.SelectionChanged);
        }

        /// <summary>Nächster selektierbarer Index ab start in direction, zyklisch,
        /// höchstens Count Schritte. Keiner gefunden -> -1. Wrap an den Rändern
        /// (nicht per Modulo auf start+direction — das würde bei start=-1 und
        /// direction=-1 einen Eintrag überspringen statt beim letzten zu landen).</summary>
        private static int NextSelectable(Level level, int start, int direction)
        {
            int count = level.Entries.Count;
            int index = start;
            for (int step = 0; step < count; step++)
            {
                index += direction;
                if (index < 0) index = count - 1;
                else if (index >= count) index = 0;
                if (level.Entries[index].IsSelectable) return index;
            }
            return -1;
        }

        /// <summary>Auswahl mit Kindern -> OpenSubmenu; sonst _barCount > 0 ->
        /// SwitchBar auf die nächste Leiste; sonst None.</summary>
        private MenuKeyAction HandleRight(Level level)
        {
            if (level.SelectedIndex >= 0)
            {
                var entry = level.Entries[level.SelectedIndex];
                if (entry.HasChildren)
                {
                    var open = MenuKeyAction.Of(MenuKeyActionKind.OpenSubmenu);
                    open.Entry = entry;
                    return open;
                }
            }
            if (_barCount > 0)
            {
                var switchBar = MenuKeyAction.Of(MenuKeyActionKind.SwitchBar);
                switchBar.NewBarIndex = (_barIndex + 1) % _barCount;
                return switchBar;
            }
            return MenuKeyAction.Of(MenuKeyActionKind.None);
        }

        /// <summary>Depth > 1 -> PopLevel + CloseLevel; sonst _barCount > 0 ->
        /// SwitchBar auf die vorige Leiste; sonst None.</summary>
        private MenuKeyAction HandleLeft()
        {
            if (Depth > 1)
            {
                PopLevel();
                return MenuKeyAction.Of(MenuKeyActionKind.CloseLevel);
            }
            if (_barCount > 0)
            {
                var switchBar = MenuKeyAction.Of(MenuKeyActionKind.SwitchBar);
                switchBar.NewBarIndex = (_barIndex - 1 + _barCount) % _barCount;
                return switchBar;
            }
            return MenuKeyAction.Of(MenuKeyActionKind.None);
        }

        /// <summary>Keine Auswahl -> None; Kinder -> OpenSubmenu(Entry);
        /// sonst -> Execute(Entry).</summary>
        private MenuKeyAction HandleEnter(Level level)
        {
            if (level.SelectedIndex < 0) return MenuKeyAction.Of(MenuKeyActionKind.None);

            var entry = level.Entries[level.SelectedIndex];
            if (entry.HasChildren)
            {
                var open = MenuKeyAction.Of(MenuKeyActionKind.OpenSubmenu);
                open.Entry = entry;
                return open;
            }
            var execute = MenuKeyAction.Of(MenuKeyActionKind.Execute);
            execute.Entry = entry;
            return execute;
        }

        /// <summary>Buchstaben- und Ziffertasten an HandleMnemonic weiterreichen;
        /// alles andere -> None.</summary>
        private MenuKeyAction HandlePossibleMnemonicKey(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
                return HandleMnemonic((char)('A' + (key - Keys.A)));
            if (key >= Keys.D0 && key <= Keys.D9)
                return HandleMnemonic((char)('0' + (key - Keys.D0)));
            return MenuKeyAction.Of(MenuKeyActionKind.None);
        }
    }
}
