using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace UIFramework.Core.Interop
{
    /// <summary>
    /// Zugriff auf die Fensterattribute des Desktop Window Managers.
    ///
    /// Der Nicht-Client-Bereich eines Fensters — Titelleiste, Titeltext, Rahmen —
    /// gehört Windows, nicht WinForms; Control.OnPaint erreicht ihn nie. DWM ist
    /// der einzige Weg, ihn einzufärben.
    ///
    /// Bewusst keine Windows-Versionsabfrage: DwmSetWindowAttribute liefert für
    /// ein unbekanntes Attribut einfach einen Fehlercode zurück. Das Betriebssystem
    /// beantwortet die Frage "kennst du das?" selbst — zuverlässiger als eine
    /// Build-Nummern-Tabelle im Code, die bei jedem Windows-Release altert.
    /// </summary>
    internal static class Dwm
    {
        /// <summary>Farbe der Titelleiste. Ab Windows 11 (Build 22000).</summary>
        private const int CaptionColorAttribute = 35;

        /// <summary>Farbe des Titeltexts. Ab Windows 11 (Build 22000).</summary>
        private const int TextColorAttribute = 36;

        /// <summary>Farbe des Fensterrahmens. Ab Windows 11 (Build 22000).</summary>
        private const int BorderColorAttribute = 34;

        /// <summary>
        /// Dunkle Fensterchrome. Ab Windows 10 (Build 18985); auf 17763..18984 lag
        /// dasselbe Attribut auf 19. Diese Klasse versucht nur die 20 — auf den
        /// betroffenen alten Builds bleibt die Leiste hell, was ein Schönheitsfehler
        /// ist und kein Absturz.
        /// </summary>
        private const int ImmersiveDarkModeAttribute = 20;

        /// <summary>Rückgabewert von DwmSetWindowAttribute bei Erfolg.</summary>
        private const int Ok = 0;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        /// <summary>
        /// Testnaht: der eigentliche Aufruf gegen DWM, austauschbar für Tests.
        /// Nimmt (Handle, Attribut, Wert) entgegen und liefert zurück, ob DWM den
        /// Wert akzeptiert hat. Voreinstellung ist der echte P/Invoke-Aufruf oben;
        /// ein Test tauscht ihn aus, um zu prüfen, WELCHE Farbe an WELCHES Attribut
        /// geht — ohne einen echten Fensterrahmen zu brauchen. Der bool-Rückgabewert
        /// bleibt hier, weil ein Test damit auch einen DWM-Ablehnungsfall simulieren
        /// kann; die öffentlichen SetXyz-Methoden unten brauchen ihn dagegen nicht,
        /// siehe deren Kommentar. Prozessweiter Zustand — ein Test MUSS den
        /// Originalwert in einem finally wiederherstellen (siehe SkinManagerCollection).
        /// </summary>
        internal static Func<IntPtr, int, int, bool> Setter = RealSetter;

        private static bool RealSetter(IntPtr handle, int attribute, int value)
        {
            int v = value;
            return DwmSetWindowAttribute(handle, attribute, ref v, sizeof(int)) == Ok;
        }

        /// <summary>
        /// Rechnet eine Farbe in ein COLORREF um: 0x00BBGGRR.
        ///
        /// Blau und Rot stehen darin GENAU ANDERSHERUM als in der üblichen
        /// Schreibweise #RRGGBB. Das ist die klassische Falle an dieser Stelle —
        /// vertauscht man sie, erscheint die Titelleiste in einer plausibel
        /// aussehenden, aber falschen Farbe, was beim Draufschauen leicht
        /// durchgeht. Deshalb eine eigene, getestete Funktion.
        ///
        /// Der Alphakanal gehört nicht ins COLORREF: Das oberste Byte muss 0
        /// bleiben, sonst deutet Windows den Wert anders.
        /// </summary>
        internal static int ToColorRef(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }

        /// <summary>
        /// Setzt die Titelleistenfarbe (und die drei Geschwister unten). Kein
        /// Rückgabewert: Auf Windows 10 und älter kennt DWM diese Attribute nicht
        /// und lehnt sie ab, und SkinnedForm hat dafür keinen Ausweichplan — der
        /// einzig sinnvolle Umgang mit "hat nicht geklappt" ist, es zu ignorieren.
        /// Ein bool, das seit der Einführung dieser Klasse an keiner einzigen
        /// Aufrufstelle geprüft wurde, behauptet eine Fehlerbehandlung, die es
        /// nicht gibt. Wer den tatsächlichen Erfolg braucht, hängt sich in Tests
        /// stattdessen in <see cref="Setter"/> ein.
        /// </summary>
        internal static void SetCaptionColor(IntPtr handle, Color color)
        {
            SetColor(handle, CaptionColorAttribute, color);
        }

        internal static void SetCaptionTextColor(IntPtr handle, Color color)
        {
            SetColor(handle, TextColorAttribute, color);
        }

        internal static void SetBorderColor(IntPtr handle, Color color)
        {
            SetColor(handle, BorderColorAttribute, color);
        }

        /// <summary>
        /// Schaltet die dunkle Fensterchrome. Nötig zusätzlich zu den exakten
        /// Farben: Die Glyphen der Systemknöpfe (Minimieren, Maximieren, Schließen)
        /// folgen NICHT der Titeltextfarbe, sondern diesem Schalter. Ohne ihn
        /// stünden dunkle Glyphen auf dunkler Leiste — unsichtbar.
        /// </summary>
        internal static void SetDarkMode(IntPtr handle, bool dark)
        {
            Setter(handle, ImmersiveDarkModeAttribute, dark ? 1 : 0);
        }

        private static void SetColor(IntPtr handle, int attribute, Color color)
        {
            Setter(handle, attribute, ToColorRef(color));
        }
    }
}
