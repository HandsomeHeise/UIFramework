using System.Text;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>"Ctrl+Shift+S" in fester Reihenfolge. Bewusst kein
    /// KeysConverter: dessen Ausgabe ist kulturabhängig und damit weder
    /// test- noch skin-editor-tauglich (Teilprojekt 6 zeigt Kürzel an).</summary>
    internal static class ShortcutDisplay
    {
        public static string Format(Keys shortcut)
        {
            if (shortcut == Keys.None) return string.Empty;

            var text = new StringBuilder();
            if ((shortcut & Keys.Control) != 0) text.Append("Ctrl+");
            if ((shortcut & Keys.Shift) != 0) text.Append("Shift+");
            if ((shortcut & Keys.Alt) != 0) text.Append("Alt+");
            text.Append(KeyName(shortcut & Keys.KeyCode));
            return text.ToString();
        }

        private static string KeyName(Keys key)
        {
            // D0..D9 hießen sonst "D1" — angezeigt wird die Ziffer.
            if (key >= Keys.D0 && key <= Keys.D9)
                return ((char)('0' + (key - Keys.D0))).ToString();
            return key.ToString();
        }
    }
}
