using System.Collections.Generic;

namespace UIFramework.Controls
{
    /// <summary>
    /// Ein Tab im Ribbon — enthält Gruppen von Ribbon-Elementen.
    /// </summary>
    public sealed class RibbonTab
    {
        private readonly List<RibbonGroup> _groups = new List<RibbonGroup>();

        public RibbonTab()
        {
            Text = string.Empty;
            Enabled = true;
        }

        public RibbonTab(string text) : this()
        {
            Text = text ?? string.Empty;
        }

        /// <summary>
        /// Text/Name des Tabs.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gibt an, ob der Tab aktiviert ist. Standard ist true.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Die Gruppen in diesem Tab — wird nie null.
        /// </summary>
        public IList<RibbonGroup> Groups { get { return _groups; } }
    }
}
