using System;
using System.Collections.Generic;
using System.Drawing;

namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Dictionary-gestützte Umsetzung von ISkin samt Rückfallkette:
    ///     elementKey × state  →  elementKey × Normal  →  FallbackAppearance
    /// Daten dürfen unvollständig sein, Code nicht: ein fehlender Eintrag ist
    /// kein Fehler, ein null-Schlüssel schon.
    /// </summary>
    public abstract class SkinBase : ISkin
    {
        /// <summary>
        /// Letzte Rettung, wenn ein Skin ein Element gar nicht kennt.
        /// Bewusst neutral und bewusst sichtbar — nie ein unsichtbares Control.
        /// </summary>
        public static readonly ElementAppearance FallbackAppearance = CreateFallback();

        /// <summary>
        /// Warum eine Fabrikmethode statt eines Objekt-Initialisierers: Diese
        /// Erscheinung läuft nie durch Define und würde sonst als einzige
        /// uneingefroren herausgehen — ausgerechnet die am breitesten geteilte
        /// Instanz im Framework.
        /// </summary>
        private static ElementAppearance CreateFallback()
        {
            var appearance = new ElementAppearance
            {
                Background = Color.FromArgb(255, 128, 128, 128),
                BorderColor = Color.FromArgb(255, 96, 96, 96),
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = Color.FromArgb(255, 0, 0, 0),
                Font = new FontSpec("Segoe UI", 9f),
                Padding = new System.Windows.Forms.Padding(4)
            };

            appearance.Freeze();
            return appearance;
        }

        private readonly Dictionary<string, ElementAppearance> _table =
            new Dictionary<string, ElementAppearance>(StringComparer.Ordinal);

        public abstract string Name { get; }

        /// <summary>
        /// Trägt eine Erscheinung für Element × Zustand in die Tabelle ein.
        /// Übernimmt dabei den Besitz der übergebenen Instanz und friert sie
        /// ein — der Aufrufer darf appearance danach nicht mehr verändern.
        /// </summary>
        protected void Define(string elementKey, ElementState state, ElementAppearance appearance)
        {
            if (elementKey == null) throw new ArgumentNullException(nameof(elementKey));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));

            // Ab hier gehört sie dem Skin. GetAppearance reicht die Einträge
            // direkt heraus — uneingefroren könnte ein Konsument damit den
            // Skin app-weit umfärben. Freeze ist idempotent: eine Erscheinung
            // darf für mehrere Element/Zustand-Paare gelten (siehe StubSkin).
            appearance.Freeze();

            _table[BuildKey(elementKey, state)] = appearance;
        }

        public ElementAppearance GetAppearance(string elementKey, ElementState state)
        {
            if (elementKey == null) throw new ArgumentNullException(nameof(elementKey));

            ElementAppearance appearance;

            if (_table.TryGetValue(BuildKey(elementKey, state), out appearance))
                return appearance;

            if (state != ElementState.Normal &&
                _table.TryGetValue(BuildKey(elementKey, ElementState.Normal), out appearance))
                return appearance;

            return FallbackAppearance;
        }

        private static string BuildKey(string elementKey, ElementState state)
        {
            return elementKey + "/" + state;
        }
    }
}
