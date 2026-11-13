using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A binary search tree (BST): a binary tree where every key in a node's left
    /// subtree is smaller than the node's key, and every key in its right subtree is
    /// larger. A search starts at the root and goes left or right at each node,
    /// discarding the other side, so it visits one node per level.
    ///
    ///   Search / insert   O(height): O(log n) when balanced, O(n) in the worst case
    ///   Range query       O(height + number of results)
    ///   Sorted listing    O(n) with an in-order traversal
    ///
    /// The worst case happens when keys arrive already sorted: each new key is
    /// larger than all the others, so it always goes right and the tree becomes a
    /// chain. Reference numbers are issued in sequence (MSA-2026-0001, 0002, ...),
    /// which is exactly that case, so the reference index uses the self-balancing
    /// AVL tree instead. This plain BST indexes requests by suburb name, which
    /// arrive in no particular order.
    ///
    /// Keys are unique. Insert returns false for a key that is already present
    /// and leaves the existing value alone.
    /// </summary>
    public class BinarySearchTree<TKey, TValue> : BinaryTree<TKey, TValue>
    {
        protected readonly IComparer<TKey> KeyComparer;

        public BinarySearchTree() : this(null)
        {
        }

        public BinarySearchTree(IComparer<TKey>? comparer)
        {
            KeyComparer = comparer ?? Comparer<TKey>.Default;
        }

        /// <summary>
        /// How many keys the most recent search or range query compared against.
        /// Shown on the "Under the hood" tab to compare the trees' efficiency.
        /// </summary>
        public int LastSearchComparisons { get; protected set; }

        // ------------------------------------------------------------------
        // Insert
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a key and value. Returns false (and changes nothing) if the key is
        /// already in the tree.
        /// </summary>
        public virtual bool Insert(TKey key, TValue value)
        {
            BinaryTreeNode<TKey, TValue> newNode = new BinaryTreeNode<TKey, TValue>(key, value);

            if (Root == null)
            {
                Root = newNode;
                Count++;
                return true;
            }

            BinaryTreeNode<TKey, TValue> current = Root;

            while (true)
            {
                int comparison = KeyComparer.Compare(key, current.Key);

                if (comparison == 0)
                {
                    return false;
                }

                if (comparison < 0)
                {
                    if (current.Left == null)
                    {
                        current.Left = newNode;
                        break;
                    }
                    current = current.Left;
                }
                else
                {
                    if (current.Right == null)
                    {
                        current.Right = newNode;
                        break;
                    }
                    current = current.Right;
                }
            }

            newNode.Parent = current;
            Count++;
            return true;
        }

        // ------------------------------------------------------------------
        // Search
        // ------------------------------------------------------------------

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            BinaryTreeNode<TKey, TValue>? node = FindNode(key);

            if (node == null)
            {
                value = default;
                return false;
            }

            value = node.Value;
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return FindNode(key) != null;
        }

        /// <summary>
        /// Walks down from the root, going left for smaller keys and right for
        /// larger ones, and records how many nodes it compared against.
        /// </summary>
        protected BinaryTreeNode<TKey, TValue>? FindNode(TKey key)
        {
            int comparisons = 0;
            BinaryTreeNode<TKey, TValue>? current = Root;

            while (current != null)
            {
                comparisons++;
                int comparison = KeyComparer.Compare(key, current.Key);

                if (comparison == 0)
                {
                    LastSearchComparisons = comparisons;
                    return current;
                }

                current = comparison < 0 ? current.Left : current.Right;
            }

            LastSearchComparisons = comparisons;
            return null;
        }

        /// <summary>
        /// Every entry with a key between low and high (both inclusive), in key order.
        /// Subtrees that cannot contain a key in the range are skipped entirely:
        /// if low is greater than a node's key, nothing on its left can qualify.
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> Range(TKey low, TKey high)
        {
            List<KeyValuePair<TKey, TValue>> result = new List<KeyValuePair<TKey, TValue>>();
            int visited = 0;

            Stack<BinaryTreeNode<TKey, TValue>> stack = new Stack<BinaryTreeNode<TKey, TValue>>();
            BinaryTreeNode<TKey, TValue>? current = Root;

            // An in-order walk that only goes left when keys there could be >= low,
            // and stops completely once it passes high.
            while (current != null || stack.Count > 0)
            {
                while (current != null)
                {
                    visited++;
                    if (KeyComparer.Compare(low, current.Key) <= 0)
                    {
                        // This node is >= low, so its left subtree may hold keys in range.
                        stack.Push(current);
                        current = current.Left;
                    }
                    else
                    {
                        // This node and everything on its left are below the range.
                        current = current.Right;
                    }
                }

                if (stack.Count == 0) break;

                BinaryTreeNode<TKey, TValue> node = stack.Pop();

                if (KeyComparer.Compare(node.Key, high) > 0)
                {
                    // In-order means every later key is larger still: finished.
                    break;
                }

                result.Add(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
                current = node.Right;
            }

            LastSearchComparisons = visited;
            return result;
        }

        // ------------------------------------------------------------------
        // Validation (used by the self-checks on the "Under the hood" tab)
        // ------------------------------------------------------------------

        /// <summary>
        /// Confirms the search-tree rule holds: an in-order traversal must give
        /// strictly increasing keys, and the node count must match Count.
        /// </summary>
        public virtual bool Validate(out string message)
        {
            List<KeyValuePair<TKey, TValue>> ordered = InOrder();

            if (ordered.Count != Count)
            {
                message = string.Format("Expected {0} nodes but found {1}.", Count, ordered.Count);
                return false;
            }

            for (int i = 1; i < ordered.Count; i++)
            {
                if (KeyComparer.Compare(ordered[i - 1].Key, ordered[i].Key) >= 0)
                {
                    message = string.Format("Keys out of order at \"{0}\".", ordered[i].Key);
                    return false;
                }
            }

            message = string.Format("{0} keys in strictly ascending order.", Count);
            return true;
        }
    }
}
