using System.Collections.Generic;

namespace UIFramework.Tree
{
    internal enum TreeRowKind
    {
        Node,
        Loading,
        Error
    }

    /// <summary>Eine sichtbare Zeile. Bei Loading/Error ist Node der
    /// Elternknoten, unter dem der Platzhalter steht.</summary>
    internal struct TreeRow
    {
        public TreeRow(TreeNode node, int level, TreeRowKind kind)
        {
            Node = node;
            Level = level;
            Kind = kind;
        }

        public TreeNode Node { get; }
        public int Level { get; }
        public TreeRowKind Kind { get; }
    }

    /// <summary>
    /// Rollt den Baum in die flache Liste der SICHTBAREN Zeilen aus: Wurzeln
    /// und die Kinder aufgeklappter Knoten. Zugeklappte Teilbäume betritt es
    /// nie — die Kosten folgen dem, was man sehen kann, nicht der Baumgröße.
    /// Die Rekursionstiefe ist die Baumtiefe (Dateisysteme: klein).
    /// </summary>
    internal static class TreeFlattener
    {
        public static List<TreeRow> Flatten(IEnumerable<TreeNode> roots)
        {
            var rows = new List<TreeRow>();
            foreach (var node in roots) Add(rows, node, 0);
            return rows;
        }

        private static void Add(List<TreeRow> rows, TreeNode node, int level)
        {
            rows.Add(new TreeRow(node, level, TreeRowKind.Node));
            if (!node.IsExpanded) return;

            if (node.IsLoading)
            {
                rows.Add(new TreeRow(node, level + 1, TreeRowKind.Loading));
                return;
            }

            if (node.LoadError != null)
            {
                rows.Add(new TreeRow(node, level + 1, TreeRowKind.Error));
                return;
            }

            foreach (var child in node.Children) Add(rows, child, level + 1);
        }
    }
}
