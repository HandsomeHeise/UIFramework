namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Ein Skin ist Daten, kein Code: eine Nachschlagetabelle
    /// Element × Zustand → Erscheinungsbild.
    /// Genau das macht den Skin-Editor (Teilprojekt 6) überhaupt möglich.
    /// </summary>
    public interface ISkin
    {
        string Name { get; }

        /// <summary>
        /// Liefert immer ein Erscheinungsbild, nie null und nie eine Exception —
        /// notfalls über die Rückfallkette. Ein lückenhafter Skin ist erlaubt.
        ///
        /// Die zurückgegebene Erscheinung MUSS eingefroren sein
        /// (ElementAppearance.Freeze): Konsumenten bekommen bewusst
        /// Live-Referenzen und dürfen sich darauf verlassen, dass niemand sie
        /// unter ihnen wegändert. SkinBase.Define erledigt das von selbst; wer
        /// ISkin direkt implementiert, friert selbst ein.
        /// </summary>
        ElementAppearance GetAppearance(string elementKey, ElementState state);
    }
}
