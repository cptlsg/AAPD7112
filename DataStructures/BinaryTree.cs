using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A binary tree: every node has at most two children, a left and a right.
    ///
    /// This class holds what all binary trees have in common, whatever rule decides
    /// where a node goes: the root, the node count, the height, and the four standard
    /// traversals. The binary search tree, AVL tree and red-black tree all inherit
    /// from it and only add their own insert and search rules.
    ///
    ///   In-order     left, node, right   -> keys in ascending order for a search tree
    ///   Reverse      right, node, left   -> keys in descending order
    ///   Pre-order    node, left, right   -> parents before children (copying a tree)
    ///   Post-order   left, right, node   -> children before parents (totals, clean-up)
    ///   Level-order  one level at a time -> the tree's shape, top to bottom
    ///
    /// Every traversal visits each node once, so each costs O(n). They are written
    /// with an explicit stack or queue instead of recursion, so a badly unbalanced
    /// tree cannot overflow the call stack.
    /// </summary>
    public class BinaryTree<TKey, TValue>
    {
        protected BinaryTreeNode<TKey, TValue>? Root { get; set; }

        public int Count { get; protected set; }

        public bool IsEmpty
        {
            get { return Root == null; }
        }

        /// <summary>
        /// Number of levels in the tree: 0 when empty, 1 for a single node.
        /// A perfectly balanced tree of n nodes has a height of about log2(n);
        /// a tree that has become a chain has a height of n.
        /// </summary>
        public int Height
        {
            get { return MeasureHeight(Root); }
        }

        /// <summary>
        /// The smallest height any binary tree holding Count nodes could have.
        /// Comparing Height with this shows how well balanced the tree is.
        /// </summary>
        public int MinimumPossibleHeight
        {
            get { return Count == 0 ? 0 : (int)Math.Floor(Math.Log(Count, 2)) + 1; }
        }

        public int LeafCount
        {
            get
            {
                int leaves = 0;
                foreach (BinaryTreeNode<TKey, TValue> node in LevelOrderNodes())
                {
                    if (node.IsLeaf) leaves++;
                }
                return leaves;
            }
        }

        // ------------------------------------------------------------------
        // Traversals
        // ------------------------------------------------------------------

        /// <summary>
        /// Left subtree, node, right subtree. For a search tree this returns the
        /// entries sorted by key, which is how the request list is ordered.
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> InOrder()
        {
            List<KeyValuePair<TKey, TValue>> result = new List<KeyValuePair<TKey, TValue>>(Count);
            Stack<BinaryTreeNode<TKey, TValue>> stack = new Stack<BinaryTreeNode<TKey, TValue>>();
            BinaryTreeNode<TKey, TValue>? current = Root;

            while (current != null || stack.Count > 0)
            {
                // Go as far left as possible, remembering the path.
                while (current != null)
                {
                    stack.Push(current);
                    current = current.Left;
                }

                current = stack.Pop();
                result.Add(new KeyValuePair<TKey, TValue>(current.Key, current.Value));
                current = current.Right;
            }

            return result;
        }

        /// <summary>
        /// Right subtree, node, left subtree: the in-order sequence backwards, so a
        /// search tree returns its entries from the largest key to the smallest.
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> ReverseInOrder()
        {
            List<KeyValuePair<TKey, TValue>> result = new List<KeyValuePair<TKey, TValue>>(Count);
            Stack<BinaryTreeNode<TKey, TValue>> stack = new Stack<BinaryTreeNode<TKey, TValue>>();
            BinaryTreeNode<TKey, TValue>? current = Root;

            while (current != null || stack.Count > 0)
            {
                while (current != null)
                {
                    stack.Push(current);
                    current = current.Right;
                }

                current = stack.Pop();
                result.Add(new KeyValuePair<TKey, TValue>(current.Key, current.Value));
                current = current.Left;
            }

            return result;
        }

        /// <summary>
        /// Node, left subtree, right subtree.
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> PreOrder()
        {
            List<KeyValuePair<TKey, TValue>> result = new List<KeyValuePair<TKey, TValue>>(Count);
            if (Root == null) return result;

            Stack<BinaryTreeNode<TKey, TValue>> stack = new Stack<BinaryTreeNode<TKey, TValue>>();
            stack.Push(Root);

            while (stack.Count > 0)
            {
                BinaryTreeNode<TKey, TValue> node = stack.Pop();
                result.Add(new KeyValuePair<TKey, TValue>(node.Key, node.Value));

                // Right is pushed first so that left is popped (visited) first.
                if (node.Right != null) stack.Push(node.Right);
                if (node.Left != null) stack.Push(node.Left);
            }

            return result;
        }

        /// <summary>
        /// Left subtree, right subtree, node. Uses two stacks: the first produces
        /// node, right, left; the second reverses that into left, right, node.
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> PostOrder()
        {
            List<KeyValuePair<TKey, TValue>> result = new List<KeyValuePair<TKey, TValue>>(Count);
            if (Root == null) return result;

            Stack<BinaryTreeNode<TKey, TValue>> pending = new Stack<BinaryTreeNode<TKey, TValue>>();
            Stack<BinaryTreeNode<TKey, TValue>> output = new Stack<BinaryTreeNode<TKey, TValue>>();
            pending.Push(Root);

            while (pending.Count > 0)
            {
                BinaryTreeNode<TKey, TValue> node = pending.Pop();
                output.Push(node);

                if (node.Left != null) pending.Push(node.Left);
                if (node.Right != null) pending.Push(node.Right);
            }

            while (output.Count > 0)
            {
                BinaryTreeNode<TKey, TValue> node = output.Pop();
                result.Add(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
            }

            return result;
        }

        /// <summary>
        /// Breadth-first: the root, then every node one level down, and so on.
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> LevelOrder()
        {
            List<KeyValuePair<TKey, TValue>> result = new List<KeyValuePair<TKey, TValue>>(Count);
            foreach (BinaryTreeNode<TKey, TValue> node in LevelOrderNodes())
            {
                result.Add(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
            }
            return result;
        }

        /// <summary>
        /// How many nodes sit on each level, from the root down. A balanced tree
        /// roughly doubles on each level; a chain has one node per level.
        /// </summary>
        public List<int> NodesPerLevel()
        {
            List<int> widths = new List<int>();
            if (Root == null) return widths;

            Queue<BinaryTreeNode<TKey, TValue>> queue = new Queue<BinaryTreeNode<TKey, TValue>>();
            queue.Enqueue(Root);

            while (queue.Count > 0)
            {
                int levelSize = queue.Count;
                widths.Add(levelSize);

                for (int i = 0; i < levelSize; i++)
                {
                    BinaryTreeNode<TKey, TValue> node = queue.Dequeue();
                    if (node.Left != null) queue.Enqueue(node.Left);
                    if (node.Right != null) queue.Enqueue(node.Right);
                }
            }

            return widths;
        }

        protected List<BinaryTreeNode<TKey, TValue>> LevelOrderNodes()
        {
            List<BinaryTreeNode<TKey, TValue>> nodes = new List<BinaryTreeNode<TKey, TValue>>(Count);
            if (Root == null) return nodes;

            Queue<BinaryTreeNode<TKey, TValue>> queue = new Queue<BinaryTreeNode<TKey, TValue>>();
            queue.Enqueue(Root);

            while (queue.Count > 0)
            {
                BinaryTreeNode<TKey, TValue> node = queue.Dequeue();
                nodes.Add(node);

                if (node.Left != null) queue.Enqueue(node.Left);
                if (node.Right != null) queue.Enqueue(node.Right);
            }

            return nodes;
        }

        /// <summary>
        /// Height of a subtree, measured level by level so that a long chain does
        /// not recurse deeply.
        /// </summary>
        protected static int MeasureHeight(BinaryTreeNode<TKey, TValue>? start)
        {
            if (start == null) return 0;

            int height = 0;
            Queue<BinaryTreeNode<TKey, TValue>> queue = new Queue<BinaryTreeNode<TKey, TValue>>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                height++;
                int levelSize = queue.Count;

                for (int i = 0; i < levelSize; i++)
                {
                    BinaryTreeNode<TKey, TValue> node = queue.Dequeue();
                    if (node.Left != null) queue.Enqueue(node.Left);
                    if (node.Right != null) queue.Enqueue(node.Right);
                }
            }

            return height;
        }
    }
}
