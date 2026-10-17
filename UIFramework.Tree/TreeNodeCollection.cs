using System;
using System.Collections;
using System.Collections.Generic;

namespace UIFramework.Tree
{
    /// <summary>
    /// Die Kinder eines Knotens oder die Wurzeln eines Baums. Jede Änderung
    /// meldet sich beim Baum (falls vorhanden) als Strukturänderung — genau
    /// eine Meldung je Aufruf, auch bei AddRange/ReplaceWith — und feuert
    /// danach Changed, genau einmal je Aufruf.
    /// </summary>
    public sealed class TreeNodeCollection : IEnumerable<TreeNode>
    {
        private readonly List<TreeNode> _items = new List<TreeNode>();
        private readonly TreeNode _ownerNode;
        private readonly TreeControl _ownerTree;

        internal TreeNodeCollection(TreeNode ownerNode)
        {
            _ownerNode = ownerNode;
        }

        internal TreeNodeCollection(TreeControl ownerTree)
        {
            _ownerTree = ownerTree;
        }

        /// <summary>Feuert nach jeder Änderung dieser Sammlung — genau einmal
        /// je Aufruf, nach der Meldung an den Baum.</summary>
        public event EventHandler Changed;

        public int Count
        {
            get { return _items.Count; }
        }

        public TreeNode this[int index]
        {
            get { return _items[index]; }
        }

        public void Add(TreeNode node)
        {
            Attach(node);
            _items.Add(node);
            OnChanged();
        }

        public void Insert(int index, TreeNode node)
        {
            Attach(node);
            _items.Insert(index, node);
            OnChanged();
        }

        public void AddRange(IEnumerable<TreeNode> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            foreach (var node in new List<TreeNode>(nodes))
            {
                Attach(node);
                _items.Add(node);
            }
            OnChanged();
        }

        public bool Remove(TreeNode node)
        {
            if (node == null || !_items.Remove(node)) return false;

            Detach(node);
            OnChanged();
            return true;
        }

        public void Clear()
        {
            if (_items.Count == 0) return;

            foreach (var node in _items) Detach(node);
            _items.Clear();
            OnChanged();
        }

        public int IndexOf(TreeNode node)
        {
            return _items.IndexOf(node);
        }

        /// <summary>
        /// Ersetzt alle Kinder in einem Zug und beendet das Laden des Besitzers
        /// (TreeNode.BeginLoading). Die Quelle wird ZUERST vollständig gelesen:
        /// eine träge Aufzählung könnte sonst noch auf die alten Kinder zeigen,
        /// während sie schon abgehängt werden. Bisherige Kinder dürfen darin
        /// wieder vorkommen.
        /// </summary>
        public void ReplaceWith(IEnumerable<TreeNode> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            var incoming = new List<TreeNode>(nodes);

            foreach (var node in _items) Detach(node);
            _items.Clear();

            foreach (var node in incoming)
            {
                Attach(node);
                _items.Add(node);
            }

            if (_ownerNode != null) _ownerNode.EndLoading();
            OnChanged();
        }

        public IEnumerator<TreeNode> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void Attach(TreeNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node.Parent != null || node.RootTree != null)
                throw new InvalidOperationException(
                    "Der Knoten hängt bereits in einem Baum — erst dort entfernen, dann hier einfügen.");
            if (_ownerNode != null && (ReferenceEquals(node, _ownerNode) || _ownerNode.IsDescendantOf(node)))
                throw new InvalidOperationException("Ein Knoten kann nicht unter sich selbst hängen.");

            node.Parent = _ownerNode;
            node.RootTree = _ownerTree;
        }

        private static void Detach(TreeNode node)
        {
            node.Parent = null;
            node.RootTree = null;
        }

        private void OnChanged()
        {
            var tree = _ownerTree ?? (_ownerNode == null ? null : _ownerNode.Tree);
            if (tree != null) tree.OnNodeStructureChanged();

            var handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
