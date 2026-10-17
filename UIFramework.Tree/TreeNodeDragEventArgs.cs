using System;
using System.Windows.Forms;

namespace UIFramework.Tree
{
    /// <summary>Ein Zug beginnt: der gezogene Knoten und die gedrückte Taste.</summary>
    public sealed class TreeNodeDragEventArgs : EventArgs
    {
        public TreeNodeDragEventArgs(TreeNode node, MouseButtons button)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            Node = node;
            Button = button;
        }

        public TreeNode Node { get; }
        public MouseButtons Button { get; }
    }
}
