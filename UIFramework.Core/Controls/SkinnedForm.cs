using System;
using System.Windows.Forms;
using UIFramework.Core.Interop;
using UIFramework.Core.Skinning;

namespace UIFramework.Core.Controls
{
    /// <summary>
    /// Ein Fenster, dessen Titelleiste, Titeltext und Rahmen dem aktiven Skin
    /// folgen. Anwendungen erben davon und bekommen das ohne weiteres Zutun.
    ///
    /// Warum das nicht wie bei SkinnedControl über OnPaint geht: Die Titelleiste
    /// ist der Nicht-Client-Bereich und gehört Windows. OnPaint erreicht sie nie.
    /// Der einzige Weg führt über DWM-Fensterattribute (siehe Dwm).
    ///
    /// Wie sie vom Skin-Wechsel erfährt: Sie registriert sich — schwach, wie jedes
    /// Control — beim SkinManager und reagiert auf das Invalidate, das
    /// SkinManager.InvalidateAll() daraufhin auslöst. BEWUSST kein Abo auf
    /// SkinManager.SkinChanged: Ein statisches Event hielte jedes je erzeugte
    /// Fenster am Leben — genau das Leck, das die schwache Registrierung
    /// verhindert und vor dem der SkinManager selbst warnt.
    ///
    /// Diese Klasse färbt sowohl die Titelleiste (Nicht-Client-Bereich, über DWM)
    /// als auch die eigene Fläche (BackColor) im selben Ton — beide aus
    /// Window/Normal. Ein bloßes SkinnedForm ohne eigene Kind-Controls ist damit
    /// bereits vollständig eingefärbt; wer strukturierten Inhalt braucht, füllt
    /// das Fenster zusätzlich mit einem SkinPanel (oder einem gleichwertigen
    /// Control), dessen Rand und abgerundete Ecken dann konsequenterweise auf
    /// dieselbe Fensterfläche treffen statt auf Windows-Standardgrau.
    /// </summary>
    public class SkinnedForm : Form
    {
        private ElementAppearance _applied;
        private int _captionApplyCount;

        public SkinnedForm()
        {
            SkinManager.Register(this);
        }

        /// <summary>
        /// Wie oft die Titelleiste tatsächlich an Windows geschoben wurde.
        /// Nur für Tests: der Merker unten lässt sich sonst nicht nachweisen.
        /// </summary>
        internal int CaptionApplyCount
        {
            get { return _captionApplyCount; }
        }

        /// <summary>
        /// Dunkel oder hell — aus Skin-Daten abgeleitet, nicht aus einem Flag.
        /// Ist der Titeltext heller als die Leiste, ist es eine dunkle Leiste.
        /// So muss ein dritter Skin nichts zusätzlich deklarieren, und es gibt
        /// kein ISkin.IsDark, das mit den Farben aus dem Tritt geraten könnte.
        /// </summary>
        internal static bool IsDarkCaption(ElementAppearance appearance)
        {
            return appearance.ForeColor.GetBrightness() > appearance.Background.GetBrightness();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Ein neues Fensterhandle trägt die Standard-Chrome von Windows; der
            // Merker unten galt nur für das alte Handle. WinForms erzeugt bei
            // gewöhnlichen Eigenschaftsänderungen (z. B. ShowInTaskbar,
            // RightToLeft) klammheimlich ein neues HWND — ohne dieses
            // Zurücksetzen würde ReferenceEquals unten das erneute Anwenden
            // überspringen, und das neue Fenster bliebe unskinnt.
            _applied = null;
            ApplyCaptionIfChanged();
        }

        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);
            ApplyCaptionIfChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) SkinManager.Unregister(this);
            base.Dispose(disposing);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Die Form kennt die Menüklassen der Controls-Assembly nicht (Referenz-
            // richtung Core <- Controls) — sie fragt alle IShortcutHandler in ihrer
            // Hierarchie. So bekommen SkinnedForm-Konsumenten Menü-Shortcuts
            // geschenkt; auf einer fremden Form ruft man MenuBar.ProcessShortcut
            // selbst (eine dokumentierte Zeile).
            if (DispatchShortcut(Controls, keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Voll qualifiziert: Form deklariert selbst ein verschachteltes
        // ControlCollection (das MDI-Sonderfälle behandelt), das den
        // unqualifizierten Namen hier verdeckt. Controls liefert aber
        // weiterhin den Basistyp Control.ControlCollection — ohne die
        // Qualifikation lehnt der Compiler die Zuweisung ab (CS1503).
        private static bool DispatchShortcut(Control.ControlCollection controls, Keys keyData)
        {
            foreach (Control child in controls)
            {
                var handler = child as IShortcutHandler;
                if (handler != null && handler.ProcessShortcut(keyData)) return true;
                if (child.HasChildren && DispatchShortcut(child.Controls, keyData)) return true;
            }
            return false;
        }

        internal bool PerformShortcutForTests(Keys keyData)
        {
            var message = new Message();
            return ProcessCmdKey(ref message, keyData);
        }

        private void ApplyCaptionIfChanged()
        {
            // Ohne Fenster gibt es nichts einzufärben.
            if (!IsHandleCreated) return;

            var appearance = SkinManager.Current.GetAppearance(ElementKeys.Window, ElementState.Normal);

            // Ein Skin liefert für denselben Schlüssel stets dieselbe Instanz,
            // und seit dem Einfrieren in SkinBase.Define ist das eine erzwungene
            // Zusicherung statt einer Konvention: Niemand kann eine Erscheinung
            // unter uns wegändern. OnInvalidated feuert bei jedem Neuzeichnen —
            // ohne diesen Vergleich ginge jedes Mal ein Schwung P/Invoke raus.
            if (ReferenceEquals(appearance, _applied)) return;
            _applied = appearance;
            _captionApplyCount++;

            // Die Fläche des Fensters selbst, nicht nur die Titelleiste: sonst
            // blitzt an unskinnten Rändern (z. B. den abgerundeten Ecken eines
            // SkinPanel mit Dock=Fill, dessen SkinnedControl absichtlich
            // transparent zeichnet, um die Elternfarbe durchscheinen zu lassen)
            // das Windows-Standardgrau von SystemColors.Control durch. Das
            // Setzen löst selbst Invalidate → OnInvalidated aus, aber _applied
            // ist bereits zugewiesen, der ReferenceEquals-Merker oben greift
            // beim Reentry also sofort — keine Endlosschleife.
            BackColor = appearance.Background;

            // Zuerst der Schalter: Die Glyphen der Systemknöpfe folgen ihm, nicht
            // der Titeltextfarbe. Ohne ihn stünden dunkle Glyphen auf dunkler Leiste.
            Dwm.SetDarkMode(Handle, IsDarkCaption(appearance));

            // Dann die exakten Farben. Auf Windows 10 und älter kennt DWM diese
            // Attribute nicht und lehnt sie ab — dann bleibt es beim Schalter oben.
            Dwm.SetCaptionColor(Handle, appearance.Background);
            Dwm.SetCaptionTextColor(Handle, appearance.ForeColor);
            Dwm.SetBorderColor(Handle, appearance.BorderColor);
        }
    }
}
