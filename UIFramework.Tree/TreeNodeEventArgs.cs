using System;

namespace UIFramework.Tree
{
    public sealed class TreeNodeEventArgs : EventArgs
    {
        public TreeNodeEventArgs(TreeNode node)
        {
            Node = node;
        }

        public TreeNode Node { get; }
    }
}
