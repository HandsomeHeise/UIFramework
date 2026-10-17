namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Der effektive Zustand eines Elements — bewusst ein einzelner Wert, keine Flags.
    /// Flags würden in der Skin-Tabelle eine Zeile pro Kombination erzwingen.
    /// Die Rangfolge (Disabled > Pressed > Hovered > Selected > Normal)
    /// errechnet SkinnedControl.
    /// Der Fokus ist NICHT Teil dieser Rangfolge — er liegt über allem und
    /// ist ein eigenes Element (ElementKeys.Focus).
    /// </summary>
    public enum ElementState
    {
        Normal = 0,
        Selected = 1,
        Hovered = 2,
        Pressed = 3,
        Disabled = 4
    }
}
