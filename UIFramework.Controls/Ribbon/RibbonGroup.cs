using System.Collections.Generic;

namespace UIFramework.Controls
{
    /// <summary>
    /// Eine Gruppe von Ribbon-Elementen innerhalb eines Ribbon-Tabs.
    /// </summary>
    public sealed class RibbonGroup
    {
        private readonly List<RibbonItem> _items = new List<RibbonItem>();

        public RibbonGroup()
        {
            Title = string.Empty;
        }

        public RibbonGroup(string title) : this()
        {
            Title = title ?? string.Empty;
        }

        /// <summary>
        /// Titel der Gruppe.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Die Elemente in dieser Gruppe — wird nie null.
        /// </summary>
        public IList<RibbonItem> Items { get { return _items; } }
    }
}
