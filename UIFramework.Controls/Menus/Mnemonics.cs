namespace UIFramework.Controls
{
    /// <summary>'&'-Konvention wie WinForms: "&Datei" markiert D,
    /// "&&" ist ein Literal. Ein Ort für die Regel — Zeichnen
    /// (SkinPainter.DrawMnemonicText) und Tastaturvergleich müssen dieselbe
    /// Deutung haben, sonst springt Alt+X auf den falschen Eintrag.</summary>
    internal static class Mnemonics
    {
        /// <summary>Der Mnemonic-Buchstabe, großgeschrieben — oder '\0'.</summary>
        public static char FromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return '\0';

            for (int i = 0; i < text.Length - 1; i++)
            {
                if (text[i] != '&') continue;
                if (text[i + 1] == '&') { i++; continue; }
                return char.ToUpperInvariant(text[i + 1]);
            }
            return '\0';
        }
    }
}
