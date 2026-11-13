using System;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// Colour of a node in a red-black tree. Other trees ignore it.
    /// </summary>
    public enum NodeColour
    {
        Red,
        Black
    }

    /// <summary>
    /// A node in a binary tree: a key, the value stored against it, and links to at
    /// most two children. One node class is shared by every binary tree in the
    /// project; each tree uses only the extra fields it needs:
    ///
    ///   Parent  - red-black tree (rotations and recolouring walk back up the tree)
    ///   Height  - AVL tree (balance factor = height of left minus height of right)
    ///   Colour  - red-black tree
    /// </summary>
    public class BinaryTreeNode<TKey, TValue>
    {
        public BinaryTreeNode(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            Height = 1;
            Colour = NodeColour.Red;
        }

        public TKey Key { get; }

        public TValue Value { get; set; }

        public BinaryTreeNode<TKey, TValue>? Left { get; set; }

        public BinaryTreeNode<TKey, TValue>? Right { get; set; }

        public BinaryTreeNode<TKey, TValue>? Parent { get; set; }

        /// <summary>
        /// Height of the subtree rooted here, counted in nodes (a leaf has height 1).
        /// Kept up to date by the AVL tree only.
        /// </summary>
        public int Height { get; set; }

        public NodeColour Colour { get; set; }

        public bool IsLeaf
        {
            get { return Left == null && Right == null; }
        }

        public override string ToString()
        {
            return Convert.ToString(Key) ?? string.Empty;
        }
    }
}
