using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A red-black tree: a binary search tree that stays balanced by colouring
    /// every node red or black and keeping four rules true after every insert:
    ///
    ///   1. Every node is red or black.
    ///   2. The root is black.
    ///   3. A red node never has a red child.
    ///   4. Every path from a node down to an empty child passes through the same
    ///      number of black nodes (the node's "black height").
    ///
    /// Rules 3 and 4 together mean the longest path from the root can be at most
    /// twice the shortest, so the height is at most 2 * log2(n + 1) and search and
    /// insert are O(log n).
    ///
    /// A new node is always inserted red. If its parent is also red, rule 3 is
    /// broken and is repaired by walking up the tree:
    ///
    ///   Uncle red    recolour parent and uncle black, grandparent red, and
    ///                continue from the grandparent
    ///   Uncle black  one or two rotations around the grandparent, then recolour;
    ///                this always finishes the repair
    ///
    /// Compared with an AVL tree, a red-black tree is slightly less strictly
    /// balanced but needs at most two rotations per insert. .NET uses red-black
    /// trees inside SortedSet and SortedDictionary.
    ///
    /// The application uses this tree to index service requests by the date and
    /// time they were submitted, for the date-sorted list and date-range filter.
    /// </summary>
    public class RedBlackTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    {
        public RedBlackTree() : this(null)
        {
        }

        public RedBlackTree(IComparer<TKey>? comparer) : base(comparer)
        {
        }

        public int RotationCount { get; private set; }

        public int RecolourCount { get; private set; }

        /// <summary>
        /// Number of black nodes on any path from the root down to an empty child.
        /// </summary>
        public int BlackHeight
        {
            get
            {
                int blackNodes = 0;
                BinaryTreeNode<TKey, TValue>? current = Root;

                while (current != null)
                {
                    if (current.Colour == NodeColour.Black) blackNodes++;
                    current = current.Left;
                }

                return blackNodes;
            }
        }

        public override bool Insert(TKey key, TValue value)
        {
            // 1. Ordinary BST insert, remembering the parent.
            BinaryTreeNode<TKey, TValue>? parent = null;
            BinaryTreeNode<TKey, TValue>? current = Root;
            int comparison = 0;

            while (current != null)
            {
                parent = current;
                comparison = KeyComparer.Compare(key, current.Key);

                if (comparison == 0)
                {
                    return false;
                }

                current = comparison < 0 ? current.Left : current.Right;
            }

            BinaryTreeNode<TKey, TValue> node = new BinaryTreeNode<TKey, TValue>(key, value)
            {
                Colour = NodeColour.Red,
                Parent = parent
            };

            if (parent == null)
            {
                Root = node;
            }
            else if (comparison < 0)
            {
                parent.Left = node;
            }
            else
            {
                parent.Right = node;
            }

            Count++;

            // 2. Repair any red-red violation the new red node created.
            FixAfterInsert(node);
            return true;
        }

        private void FixAfterInsert(BinaryTreeNode<TKey, TValue> node)
        {
            while (node.Parent != null && node.Parent.Colour == NodeColour.Red)
            {
                BinaryTreeNode<TKey, TValue> parent = node.Parent;

                // The parent is red, so it cannot be the (black) root: a grandparent exists.
                BinaryTreeNode<TKey, TValue> grandparent = parent.Parent!;

                if (parent == grandparent.Left)
                {
                    BinaryTreeNode<TKey, TValue>? uncle = grandparent.Right;

                    if (uncle != null && uncle.Colour == NodeColour.Red)
                    {
                        // Uncle red: push the blackness down one level and move up.
                        parent.Colour = NodeColour.Black;
                        uncle.Colour = NodeColour.Black;
                        grandparent.Colour = NodeColour.Red;
                        RecolourCount++;
                        node = grandparent;
                    }
                    else
                    {
                        if (node == parent.Right)
                        {
                            // Zig-zag: rotate the parent so the node lines up on the left.
                            node = parent;
                            RotateLeft(node);
                            parent = node.Parent!;
                        }

                        // Zig-zig: rotate the grandparent right and swap colours.
                        parent.Colour = NodeColour.Black;
                        grandparent.Colour = NodeColour.Red;
                        RotateRight(grandparent);
                    }
                }
                else
                {
                    // Mirror image of the case above.
                    BinaryTreeNode<TKey, TValue>? uncle = grandparent.Left;

                    if (uncle != null && uncle.Colour == NodeColour.Red)
                    {
                        parent.Colour = NodeColour.Black;
                        uncle.Colour = NodeColour.Black;
                        grandparent.Colour = NodeColour.Red;
                        RecolourCount++;
                        node = grandparent;
                    }
                    else
                    {
                        if (node == parent.Left)
                        {
                            node = parent;
                            RotateRight(node);
                            parent = node.Parent!;
                        }

                        parent.Colour = NodeColour.Black;
                        grandparent.Colour = NodeColour.Red;
                        RotateLeft(grandparent);
                    }
                }
            }

            // Rule 2: the root is always black.
            Root!.Colour = NodeColour.Black;
        }

        private void RotateLeft(BinaryTreeNode<TKey, TValue> x)
        {
            BinaryTreeNode<TKey, TValue> y = x.Right!;

            x.Right = y.Left;
            if (y.Left != null)
            {
                y.Left.Parent = x;
            }

            y.Parent = x.Parent;
            if (x.Parent == null)
            {
                Root = y;
            }
            else if (x == x.Parent.Left)
            {
                x.Parent.Left = y;
            }
            else
            {
                x.Parent.Right = y;
            }

            y.Left = x;
            x.Parent = y;
            RotationCount++;
        }

        private void RotateRight(BinaryTreeNode<TKey, TValue> x)
        {
            BinaryTreeNode<TKey, TValue> y = x.Left!;

            x.Left = y.Right;
            if (y.Right != null)
            {
                y.Right.Parent = x;
            }

            y.Parent = x.Parent;
            if (x.Parent == null)
            {
                Root = y;
            }
            else if (x == x.Parent.Right)
            {
                x.Parent.Right = y;
            }
            else
            {
                x.Parent.Left = y;
            }

            y.Right = x;
            x.Parent = y;
            RotationCount++;
        }

        /// <summary>
        /// The search-tree rule, plus all four red-black rules and correct parent links.
        /// </summary>
        public override bool Validate(out string message)
        {
            if (!base.Validate(out message))
            {
                return false;
            }

            if (Root == null)
            {
                message = "Empty tree.";
                return true;
            }

            if (Root.Colour != NodeColour.Black)
            {
                message = "The root is not black.";
                return false;
            }

            if (Root.Parent != null)
            {
                message = "The root has a parent link.";
                return false;
            }

            int expectedBlackHeight = -1;
            string? problem = CheckSubtree(Root, 0, ref expectedBlackHeight);

            if (problem != null)
            {
                message = problem;
                return false;
            }

            message = string.Format(
                "{0} keys in order; root black; no red node has a red child; " +
                "every path has {1} black nodes; height {2}.",
                Count, BlackHeight, Height);
            return true;
        }

        private static string? CheckSubtree(
            BinaryTreeNode<TKey, TValue>? node, int blackSoFar, ref int expectedBlackHeight)
        {
            if (node == null)
            {
                // Reached an empty child: every such path must have the same black count.
                if (expectedBlackHeight < 0)
                {
                    expectedBlackHeight = blackSoFar;
                }
                else if (blackSoFar != expectedBlackHeight)
                {
                    return "Two paths have a different number of black nodes.";
                }
                return null;
            }

            if (node.Colour == NodeColour.Red)
            {
                if ((node.Left != null && node.Left.Colour == NodeColour.Red) ||
                    (node.Right != null && node.Right.Colour == NodeColour.Red))
                {
                    return string.Format("Red node \"{0}\" has a red child.", node.Key);
                }
            }

            if ((node.Left != null && node.Left.Parent != node) ||
                (node.Right != null && node.Right.Parent != node))
            {
                return string.Format("A parent link is wrong below \"{0}\".", node.Key);
            }

            int blackCount = blackSoFar + (node.Colour == NodeColour.Black ? 1 : 0);

            return CheckSubtree(node.Left, blackCount, ref expectedBlackHeight)
                   ?? CheckSubtree(node.Right, blackCount, ref expectedBlackHeight);
        }
    }
}
