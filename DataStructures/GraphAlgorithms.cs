using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// One vertex reached by a breadth-first search: how many edges away from the
    /// start it is, and which vertex it was reached from.
    /// </summary>
    public class TraversalStep<T>
    {
        public TraversalStep(T vertex, int level, T? parent, bool hasParent)
        {
            Vertex = vertex;
            Level = level;
            Parent = parent;
            HasParent = hasParent;
        }

        public T Vertex { get; }

        public int Level { get; }

        public T? Parent { get; }

        public bool HasParent { get; }
    }

    /// <summary>
    /// Result of Dijkstra's algorithm from one source vertex.
    /// </summary>
    public class ShortestPathResult<T> where T : notnull
    {
        public ShortestPathResult(T source, Dictionary<T, double> distances, Dictionary<T, T> previous)
        {
            Source = source;
            Distances = distances;
            Previous = previous;
        }

        public T Source { get; }

        /// <summary>
        /// Shortest distance from the source to every reachable vertex.
        /// </summary>
        public Dictionary<T, double> Distances { get; }

        /// <summary>
        /// For each reachable vertex, the vertex before it on the shortest path.
        /// </summary>
        public Dictionary<T, T> Previous { get; }

        public bool CanReach(T target)
        {
            return Distances.ContainsKey(target);
        }

        /// <summary>
        /// The vertices on the shortest path from the source to target, inclusive.
        /// Empty if target cannot be reached.
        /// </summary>
        public List<T> PathTo(T target)
        {
            List<T> path = new List<T>();
            if (!Distances.ContainsKey(target)) return path;

            T current = target;
            path.Add(current);

            while (Previous.TryGetValue(current, out T before))
            {
                current = before;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }

    /// <summary>
    /// Result of a minimum spanning tree algorithm.
    /// </summary>
    public class SpanningTreeResult<T>
    {
        public SpanningTreeResult(List<GraphEdge<T>> edges, double totalWeight, int verticesReached, int vertexCount)
        {
            Edges = edges;
            TotalWeight = totalWeight;
            VerticesReached = verticesReached;
            VertexCount = vertexCount;
        }

        public List<GraphEdge<T>> Edges { get; }

        public double TotalWeight { get; }

        public int VerticesReached { get; }

        public int VertexCount { get; }

        /// <summary>
        /// False if the graph was not connected, so some vertices could not be reached.
        /// </summary>
        public bool SpansWholeGraph
        {
            get { return VerticesReached == VertexCount; }
        }
    }

    /// <summary>
    /// Graph traversal, ordering, shortest path and minimum spanning tree
    /// algorithms used by the Service Request Status page.
    /// </summary>
    public static class GraphAlgorithms
    {
        // ------------------------------------------------------------------
        // Traversal
        // ------------------------------------------------------------------

        /// <summary>
        /// Breadth-first search (BFS). Visits the start vertex, then all of its
        /// neighbours, then their neighbours, and so on, using a queue (first in,
        /// first out). Each vertex is reached by the fewest possible edges, so its
        /// Level is its distance in "hops" from the start. O(V + E).
        ///
        /// Used to show which requests are directly (level 1) and indirectly
        /// (level 2 and beyond) blocked by, or blocking, the selected request.
        /// </summary>
        public static List<TraversalStep<T>> BreadthFirst<T>(Graph<T> graph, T start) where T : notnull
        {
            List<TraversalStep<T>> steps = new List<TraversalStep<T>>();
            if (!graph.ContainsVertex(start)) return steps;

            HashSet<T> visited = new HashSet<T>(graph.VertexComparer) { start };
            Queue<TraversalStep<T>> queue = new Queue<TraversalStep<T>>();
            queue.Enqueue(new TraversalStep<T>(start, 0, default, false));

            while (queue.Count > 0)
            {
                TraversalStep<T> step = queue.Dequeue();
                steps.Add(step);

                foreach (GraphEdge<T> edge in graph.GetEdges(step.Vertex))
                {
                    // HashSet.Add returns false for a vertex already seen, which
                    // stops cycles in the graph from causing an endless loop.
                    if (visited.Add(edge.To))
                    {
                        queue.Enqueue(new TraversalStep<T>(edge.To, step.Level + 1, step.Vertex, true));
                    }
                }
            }

            return steps;
        }

        /// <summary>
        /// Depth-first search (DFS). Follows one path as far as it goes before
        /// backing up to try the next, using a stack (last in, first out). Returns
        /// the vertices in the order they were first visited (pre-order). O(V + E).
        ///
        /// Used to turn the crew route's spanning tree into a visiting order.
        /// </summary>
        public static List<T> DepthFirst<T>(Graph<T> graph, T start) where T : notnull
        {
            List<T> order = new List<T>();
            if (!graph.ContainsVertex(start)) return order;

            HashSet<T> visited = new HashSet<T>(graph.VertexComparer);
            Stack<T> stack = new Stack<T>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                T vertex = stack.Pop();
                if (!visited.Add(vertex)) continue;

                order.Add(vertex);

                // Push neighbours in reverse so they are visited in the order they were added.
                IReadOnlyList<GraphEdge<T>> edges = graph.GetEdges(vertex);
                for (int i = edges.Count - 1; i >= 0; i--)
                {
                    if (!visited.Contains(edges[i].To))
                    {
                        stack.Push(edges[i].To);
                    }
                }
            }

            return order;
        }

        /// <summary>
        /// True if target can be reached from start by following edges (DFS).
        /// Before a new dependency A -> B is added, the graph is checked for an
        /// existing path B -> A; if there is one, the new edge would form a cycle
        /// (A waits for B, which waits for A) and it is refused.
        /// </summary>
        public static bool HasPath<T>(Graph<T> graph, T start, T target) where T : notnull
        {
            if (!graph.ContainsVertex(start) || !graph.ContainsVertex(target)) return false;
            if (graph.VertexComparer.Equals(start, target)) return true;

            HashSet<T> visited = new HashSet<T>(graph.VertexComparer) { start };
            Stack<T> stack = new Stack<T>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                T vertex = stack.Pop();

                foreach (GraphEdge<T> edge in graph.GetEdges(vertex))
                {
                    if (graph.VertexComparer.Equals(edge.To, target)) return true;

                    if (visited.Add(edge.To))
                    {
                        stack.Push(edge.To);
                    }
                }
            }

            return false;
        }

        // ------------------------------------------------------------------
        // Ordering
        // ------------------------------------------------------------------

        /// <summary>
        /// Everything target depends on, directly or indirectly, in an order where
        /// each item comes after everything it needs, with target last.
        ///
        /// The graph's edges must point from an item to the items it needs. A
        /// depth-first search adds a vertex only after it has finished all of the
        /// vertex's prerequisites (post-order), which produces exactly that order.
        /// The graph must have no cycles; the manager guarantees this.
        /// </summary>
        public static List<T> DependencyOrder<T>(Graph<T> prerequisites, T target) where T : notnull
        {
            List<T> order = new List<T>();
            if (!prerequisites.ContainsVertex(target)) return order;

            HashSet<T> visited = new HashSet<T>(prerequisites.VertexComparer);
            AddAfterPrerequisites(prerequisites, target, visited, order);
            return order;
        }

        private static void AddAfterPrerequisites<T>(
            Graph<T> prerequisites, T vertex, HashSet<T> visited, List<T> order) where T : notnull
        {
            if (!visited.Add(vertex)) return;

            foreach (GraphEdge<T> edge in prerequisites.GetEdges(vertex))
            {
                AddAfterPrerequisites(prerequisites, edge.To, visited, order);
            }

            order.Add(vertex);
        }

        /// <summary>
        /// Topological sort of a whole directed graph (Kahn's algorithm): every
        /// vertex comes before all of the vertices its edges point to. Vertices
        /// with no incoming edges go into a queue; each one taken out "removes" its
        /// outgoing edges, which may free more vertices. O(V + E).
        ///
        /// Returns null if the graph has a cycle, because then some vertices never
        /// reach zero incoming edges. Used as a self-check that the dependency
        /// graph is acyclic.
        /// </summary>
        public static List<T>? TopologicalSort<T>(Graph<T> graph) where T : notnull
        {
            Dictionary<T, int> incoming = new Dictionary<T, int>(graph.VertexComparer);

            foreach (T vertex in graph.Vertices)
            {
                incoming[vertex] = 0;
            }

            foreach (T vertex in graph.Vertices)
            {
                foreach (GraphEdge<T> edge in graph.GetEdges(vertex))
                {
                    incoming[edge.To]++;
                }
            }

            Queue<T> ready = new Queue<T>();
            foreach (KeyValuePair<T, int> entry in incoming)
            {
                if (entry.Value == 0) ready.Enqueue(entry.Key);
            }

            List<T> order = new List<T>(graph.VertexCount);

            while (ready.Count > 0)
            {
                T vertex = ready.Dequeue();
                order.Add(vertex);

                foreach (GraphEdge<T> edge in graph.GetEdges(vertex))
                {
                    incoming[edge.To]--;
                    if (incoming[edge.To] == 0)
                    {
                        ready.Enqueue(edge.To);
                    }
                }
            }

            return order.Count == graph.VertexCount ? order : null;
        }

        // ------------------------------------------------------------------
        // Shortest paths
        // ------------------------------------------------------------------

        /// <summary>
        /// Dijkstra's algorithm: the shortest distance from source to every other
        /// vertex in a graph with non-negative weights. A min-heap always hands back
        /// the closest vertex not yet finalised; its distance can no longer improve,
        /// so its neighbours are relaxed through it. O((V + E) log V).
        ///
        /// Instead of a "decrease key" operation, an improved distance is simply
        /// pushed again and stale entries are skipped when they come out (lazy deletion).
        /// </summary>
        public static ShortestPathResult<T> ShortestPaths<T>(Graph<T> graph, T source) where T : notnull
        {
            Dictionary<T, double> distances = new Dictionary<T, double>(graph.VertexComparer);
            Dictionary<T, T> previous = new Dictionary<T, T>(graph.VertexComparer);
            HashSet<T> finalised = new HashSet<T>(graph.VertexComparer);

            if (!graph.ContainsVertex(source))
            {
                return new ShortestPathResult<T>(source, distances, previous);
            }

            IComparer<KeyValuePair<double, T>> byDistance =
                Comparer<KeyValuePair<double, T>>.Create((a, b) => a.Key.CompareTo(b.Key));
            MinHeap<KeyValuePair<double, T>> frontier = new MinHeap<KeyValuePair<double, T>>(byDistance);

            distances[source] = 0;
            frontier.Insert(new KeyValuePair<double, T>(0, source));

            while (frontier.TryExtractMin(out KeyValuePair<double, T> entry))
            {
                T vertex = entry.Value;
                if (!finalised.Add(vertex)) continue;

                foreach (GraphEdge<T> edge in graph.GetEdges(vertex))
                {
                    if (finalised.Contains(edge.To)) continue;

                    double candidate = entry.Key + edge.Weight;

                    if (!distances.TryGetValue(edge.To, out double known) || candidate < known)
                    {
                        distances[edge.To] = candidate;
                        previous[edge.To] = vertex;
                        frontier.Insert(new KeyValuePair<double, T>(candidate, edge.To));
                    }
                }
            }

            return new ShortestPathResult<T>(source, distances, previous);
        }

        // ------------------------------------------------------------------
        // Minimum spanning trees
        // ------------------------------------------------------------------

        /// <summary>
        /// Prim's algorithm for a minimum spanning tree (MST) of an undirected,
        /// weighted graph: the set of edges that connects every vertex, with no
        /// cycles, at the lowest possible total weight. A spanning tree of V
        /// vertices always has exactly V - 1 edges.
        ///
        /// Starting from one vertex, the tree grows one edge at a time, always
        /// taking the cheapest edge that reaches a vertex not yet in the tree. A
        /// min-heap holds the candidate edges so the cheapest is found in O(log E).
        /// Overall O(E log E).
        /// </summary>
        public static SpanningTreeResult<T> PrimMinimumSpanningTree<T>(Graph<T> graph, T start) where T : notnull
        {
            List<GraphEdge<T>> treeEdges = new List<GraphEdge<T>>();
            double total = 0;

            if (!graph.ContainsVertex(start))
            {
                return new SpanningTreeResult<T>(treeEdges, 0, 0, graph.VertexCount);
            }

            IComparer<GraphEdge<T>> byWeight =
                Comparer<GraphEdge<T>>.Create((a, b) => a.Weight.CompareTo(b.Weight));
            MinHeap<GraphEdge<T>> candidates = new MinHeap<GraphEdge<T>>(byWeight);
            HashSet<T> inTree = new HashSet<T>(graph.VertexComparer) { start };

            foreach (GraphEdge<T> edge in graph.GetEdges(start))
            {
                candidates.Insert(edge);
            }

            while (inTree.Count < graph.VertexCount && candidates.TryExtractMin(out GraphEdge<T> cheapest))
            {
                // Both ends already in the tree: this edge would make a cycle.
                if (!inTree.Add(cheapest.To)) continue;

                treeEdges.Add(cheapest);
                total += cheapest.Weight;

                foreach (GraphEdge<T> edge in graph.GetEdges(cheapest.To))
                {
                    if (!inTree.Contains(edge.To))
                    {
                        candidates.Insert(edge);
                    }
                }
            }

            return new SpanningTreeResult<T>(treeEdges, total, inTree.Count, graph.VertexCount);
        }

        /// <summary>
        /// Kruskal's algorithm for a minimum spanning tree: sort every edge by
        /// weight, then take each edge in turn unless its two ends are already
        /// connected. A disjoint-set (union-find) structure answers "already
        /// connected?" in almost O(1). O(E log E).
        ///
        /// Prim's algorithm is the one used for the crew route; Kruskal's is run on
        /// the same graph as a self-check, since both must find the same total weight.
        /// </summary>
        public static SpanningTreeResult<T> KruskalMinimumSpanningTree<T>(Graph<T> graph) where T : notnull
        {
            List<GraphEdge<T>> sorted = new List<GraphEdge<T>>(graph.Edges);
            sorted.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            DisjointSet<T> components = new DisjointSet<T>(graph.Vertices, graph.VertexComparer);
            List<GraphEdge<T>> treeEdges = new List<GraphEdge<T>>();
            double total = 0;

            foreach (GraphEdge<T> edge in sorted)
            {
                if (treeEdges.Count == graph.VertexCount - 1) break;

                if (components.Union(edge.From, edge.To))
                {
                    treeEdges.Add(edge);
                    total += edge.Weight;
                }
            }

            int reached = graph.VertexCount == 0 ? 0 : treeEdges.Count + 1;
            return new SpanningTreeResult<T>(treeEdges, total, reached, graph.VertexCount);
        }
    }

    /// <summary>
    /// Disjoint-set (union-find) with path compression and union by rank. Keeps
    /// track of which vertices are already connected while Kruskal's algorithm runs.
    /// </summary>
    public class DisjointSet<T> where T : notnull
    {
        private readonly Dictionary<T, T> _parent;
        private readonly Dictionary<T, int> _rank;

        public DisjointSet(IEnumerable<T> items, IEqualityComparer<T> comparer)
        {
            _parent = new Dictionary<T, T>(comparer);
            _rank = new Dictionary<T, int>(comparer);

            foreach (T item in items)
            {
                _parent[item] = item;
                _rank[item] = 0;
            }
        }

        /// <summary>
        /// The representative of the set holding item. Every node on the way is
        /// pointed straight at the representative so later lookups are faster.
        /// </summary>
        public T Find(T item)
        {
            T root = item;
            while (!_parent.Comparer.Equals(_parent[root], root))
            {
                root = _parent[root];
            }

            T current = item;
            while (!_parent.Comparer.Equals(current, root))
            {
                T next = _parent[current];
                _parent[current] = root;
                current = next;
            }

            return root;
        }

        /// <summary>
        /// Joins the sets holding a and b. Returns false if they were already joined.
        /// </summary>
        public bool Union(T a, T b)
        {
            T rootA = Find(a);
            T rootB = Find(b);

            if (_parent.Comparer.Equals(rootA, rootB)) return false;

            int rankA = _rank[rootA];
            int rankB = _rank[rootB];

            if (rankA < rankB)
            {
                _parent[rootA] = rootB;
            }
            else if (rankA > rankB)
            {
                _parent[rootB] = rootA;
            }
            else
            {
                _parent[rootB] = rootA;
                _rank[rootA] = rankA + 1;
            }

            return true;
        }
    }
}
