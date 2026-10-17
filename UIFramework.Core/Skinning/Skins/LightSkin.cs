using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Core.Skinning.Skins
{
    /// <summary>
    /// Heller Skin. Zusammen mit DarkSkin die EINZIGE Stelle im Framework,
    /// die Farbwerte enthalten darf — Task 14 erzwingt das maschinell.
    /// </summary>
    public sealed class LightSkin : SkinBase
    {
        private static readonly FontSpec BodyFont = new FontSpec("Segoe UI", 9f);

        private static readonly Color Surface = Color.FromArgb(255, 250, 250, 250);
        private static readonly Color SurfaceRaised = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color BorderSubtle = Color.FromArgb(255, 214, 214, 218);
        private static readonly Color BorderStrong = Color.FromArgb(255, 176, 176, 182);
        private static readonly Color TextPrimary = Color.FromArgb(255, 28, 28, 32);
        private static readonly Color TextDisabled = Color.FromArgb(255, 160, 160, 166);
        private static readonly Color Accent = Color.FromArgb(255, 0, 102, 204);
        private static readonly Color AccentHover = Color.FromArgb(255, 0, 118, 234);
        private static readonly Color AccentPressed = Color.FromArgb(255, 0, 84, 168);
        private static readonly Color DisabledFill = Color.FromArgb(255, 236, 236, 239);

        public override string Name
        {
            get { return "Light"; }
        }

        public LightSkin()
        {
            DefineButton();
            DefinePanel();
            DefineLabel();
            DefineFocus();
            DefineWindow();
            DefineScrollBar();
            DefineGrid();
            DefineTextBox();
            DefineComboBox();
            DefineTab();
            DefineEditorButton();
            DefineCheckBox();
            DefineCalendar();
            DefineMenu();
            DefineRibbon();
            DefineGallery();
            DefineExplorer();
        }

        private void DefineButton()
        {
            Define(ElementKeys.Button, ElementState.Normal, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });

            Define(ElementKeys.Button, ElementState.Hovered, new ElementAppearance
            {
                Background = AccentHover,
                BorderColor = AccentHover,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });

            Define(ElementKeys.Button, ElementState.Pressed, new ElementAppearance
            {
                Background = AccentPressed,
                BorderColor = AccentPressed,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });

            Define(ElementKeys.Button, ElementState.Selected, new ElementAppearance
            {
                Background = AccentPressed,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });

            Define(ElementKeys.Button, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });
        }

        private void DefinePanel()
        {
            Define(ElementKeys.Panel, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(6),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8)
            });

            Define(ElementKeys.Panel, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(6),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(8)
            });
        }

        private void DefineLabel()
        {
            Define(ElementKeys.Label, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.Label, ElementState.Disabled, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(0)
            });
        }

        private void DefineFocus()
        {
            Define(ElementKeys.Focus, ElementState.Normal, new ElementAppearance
            {
                Background = Color.Transparent,
                BorderColor = TextPrimary,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });
        }

        private void DefineWindow()
        {
            Define(ElementKeys.Window, ElementState.Normal, new ElementAppearance
            {
                // Der Ton der Fläche darunter (siehe DefinePanel), damit die
                // Leiste optisch in den Client-Bereich übergeht.
                Background = Surface,
                BorderColor = BorderSubtle,
                // Steuert nichts — die Rahmengeometrie gehört Windows. Aber
                // BorderColor oben geht an DWMWA_BORDER_COLOR, und Windows
                // zeichnet daraufhin einen 1px-Rahmen. 0 wäre eine Falschaussage
                // gegenüber jedem, der diese Zeile liest oder anzeigt.
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });
        }

        private void DefineScrollBar()
        {
            // Rinne: etwas abgesetzt von der Fläche, damit die Leiste als eigenes
            // Element lesbar bleibt.
            Define(ElementKeys.ScrollBar, ElementState.Normal, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.ScrollBar, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.ScrollBarThumb, ElementState.Normal, new ElementAppearance
            {
                Background = BorderStrong,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.ScrollBarThumb, ElementState.Hovered, new ElementAppearance
            {
                Background = TextDisabled,
                BorderColor = TextDisabled,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.ScrollBarThumb, ElementState.Pressed, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(2)
            });
        }

        private void DefineGrid()
        {
            // Die Fläche unter der letzten Zeile: bewusst der Grundton, nicht der
            // Zellton — so ist sichtbar, wo die Daten enden.
            Define(ElementKeys.Grid, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.GridHeader, ElementState.Normal, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 5, 8, 5)
            });

            Define(ElementKeys.GridHeader, ElementState.Hovered, new ElementAppearance
            {
                Background = BorderSubtle,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 5, 8, 5)
            });

            Define(ElementKeys.GridHeader, ElementState.Pressed, new ElementAppearance
            {
                Background = BorderStrong,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 5, 8, 5)
            });

            Define(ElementKeys.GridCell, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.GridCell, ElementState.Hovered, new ElementAppearance
            {
                Background = Surface,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.GridCell, ElementState.Selected, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.GridCell, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });
        }

        private void DefineTextBox()
        {
            Define(ElementKeys.TextBox, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });

            Define(ElementKeys.TextBox, ElementState.Hovered, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });

            Define(ElementKeys.TextBox, ElementState.Selected, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });

            Define(ElementKeys.TextBox, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });
        }

        private void DefineComboBox()
        {
            Define(ElementKeys.ComboBox, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.ComboBox, ElementState.Hovered, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.ComboBox, ElementState.Selected, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.ComboBox, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.ComboBoxList, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });
        }

        private void DefineTab()
        {
            Define(ElementKeys.Tab, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(14, 8, 14, 8)
            });

            Define(ElementKeys.Tab, ElementState.Hovered, new ElementAppearance
            {
                Background = BorderSubtle,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(14, 8, 14, 8)
            });

            Define(ElementKeys.Tab, ElementState.Selected, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Accent,
                BorderWidth = 2,
                Corners = CornerRadius.None,
                ForeColor = Accent,
                Font = BodyFont,
                Padding = new Padding(14, 8, 14, 8)
            });

            Define(ElementKeys.Tab, ElementState.Disabled, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(14, 8, 14, 8)
            });
        }

        private void DefineEditorButton()
        {
            // Bewusst flacher als ein SkinButton: Der Knopf sitzt IN einem Editor,
            // dessen Rahmen die Kontur schon liefert.
            Define(ElementKeys.EditorButton, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.EditorButton, ElementState.Hovered, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.EditorButton, ElementState.Pressed, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.EditorButton, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(2)
            });
        }

        private void DefineCheckBox()
        {
            // Das Control als Ganzes: Fläche wie Label (siehe DefineLabel) —
            // verschmilzt mit dem Panel darunter, kein Rahmen ums Ganze. Nur
            // Normal und Disabled definiert, genau wie Label: Hovered/Pressed/
            // Selected fallen bewusst auf Normal zurück (Rückfallkette, siehe
            // SkinBase) — der Text färbt bei Hover NICHT um, das übernimmt
            // allein die Box (CheckBoxIndicator). Padding bleibt bei 2, nicht
            // Labels 0: CheckEdit.PaintContent/GetPreferredSize misst Box und
            // Text über GetContentRectangle/InflateByPadding dieser Erscheinung.
            Define(ElementKeys.CheckBox, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.CheckBox, ElementState.Disabled, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            // Die gezeichnete Box — unverändert aus der bisherigen
            // CheckBox-Definition übernommen, nur der Schlüssel ist neu.
            // ForeColor ist die Hakenfarbe. Selected fällt bewusst auf Normal
            // zurück — "angehakt" zeigt der Haken, nicht die Fläche.
            Define(ElementKeys.CheckBoxIndicator, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = Accent,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.CheckBoxIndicator, ElementState.Hovered, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = AccentHover,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.CheckBoxIndicator, ElementState.Pressed, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = AccentPressed,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = AccentPressed,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.CheckBoxIndicator, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(2)
            });
        }

        private void DefineCalendar()
        {
            // Tage: randlos, die Auswahl trägt die Fläche. Disabled = Nachbarmonat.
            Define(ElementKeys.CalendarDay, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(4)
            });

            Define(ElementKeys.CalendarDay, ElementState.Hovered, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(4)
            });

            Define(ElementKeys.CalendarDay, ElementState.Selected, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(4)
            });

            Define(ElementKeys.CalendarDay, ElementState.Disabled, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(4)
            });

            Define(ElementKeys.CalendarHeader, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(4)
            });

            Define(ElementKeys.CalendarHeader, ElementState.Hovered, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(4)
            });

            Define(ElementKeys.CalendarToday, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = Accent,
                Font = BodyFont,
                Padding = new Padding(4)
            });

            Define(ElementKeys.CalendarToday, ElementState.Hovered, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = AccentHover,
                Font = BodyFont,
                Padding = new Padding(4)
            });
        }

        private void DefineMenu()
        {
            // Die Leiste verschmilzt mit der Fläche darunter — wie die Titelleiste
            // (Window nutzt denselben Surface-Ton). Kein Rahmen: Der Streifen grenzt
            // sich durch die Popups und Hover-Flächen ab, nicht durch eine Linie.
            Define(ElementKeys.MenuBar, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(4, 2, 4, 2)
            });

            Define(ElementKeys.MenuBarItem, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(10, 4, 10, 4)
            });

            Define(ElementKeys.MenuBarItem, ElementState.Hovered, new ElementAppearance
            {
                Background = BorderSubtle,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(10, 4, 10, 4)
            });

            // Selected = Dropdown offen: die erhöhte Fläche des Popups zieht bis in
            // die Leiste hinein, Eintrag und Popup lesen sich als ein Stück.
            Define(ElementKeys.MenuBarItem, ElementState.Selected, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(10, 4, 10, 4)
            });

            Define(ElementKeys.MenuBarItem, ElementState.Disabled, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(10, 4, 10, 4)
            });

            // Popup-Rahmen wie ComboBoxList — dasselbe "aufgeklappte Fläche"-Idiom.
            // Padding(2): der Einzug der Einträge gegenüber dem Rahmen.
            Define(ElementKeys.MenuPopup, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.MenuItem, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            // Maus-Hover UND Tastatur-Auswahl — Accent-Hervorhebung wie GridCell/Selected.
            Define(ElementKeys.MenuItem, ElementState.Hovered, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.MenuItem, ElementState.Disabled, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            // BorderColor/Width ist die Linie selbst (DrawSeparatorLine), Padding
            // links/rechts der Einzug, oben/unten die halbe Zeilenhöhe.
            Define(ElementKeys.MenuSeparator, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 3, 8, 3)
            });
        }

        private void DefineRibbon()
        {
            // Fläche wie die Menüleiste: verschmilzt mit dem Fenster darunter.
            Define(ElementKeys.Ribbon, ElementState.Normal, new ElementAppearance
            {
                Background = Surface, BorderColor = Color.Transparent, BorderWidth = 0,
                Corners = CornerRadius.None, ForeColor = TextPrimary, Font = BodyFont,
                Padding = new Padding(6, 2, 6, 4)
            });

            Define(ElementKeys.RibbonTabHeader, ElementState.Normal, new ElementAppearance
            {
                Background = Surface, BorderColor = Color.Transparent, BorderWidth = 0,
                Corners = new CornerRadius(3), ForeColor = TextPrimary, Font = BodyFont,
                Padding = new Padding(12, 4, 12, 4)
            });
            Define(ElementKeys.RibbonTabHeader, ElementState.Hovered, new ElementAppearance
            {
                Background = BorderSubtle, BorderColor = Color.Transparent, BorderWidth = 0,
                Corners = new CornerRadius(3), ForeColor = TextPrimary, Font = BodyFont,
                Padding = new Padding(12, 4, 12, 4)
            });
            // Selected: die erhöhte Blattfläche zieht in den Reiter — wie MenuBarItem/Selected.
            Define(ElementKeys.RibbonTabHeader, ElementState.Selected, new ElementAppearance
            {
                Background = SurfaceRaised, BorderColor = BorderStrong, BorderWidth = 1,
                Corners = new CornerRadius(3), ForeColor = TextPrimary, Font = BodyFont,
                Padding = new Padding(12, 4, 12, 4)
            });
            Define(ElementKeys.RibbonTabHeader, ElementState.Disabled, new ElementAppearance
            {
                Background = Surface, BorderColor = Color.Transparent, BorderWidth = 0,
                Corners = new CornerRadius(3), ForeColor = TextDisabled, Font = BodyFont,
                Padding = new Padding(12, 4, 12, 4)
            });

            // Titeltext bewusst gedämpft (TextDisabled als Ton, nicht als Zustand):
            // der Gruppentitel ist Beschriftung. Gruppen kennen in v1 kein Disabled.
            Define(ElementKeys.RibbonGroup, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised, BorderColor = BorderSubtle, BorderWidth = 1,
                Corners = new CornerRadius(4), ForeColor = TextDisabled, Font = BodyFont,
                Padding = new Padding(4)
            });

            Define(ElementKeys.RibbonButton, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised, BorderColor = Color.Transparent, BorderWidth = 0,
                Corners = new CornerRadius(3), ForeColor = TextPrimary, Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });
            Define(ElementKeys.RibbonButton, ElementState.Hovered, new ElementAppearance
            {
                Background = DisabledFill, BorderColor = BorderStrong, BorderWidth = 1,
                Corners = new CornerRadius(3), ForeColor = TextPrimary, Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });
            Define(ElementKeys.RibbonButton, ElementState.Pressed, new ElementAppearance
            {
                Background = Accent, BorderColor = Accent, BorderWidth = 1,
                Corners = new CornerRadius(3), ForeColor = SurfaceRaised, Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });
            // Selected = Checked: ruhige Fläche mit Accent-Kontur — gedrückt sieht
            // anders aus (volle Accent-Fläche), die zwei Zustände bleiben unterscheidbar.
            Define(ElementKeys.RibbonButton, ElementState.Selected, new ElementAppearance
            {
                Background = DisabledFill, BorderColor = Accent, BorderWidth = 1,
                Corners = new CornerRadius(3), ForeColor = TextPrimary, Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });
            Define(ElementKeys.RibbonButton, ElementState.Disabled, new ElementAppearance
            {
                Background = SurfaceRaised, BorderColor = Color.Transparent, BorderWidth = 0,
                Corners = new CornerRadius(3), ForeColor = TextDisabled, Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });

            Define(ElementKeys.RibbonSeparator, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised, BorderColor = BorderSubtle, BorderWidth = 1,
                Corners = CornerRadius.None, ForeColor = TextPrimary, Font = BodyFont,
                Padding = new Padding(3, 4, 3, 4)
            });
        }

        private void DefineGallery()
        {
            Define(ElementKeys.Gallery, ElementState.Normal, new ElementAppearance
            {
                Background = Color.FromArgb(255, 242, 242, 245),
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.GalleryItem, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6)
            });

            Define(ElementKeys.GalleryItem, ElementState.Hovered, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6)
            });

            Define(ElementKeys.GalleryItem, ElementState.Selected, new ElementAppearance
            {
                Background = Color.FromArgb(255, 219, 234, 250),
                BorderColor = Accent,
                BorderWidth = 2,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6)
            });

            Define(ElementKeys.GalleryItem, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(6)
            });

            Define(ElementKeys.GalleryGroupCaption, ElementState.Normal, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });
        }

        private void DefineExplorer()
        {
            Define(ElementKeys.Tree, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.Tree, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.TreeNode, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = SurfaceRaised,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(4, 3, 4, 3)
            });

            Define(ElementKeys.TreeNode, ElementState.Hovered, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = DisabledFill,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(4, 3, 4, 3)
            });

            // Wie GridCell/Selected: Akzentfläche, weißer Text.
            Define(ElementKeys.TreeNode, ElementState.Selected, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(4, 3, 4, 3)
            });

            Define(ElementKeys.TreeNode, ElementState.Pressed, new ElementAppearance
            {
                Background = BorderSubtle,
                BorderColor = BorderSubtle,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(4, 3, 4, 3)
            });

            Define(ElementKeys.TreeNode, ElementState.Disabled, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = SurfaceRaised,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(4, 3, 4, 3)
            });

            Define(ElementKeys.TreeGlyph, ElementState.Normal, new ElementAppearance
            {
                Background = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = BorderStrong,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.TreeGlyph, ElementState.Hovered, new ElementAppearance
            {
                Background = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.AddressBar, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.AddressBar, ElementState.Hovered, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.AddressBar, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.AddressSegment, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = SurfaceRaised,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 3, 6, 3)
            });

            Define(ElementKeys.AddressSegment, ElementState.Hovered, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = DisabledFill,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 3, 6, 3)
            });

            Define(ElementKeys.AddressSegment, ElementState.Pressed, new ElementAppearance
            {
                Background = BorderSubtle,
                BorderColor = BorderSubtle,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 3, 6, 3)
            });

            Define(ElementKeys.AddressSegment, ElementState.Disabled, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = SurfaceRaised,
                BorderWidth = 0,
                Corners = new CornerRadius(3),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(6, 3, 6, 3)
            });

            Define(ElementKeys.Splitter, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Surface,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(3)
            });

            Define(ElementKeys.Splitter, ElementState.Hovered, new ElementAppearance
            {
                Background = BorderSubtle,
                BorderColor = BorderSubtle,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(3)
            });

            Define(ElementKeys.Splitter, ElementState.Pressed, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(3)
            });

            Define(ElementKeys.Splitter, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = DisabledFill,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(3)
            });

            Define(ElementKeys.DropTarget, ElementState.Normal, new ElementAppearance
            {
                Background = Color.FromArgb(255, 219, 234, 250),
                BorderColor = Accent,
                BorderWidth = 2,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });
        }
    }
}
