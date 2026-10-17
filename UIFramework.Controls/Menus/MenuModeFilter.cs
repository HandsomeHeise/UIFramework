using System;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Solange der Menü-Modus offen ist, gehört jede Tastatur-Eingabe dem
    /// Menü, nicht dem gerade fokussierten Fenster — dieser Filter fängt sie
    /// VOR dem Zielfenster ab. Maus-Klicks außerhalb der Kette schließen den
    /// Modus; Klicks innerhalb (Kettenfenster oder Besitzer) laufen normal
    /// durch. Der MenuController installiert/entfernt die Instanz selbst
    /// (Application.AddMessageFilter/RemoveMessageFilter).
    /// </summary>
    internal sealed class MenuModeFilter : IMessageFilter
    {
        private readonly MenuController _controller;

        public MenuModeFilter(MenuController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            _controller = controller;
        }

        public bool PreFilterMessage(ref Message m)
        {
            const int WM_KEYDOWN = 0x0100, WM_CHAR = 0x0102, WM_SYSKEYDOWN = 0x0104;
            const int WM_LBUTTONDOWN = 0x0201, WM_RBUTTONDOWN = 0x0204, WM_MBUTTONDOWN = 0x0207;
            const int WM_NCLBUTTONDOWN = 0x00A1, WM_NCRBUTTONDOWN = 0x00A4;
            const int WM_MOUSEWHEEL = 0x020A;

            switch (m.Msg)
            {
                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                    _controller.HandleKey((Keys)unchecked((int)m.WParam.ToInt64()) | Control.ModifierKeys);
                    return true;                       // Menü-Modus schluckt ALLE Tasten
                case WM_CHAR:
                    return true;                       // Reste geschluckter Tasten nicht durchsickern lassen
                case WM_LBUTTONDOWN:
                case WM_RBUTTONDOWN:
                case WM_MBUTTONDOWN:
                    if (_controller.IsWindowInChain(m.HWnd)) return false;
                    _controller.CloseAll();
                    return true;                       // der erste Außenklick schließt NUR
                case WM_NCLBUTTONDOWN:
                case WM_NCRBUTTONDOWN:
                    _controller.CloseAll();
                    return false;                      // Titelleisten-Klick wirkt (Fenster ziehen)
                case WM_MOUSEWHEEL:
                    return true;                       // Menüs scrollen nicht (v1)
            }
            return false;
        }
    }
}
