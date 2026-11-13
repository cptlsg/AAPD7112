using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// An AVL tree (Adelson-Velsky and Landis, 1962): a binary search tree that
    /// rebalances itself after every insert so that it can never become a chain.
    ///
    /// Each node stores the height of its subtree. Its balance factor is
    ///
    ///     height(left subtree) - height(right subtree)
    ///
    /// and must stay -1, 0 or +1. When an insert pushes a node to +2 or -2, one or
    /// two rotations restore the balance:
    ///
    ///   Left-Left    (+2, left child +1 or 0)   rotate right
    ///   Right-Right  (-2, right child -1 or 0)  rotate left
    ///   Left-Right   (+2, left child -1)        rotate the child left, then the node right
    ///   Right-Left   (-2, right child +1)       rotate the child right, then the node left
    ///
    /// A rotation only relinks three pointers, so it costs O(1). Because every
    /// node is balanced, the height is at most about 1.44 * log2(n), and search
    /// and insert are guaranteed O(log n) even when keys arrive in sorted order.
    ///
    /// The application uses this tree as the primary index of service requests by
    /// reference number, because reference numbers are issued in ascending order.
    /// </summary>
    public class AvlTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    {
        public AvlTree() : this(null)
        {
        }

        public AvlTree(IComparer<TKey>? comparer) : base(comparer)
        {
        }

        /// <summary>
        /// Total single rotations performed since the tree was created (a double
        /// rotation counts as two).
        /// </summary>
        public int RotationCount { get; private set; }

        public override bool Insert(TKey key, TValue value)
        {
            bool inserted = false;
            Root = InsertInto(Root, key, value, ref inserted);

            if (inserted)
            {
                Count++;
            }

            return inserted;
        }

        /// <summary>
        /// Inserts into the subtree rooted at node and returns the subtree's new
        /// root, which changes when a rotation happens. On the way back up from the
        /// new leaf, every ancestor updates its height and rebalances if needed.
        /// </summary>
        private BinaryTreeNode<TKey, TValue> InsertInto(
            BinaryTreeNode<TKey, TValue>? node, TKey key, TValue value, ref bool inserted)
        {
            if (node == null)
            {
                inserted = true;
                return new BinaryTreeNode<TKey, TValue>(key, value);
            }

            int comparison = KeyComparer.Compare(key, node.Key);

            if (comparison < 0)
            {
                node.Left = InsertInto(node.Left, key, value, ref inserted);
            }
            else if (comparison > 0)
            {
                node.Right = InsertInto(node.Right, key, value, ref inserted);
            }
            else
            {
                // Duplicate key: nothing changes below, so no rebalancing is needed.
                return node;
            }

            UpdateHeight(node);
            return Rebalance(node);
        }

        private BinaryTreeNode<TKey, TValue> Rebalance(BinaryTreeNode<TKey, TValue> node)
        {
            int balance = BalanceFactor(node);

            if (balance > 1)
            {
                // Left-heavy. If the left child leans right, this is the Left-Right
                // case: straighten it into Left-Left first.
                if (BalanceFactor(node.Left!) < 0)
                {
                    node.Left = RotateLeft(node.Left!);
                }
                return RotateRight(node);
            }

            if (balance < -1)
            {
                // Right-heavy. If the right child leans left (Right-Left case),
                // straighten it into Right-Right first.
                if (BalanceFactor(node.Right!) > 0)
                {
                    node.Right = RotateRight(node.Right!);
                }
                return RotateLeft(node);
            }

            return node;
        }

        /// <summary>
        ///         y                x
        ///        / \              / \
        ///       x   C    -->     A   y
        ///      / \                  / \
        ///     A   B                B   C
        /// </summary>
        private BinaryTreeNode<TKey, TValue> RotateRight(BinaryTreeNode<TKey, TValue> y)
        {
            BinaryTreeNode<TKey, TValue> x = y.Left!;
            BinaryTreeNode<TKey, TValue>? b = x.Right;

            x.Right = y;
            y.Left = b;

            // y is now below x, so its height must be recalculated first.
            UpdateHeight(y);
            UpdateHeight(x);

            RotationCount++;
            return x;
        }

        /// <summary>
        ///       x                    y
        ///      / \                  / \
        ///     A   y      -->       x   C
        ///        / \              / \
        ///       B   C            A   B
        /// </summary>
        private BinaryTreeNode<TKey, TValue> RotateLeft(BinaryTreeNode<TKey, TValue> x)
        {
            BinaryTreeNode<TKey, TValue> y = x.Right!;
            BinaryTreeNode<TKey, TValue>? b = y.Left;

            y.Left = x;
            x.Right = b;

            UpdateHeight(x);
            UpdateHeight(y);

            RotationCount++;
            return y;
        }

        private static int HeightOf(BinaryTreeNode<TKey, TValue>? node)
        {
            return node == null ? 0 : node.Height;
        }

        private static int BalanceFactor(BinaryTreeNode<TKey, TValue> node)
        {
            return HeightOf(node.Left) - HeightOf(node.Right);
        }

        private static void UpdateHeight(BinaryTreeNode<TKey, TValue> node)
        {
            node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));
        }

        /// <summary>
        /// The search-tree rule, plus: every stored height is correct and every
        /// balance factor is between -1 and +1.
        /// </summary>
        public override bool Validate(out string message)
        {
            if (!base.Validate(out message))
            {
                return false;
            }

            foreach (BinaryTreeNode<TKey, TValue> node in LevelOrderNodes())
            {
                int expectedHeight = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));
                if (node.Height != expectedHeight)
                {
                    message = string.Format("Stored height is wrong at \"{0}\".", node.Key);
                    return false;
                }

                int balance = BalanceFactor(node);
                if (balance < -1 || balance > 1)
                {
                    message = string.Format("Balance factor {0} at \"{1}\".", balance, node.Key);
                    return false;
                }
            }

            message = string.Format(
                "{0} keys in order; every balance factor is -1, 0 or +1; height {1}.",
                Count, Height);
            return true;
        }
    }
}
