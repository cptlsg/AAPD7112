using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A binary min-heap: a complete binary tree in which every parent is smaller
    /// than (or equal to) its children, so the smallest item is always at the root.
    ///
    /// Because the tree is complete (every level full except the last, which fills
    /// from the left), it is stored in a plain list with no child pointers:
    ///
    ///   children of index i:  2i + 1 and 2i + 2
    ///   parent of index i:    (i - 1) / 2
    ///
    ///   Peek          O(1)       the root is at index 0
    ///   Insert        O(log n)   add at the end, then sift up
    ///   ExtractMin    O(log n)   move the last item to the root, then sift down
    ///   Build heap    O(n)       sift down every parent, from the bottom up
    ///
    /// "Smaller" is decided by the comparer. The application uses this heap to
    /// order requests waiting for a crew by urgency (Critical first, then oldest
    /// first), and inside Dijkstra's and Prim's algorithms to pick the closest
    /// suburb or the cheapest road next.
    /// </summary>
    public class MinHeap<T>
    {
        private readonly List<T> _items;
        private readonly IComparer<T> _comparer;

        public MinHeap(IComparer<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _items = new List<T>();
        }

        /// <summary>
        /// Builds a heap from existing items in O(n) (Floyd's method) instead of
        /// inserting them one at a time in O(n log n).
        /// </summary>
        public MinHeap(IEnumerable<T> items, IComparer<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _items = new List<T>(items ?? throw new ArgumentNullException(nameof(items)));
            BuildHeap();
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsEmpty
        {
            get { return _items.Count == 0; }
        }

        /// <summary>
        /// The underlying array in heap order (index 0 is the root), for display.
        /// </summary>
        public IReadOnlyList<T> Items
        {
            get { return _items; }
        }

        public void Insert(T item)
        {
            _items.Add(item);
            SiftUp(_items.Count - 1);
        }

        public T Peek()
        {
            if (_items.Count == 0)
            {
                throw new InvalidOperationException("The heap is empty.");
            }
            return _items[0];
        }

        public T ExtractMin()
        {
            if (_items.Count == 0)
            {
                throw new InvalidOperationException("The heap is empty.");
            }

            T smallest = _items[0];
            int lastIndex = _items.Count - 1;

            _items[0] = _items[lastIndex];
            _items.RemoveAt(lastIndex);

            if (_items.Count > 0)
            {
                SiftDown(0);
            }

            return smallest;
        }

        public bool TryExtractMin(out T item)
        {
            if (_items.Count == 0)
            {
                item = default!;
                return false;
            }

            item = ExtractMin();
            return true;
        }

        /// <summary>
        /// A separate copy of this heap. Taking items from the copy leaves this
        /// heap unchanged, which is how the "next in line" list is shown without
        /// actually dispatching anything.
        /// </summary>
        public MinHeap<T> Clone()
        {
            // A copy of a valid heap array is already a valid heap; BuildHeap will
            // find nothing to move, so this is a straight O(n) copy.
            return new MinHeap<T>(_items, _comparer);
        }

        /// <summary>
        /// True when every parent is no larger than its children.
        /// </summary>
        public bool IsValid()
        {
            for (int i = 1; i < _items.Count; i++)
            {
                int parent = (i - 1) / 2;
                if (_comparer.Compare(_items[parent], _items[i]) > 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Moves the item at index up while it is smaller than its parent.
        /// </summary>
        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;

                if (_comparer.Compare(_items[index], _items[parent]) >= 0)
                {
                    break;
                }

                Swap(index, parent);
                index = parent;
            }
        }

        /// <summary>
        /// Moves the item at index down, swapping with its smaller child, until
        /// neither child is smaller.
        /// </summary>
        private void SiftDown(int index)
        {
            int count = _items.Count;

            while (true)
            {
                int left = 2 * index + 1;
                if (left >= count)
                {
                    break;
                }

                int right = left + 1;
                int smaller = left;

                if (right < count && _comparer.Compare(_items[right], _items[left]) < 0)
                {
                    smaller = right;
                }

                if (_comparer.Compare(_items[smaller], _items[index]) >= 0)
                {
                    break;
                }

                Swap(index, smaller);
                index = smaller;
            }
        }

        private void BuildHeap()
        {
            // Leaves are already valid one-item heaps; start at the last parent.
            for (int i = _items.Count / 2 - 1; i >= 0; i--)
            {
                SiftDown(i);
            }
        }

        private void Swap(int a, int b)
        {
            T temporary = _items[a];
            _items[a] = _items[b];
            _items[b] = temporary;
        }
    }
}
