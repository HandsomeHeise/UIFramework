using System.Collections.Generic;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>Sucht das Kürzel im Eintragsbaum. Disabled-Einträge feuern
    /// nicht, ein Disabled-Elter sperrt seinen ganzen Teilbaum (was der
    /// Anwender nicht erreichen kann, darf auch kein Kürzel erreichen),
    /// und Eltern mit Kindern führen selbst nie aus.</summary>
    internal static class MenuShortcuts
    {
        public static MenuEntry Find(IList<MenuEntry> entries, Keys keyData)
        {
            if (entries == null) return null;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.IsSeparator || !entry.Enabled) continue;

                if (entry.HasChildren)
                {
                    var hit = Find(entry.Items, keyData);
                    if (hit != null) return hit;
                }
                else if (entry.Shortcut != Keys.None && entry.Shortcut == keyData)
                {
                    return entry;
                }
            }
            return null;
        }
    }
}
