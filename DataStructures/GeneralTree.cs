using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A node in a general (n-ary) tree. Unlike a binary tree node it can have any
    /// number of children, which suits hierarchies such as folders or an
    /// organisation chart.
    /// </summary>
    public class GeneralTreeNode<T>
    {
        private readonly List<GeneralTreeNode<T>> _children = new List<GeneralTreeNode<T>>();

        public GeneralTreeNode(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public GeneralTreeNode<T>? Parent { get; private set; }

        public IReadOnlyList<GeneralTreeNode<T>> Children
        {
            get { return _children; }
        }

        public bool IsRoot
        {
            get { return Parent == null; }
        }

        public bool IsLeaf
        {
            get { return _children.Count == 0; }
        }

        /// <summary>
        /// Number of edges between this node and the root (the root has depth 0).
        /// </summary>
        public int Depth
        {
            get
            {
                int depth = 0;
                GeneralTreeNode<T>? ancestor = Parent;

                while (ancestor != null)
                {
                    depth++;
                    ancestor = ancestor.Parent;
                }

                return depth;
            }
        }

        public GeneralTreeNode<T> AddChild(T value)
        {
            GeneralTreeNode<T> child = new GeneralTreeNode<T>(value);
            child.Parent = this;
            _children.Add(child);
            return child;
        }

        /// <summary>
        /// The first direct child whose value matches, or null.
        /// </summary>
        public GeneralTreeNode<T>? FindChild(Func<T, bool> match)
        {
            foreach (GeneralTreeNode<T> child in _children)
            {
                if (match(child.Value)) return child;
            }
            return null;
        }
    }

    /// <summary>
    /// A general tree: one root, and every other node has exactly one parent and
    /// any number of children. There are no cycles, so there is exactly one path
    /// from the root to each node.
    ///
    /// The application uses one to organise service requests by department:
    ///
    ///   City of Tshwane                      (root)
    ///   +-- Water and Sanitation             (department)
    ///   |   +-- Water and Sewerage           (category)
    ///   |   |   +-- MSA-2026-0011 ...        (request, a leaf)
    ///   |   +-- Sanitation
    ///   +-- Energy and Electricity
    ///   ...
    ///
    /// Choosing a department collects every request below it with a pre-order
    /// traversal, and the open/total counts beside each branch are added up from
    /// the leaves with a post-order traversal.
    /// </summary>
    public class GeneralTree<T>
    {
        public GeneralTree(T rootValue)
        {
            Root = new GeneralTreeNode<T>(rootValue);
        }

        public GeneralTreeNode<T> Root { get; }

        public int Count
        {
            get { return PreOrder(Root).Count; }
        }

        /// <summary>
        /// Number of levels from the root to the deepest leaf (a lone root has height 1).
        /// </summary>
        public int Height
        {
            get
            {
                int deepest = 0;
                foreach (GeneralTreeNode<T> node in PreOrder(Root))
                {
                    if (node.IsLeaf)
                    {
                        deepest = Math.Max(deepest, node.Depth);
                    }
                }
                return deepest + 1;
            }
        }

        /// <summary>
        /// Parent first, then each child's subtree from left to right.
        /// </summary>
        public List<GeneralTreeNode<T>> PreOrder(GeneralTreeNode<T>? start = null)
        {
            List<GeneralTreeNode<T>> result = new List<GeneralTreeNode<T>>();
            Stack<GeneralTreeNode<T>> stack = new Stack<GeneralTreeNode<T>>();
            stack.Push(start ?? Root);

            while (stack.Count > 0)
            {
                GeneralTreeNode<T> node = stack.Pop();
                result.Add(node);

                // Push in reverse so the first child is popped (visited) first.
                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Every child's subtree first, then the parent. A parent always comes
        /// after all of its descendants, so totals can be built from the leaves up.
        /// </summary>
        public List<GeneralTreeNode<T>> PostOrder(GeneralTreeNode<T>? start = null)
        {
            List<GeneralTreeNode<T>> result = new List<GeneralTreeNode<T>>();
            AddPostOrder(start ?? Root, result);
            return result;
        }

        private static void AddPostOrder(GeneralTreeNode<T> node, List<GeneralTreeNode<T>> result)
        {
            foreach (GeneralTreeNode<T> child in node.Children)
            {
                AddPostOrder(child, result);
            }
            result.Add(node);
        }

        /// <summary>
        /// The root, then all nodes at depth 1, then depth 2, and so on.
        /// </summary>
        public List<GeneralTreeNode<T>> LevelOrder(GeneralTreeNode<T>? start = null)
        {
            List<GeneralTreeNode<T>> result = new List<GeneralTreeNode<T>>();
            Queue<GeneralTreeNode<T>> queue = new Queue<GeneralTreeNode<T>>();
            queue.Enqueue(start ?? Root);

            while (queue.Count > 0)
            {
                GeneralTreeNode<T> node = queue.Dequeue();
                result.Add(node);

                foreach (GeneralTreeNode<T> child in node.Children)
                {
                    queue.Enqueue(child);
                }
            }

            return result;
        }

        /// <summary>
        /// Breadth-first search for the first node whose value matches.
        /// </summary>
        public GeneralTreeNode<T>? Find(Func<T, bool> match)
        {
            foreach (GeneralTreeNode<T> node in LevelOrder(Root))
            {
                if (match(node.Value)) return node;
            }
            return null;
        }

        /// <summary>
        /// The nodes from the root down to the given node, e.g. for a breadcrumb.
        /// </summary>
        public List<GeneralTreeNode<T>> PathFromRoot(GeneralTreeNode<T> node)
        {
            List<GeneralTreeNode<T>> path = new List<GeneralTreeNode<T>>();
            GeneralTreeNode<T>? current = node;

            while (current != null)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// The values of every leaf in the subtree below start, in pre-order.
        /// </summary>
        public List<T> LeafValues(GeneralTreeNode<T>? start = null)
        {
            List<T> leaves = new List<T>();
            foreach (GeneralTreeNode<T> node in PreOrder(start))
            {
                if (node.IsLeaf) leaves.Add(node.Value);
            }
            return leaves;
        }
    }
}
