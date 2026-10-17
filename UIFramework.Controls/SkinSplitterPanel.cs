using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Ein Bereich eines <see cref="SkinSplitContainer"/>: ein SkinPanel ohne
    /// eigenen Rahmen und ohne Einzug. Ein normales SkinPanel legte um den Baum
    /// und die Liste je einen zweiten Rahmen mit 8 px Abstand — das Explorer-Bild
    /// verlangt Inhalt bündig am Splitter. Gemalt wird hier nichts: die
    /// Hintergrundfarbe ist transparent (SkinnedControl), also scheint die vom
    /// Container gemalte Fläche durch. Nur der Container erzeugt Bereiche.
    /// </summary>
    [ToolboxItem(false)]
    public sealed class SkinSplitterPanel : SkinPanel
    {
        internal SkinSplitterPanel()
        {
        }

        public override Rectangle DisplayRectangle
        {
            get { return ClientRectangle; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Bewusst leer — siehe Klassenkommentar.
        }
    }
}
