using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// A connection between two vertices, with an optional weight (a cost such as
    /// a distance in kilometres).
    /// </summary>
    public class GraphEdge<T>
    {
        public GraphEdge(T from, T to, double weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }

        public T From { get; }

        public T To { get; }

        public double Weight { get; }

        public override string ToString()
        {
            return string.Format("{0} -> {1} ({2})", From, To, Weight);
        }
    }

    /// <summary>
    /// A graph: a set of vertices (things) joined by edges (relationships). Unlike
    /// a tree there is no root, a vertex can have any number of neighbours, and
    /// paths can loop back on themselves.
    ///
    ///   Directed    an edge goes one way: A -> B does not imply B -> A
    ///   Undirected  every edge works both ways (stored as two directed edges)
    ///   Weighted    every edge has a cost
    ///
    /// The graph is stored as an adjacency list: a dictionary from each vertex to
    /// the list of edges leaving it. Finding a vertex's neighbours is O(1) to reach
    /// the list plus O(degree) to read it, and the whole graph takes O(V + E) space,
    /// which suits sparse graphs like these far better than a V x V matrix.
    ///
    /// The application uses two kinds:
    ///   - a directed graph of dependencies between service requests
    ///     (a burst pipe must be fixed before the road above it is resurfaced)
    ///   - an undirected, weighted graph of roads between Tshwane suburbs
    /// </summary>
    public class Graph<T> where T : notnull
    {
        private readonly Dictionary<T, List<GraphEdge<T>>> _adjacency;
        private readonly List<GraphEdge<T>> _edges = new List<GraphEdge<T>>();
        private readonly IEqualityComparer<T> _vertexComparer;

        public Graph(bool isDirected) : this(isDirected, null)
        {
        }

        public Graph(bool isDirected, IEqualityComparer<T>? vertexComparer)
        {
            IsDirected = isDirected;
            _vertexComparer = vertexComparer ?? EqualityComparer<T>.Default;
            _adjacency = new Dictionary<T, List<GraphEdge<T>>>(_vertexComparer);
        }

        public bool IsDirected { get; }

        public IEqualityComparer<T> VertexComparer
        {
            get { return _vertexComparer; }
        }

        public int VertexCount
        {
            get { return _adjacency.Count; }
        }

        /// <summary>
        /// Number of edges added. An undirected edge counts once.
        /// </summary>
        public int EdgeCount
        {
            get { return _edges.Count; }
        }

        public IEnumerable<T> Vertices
        {
            get { return _adjacency.Keys; }
        }

        /// <summary>
        /// Every edge as it was added (an undirected edge appears once).
        /// </summary>
        public IReadOnlyList<GraphEdge<T>> Edges
        {
            get { return _edges; }
        }

        public bool AddVertex(T vertex)
        {
            return _adjacency.TryAdd(vertex, new List<GraphEdge<T>>());
        }

        public bool ContainsVertex(T vertex)
        {
            return _adjacency.ContainsKey(vertex);
        }

        /// <summary>
        /// Adds an edge, creating either vertex if it does not exist yet. An
        /// undirected graph also stores the reverse edge so that both ends see it.
        /// </summary>
        public void AddEdge(T from, T to, double weight = 1)
        {
            if (weight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), "Edge weights cannot be negative.");
            }

            AddVertex(from);
            AddVertex(to);

            GraphEdge<T> edge = new GraphEdge<T>(from, to, weight);
            _adjacency[from].Add(edge);
            _edges.Add(edge);

            if (!IsDirected)
            {
                _adjacency[to].Add(new GraphEdge<T>(to, from, weight));
            }
        }

        public bool HasEdge(T from, T to)
        {
            if (!_adjacency.TryGetValue(from, out List<GraphEdge<T>>? edges))
            {
                return false;
            }

            foreach (GraphEdge<T> edge in edges)
            {
                if (_vertexComparer.Equals(edge.To, to)) return true;
            }
            return false;
        }

        /// <summary>
        /// The edges leaving a vertex (empty if the vertex is unknown).
        /// </summary>
        public IReadOnlyList<GraphEdge<T>> GetEdges(T vertex)
        {
            if (_adjacency.TryGetValue(vertex, out List<GraphEdge<T>>? edges))
            {
                return edges;
            }
            return Array.Empty<GraphEdge<T>>();
        }

        public int Degree(T vertex)
        {
            return GetEdges(vertex).Count;
        }
    }
}
