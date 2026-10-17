using System;
using System.Drawing;

namespace UIFramework.Tree
{
    /// <summary>
    /// Ein Knoten des Baums. Hält nur Daten und meldet Änderungen an den Baum,
    /// in dem er hängt — gezeichnet und gerechnet wird im TreeControl.
    ///
    /// Laden beim Aufklappen: HasChildrenHint sagt "hat vielleicht Kinder"
    /// (Glyphe sichtbar, ohne dass die Kinder schon da sind). Das erste
    /// Aufklappen feuert TreeControl.NodeExpanding; die App ruft BeginLoading,
    /// lädt asynchron und schließt mit Children.ReplaceWith (oder
    /// EndLoadingWithError). Alle Aufrufe gehören auf den UI-Thread.
    ///
    /// Das Bild gehört der App: es wird weder kopiert noch disposed.
    /// </summary>
    public sealed class TreeNode
    {
        private string _text;
        private Image _image;
        private bool _hasChildrenHint;
        private bool _expanded;
        private bool _loading;
        private string _loadError;

        public TreeNode()
        {
            Children = new TreeNodeCollection(this);
        }

        public TreeNode(string text)
            : this()
        {
            _text = text;
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (string.Equals(_text, value, StringComparison.Ordinal)) return;
                _text = value;
                NotifyVisual();
            }
        }

        public Image Image
        {
            get { return _image; }
            set
            {
                if (ReferenceEquals(_image, value)) return;
                _image = value;
                NotifyVisual();
            }
        }

        public object Tag { get; set; }

        /// <summary>"Hat vielleicht Kinder" — die Glyphe erscheint, ohne dass
        /// die Kinder geladen sind.</summary>
        public bool HasChildrenHint
        {
            get { return _hasChildrenHint; }
            set
            {
                if (_hasChildrenHint == value) return;
                _hasChildrenHint = value;
                NotifyStructure();
            }
        }

        public TreeNodeCollection Children { get; }

        /// <summary>Der Elternknoten; null bei Wurzeln und losen Knoten.</summary>
        public TreeNode Parent { get; internal set; }

        /// <summary>Gesetzt nur an Wurzeln, die direkt in TreeControl.Nodes hängen.</summary>
        internal TreeControl RootTree { get; set; }

        /// <summary>Der Baum, in dem der Knoten hängt — null, solange keiner.</summary>
        public TreeControl Tree
        {
            get { return Parent != null ? Parent.Tree : RootTree; }
        }

        public int Level
        {
            get
            {
                int level = 0;
                for (var p = Parent; p != null; p = p.Parent) level++;
                return level;
            }
        }

        public bool IsExpanded
        {
            get { return _expanded; }
            set
            {
                if (value) Expand();
                else Collapse();
            }
        }

        /// <summary>Kinder vorhanden oder angekündigt — dann zeigt der Baum die Glyphe.</summary>
        public bool IsExpandable
        {
            get { return Children.Count > 0 || _hasChildrenHint; }
        }

        public bool IsLoading
        {
            get { return _loading; }
        }

        /// <summary>Fehlertext des letzten Ladeversuchs; null ohne Fehler.</summary>
        public string LoadError
        {
            get { return _loadError; }
        }

        public void Expand()
        {
            if (_expanded || !IsExpandable) return;

            _expanded = true;

            // Ein Fehler von vorhin gilt nicht mehr: jedes neue Aufklappen ohne
            // Kinder ist ein neuer Ladeversuch.
            bool needsLoad = Children.Count == 0 && !_loading;
            if (needsLoad) _loadError = null;

            NotifyStructure();

            var tree = Tree;
            if (needsLoad && tree != null) tree.RaiseNodeExpanding(this);
        }

        public void Collapse()
        {
            if (!_expanded) return;

            _expanded = false;

            var tree = Tree;
            if (tree != null) tree.OnNodeCollapsed(this);

            NotifyStructure();
        }

        public void Toggle()
        {
            if (_expanded) Collapse();
            else Expand();
        }

        /// <summary>Zeigt unter dem (aufgeklappten) Knoten "Wird geladen…".</summary>
        public void BeginLoading()
        {
            if (_loading) return;

            _loading = true;
            _loadError = null;
            NotifyStructure();
        }

        /// <summary>Beendet das Laden mit einer Fehlerzeile statt Kindern.</summary>
        public void EndLoadingWithError(string text)
        {
            _loading = false;
            _loadError = string.IsNullOrEmpty(text) ? "Fehler" : text;
            NotifyStructure();
        }

        /// <summary>Von TreeNodeCollection.ReplaceWith gerufen (die meldet die
        /// Struktur selbst). Null Kinder: Hinweis und Glyphe verschwinden, der
        /// Knoten klappt zu — es gibt nichts aufzuklappen.</summary>
        internal void EndLoading()
        {
            _loading = false;
            _loadError = null;

            if (Children.Count == 0)
            {
                _hasChildrenHint = false;
                _expanded = false;
            }
        }

        internal bool IsDescendantOf(TreeNode ancestor)
        {
            for (var p = Parent; p != null; p = p.Parent)
                if (ReferenceEquals(p, ancestor)) return true;
            return false;
        }

        private void NotifyVisual()
        {
            var tree = Tree;
            if (tree != null) tree.OnNodeVisualChanged(this);
        }

        internal void NotifyStructure()
        {
            var tree = Tree;
            if (tree != null) tree.OnNodeStructureChanged();
        }
    }
}
