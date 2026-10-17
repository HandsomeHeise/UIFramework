using System.Windows.Forms;

namespace UIFramework.Core.Controls
{
    /// <summary>
    /// Ein Control, das App-weite Tastenkürzel verarbeiten kann — unabhängig
    /// vom Fokus und unabhängig davon, ob gerade ein Menü offen ist. Hier in
    /// Core deklariert, obwohl der einzige heutige Implementierer (MenuBar)
    /// in der Controls-Assembly lebt: SkinnedForm.ProcessCmdKey fragt
    /// rekursiv alle IShortcutHandler seiner Control-Hierarchie ab, und die
    /// Referenzrichtung des Frameworks läuft ausschließlich Core &lt;- Controls
    /// — Core darf die Menüklassen nicht kennen. Ein Treffer schließt eine
    /// evtl. offene Menü-Kette, feuert den Eintrag und meldet true; sonst
    /// false, damit der nächste Handler in der Hierarchie gefragt wird.
    /// </summary>
    public interface IShortcutHandler
    {
        bool ProcessShortcut(Keys keyData);
    }
}
