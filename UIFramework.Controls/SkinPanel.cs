using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Container. Prüfstand für das Zusammenspiel mit dem WinForms-Layout.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design")]
    public class SkinPanel : SkinnedControl
    {
        public SkinPanel()
        {
            SetStyle(ControlStyles.ContainerControl, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Size = new Size(200, 150);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Panel; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        /// <summary>
        /// WinForms platziert angedockte Kind-Controls hierin. Ohne dieses
        /// Überschreiben lägen Kinder unter dem Rahmen und im Innenabstand —
        /// der Skin wäre wirkungslos.
        /// </summary>
        public override Rectangle DisplayRectangle
        {
            get
            {
                // Der Einzug (Padding + Rahmenbreite, DPI-skaliert) wird im Painter
                // berechnet, nicht hier: Controls dürfen selbst nicht skalieren.
                return SkinPainter.GetContentRectangle(ClientRectangle, CurrentAppearance, DeviceDpi);
            }
        }
    }
}
