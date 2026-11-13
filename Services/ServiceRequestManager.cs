using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MunicipalServicesApp.Data;
using MunicipalServicesApp.DataStructures;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// Stores every service request and keeps one data structure for each job the
    /// Service Request Status page does, so that no feature has to scan a list:
    ///
    ///   AvlTree            reference number -> request   track by ID, list by reference
    ///   RedBlackTree       (submitted, ref) -> request   list by date, date-range filter
    ///   BinarySearchTree   suburb -> requests            "suburb starts with" search
    ///   GeneralTree        department > category > request   browse tree with counts
    ///   MinHeap            requests waiting for a crew   most urgent first, dispatch
    ///   Graph (directed)   dependencies between requests blocked status, BFS, DFS
    ///   Graph (weighted)   roads between suburbs         Dijkstra + Prim crew route
    ///
    /// A second, unbalanced BinarySearchTree keyed by reference number is kept only
    /// so the page can show why the AVL tree is needed (see TrackingResult).
    ///
    /// Every request is added through Add, which updates all of the structures
    /// together so they can never disagree with each other.
    /// </summary>
    public class ServiceRequestManager
    {
        private static readonly Lazy<ServiceRequestManager> SharedInstance =
            new Lazy<ServiceRequestManager>(() => CreateSeeded(DateTime.Now));

        /// <summary>
        /// The single instance used by the application, loaded with sample data the
        /// first time it is needed. It lives for the whole session, so requests from
        /// the Report Issues page appear on the status page straight away.
        /// </summary>
        public static ServiceRequestManager Shared
        {
            get { return SharedInstance.Value; }
        }

        private static readonly Regex ReferenceWithYear =
            new Regex(@"^(?:MSA)?-?(\d{4})-?(\d{1,4})$", RegexOptions.Compiled);

        private static readonly Regex SequenceOnly =
            new Regex(@"^\d{1,4}$", RegexOptions.Compiled);

        // Date first, then reference number, so two requests submitted at the same
        // moment still have different keys.
        private static readonly IComparer<(DateTime, string)> SubmissionKeyComparer =
            Comparer<(DateTime, string)>.Create((a, b) =>
            {
                int byDate = a.Item1.CompareTo(b.Item1);
                return byDate != 0 ? byDate : string.CompareOrdinal(a.Item2, b.Item2);
            });

        // Most urgent first: priority, then the request that has waited longest.
        private static readonly IComparer<ServiceRequest> UrgencyComparer =
            Comparer<ServiceRequest>.Create((a, b) =>
            {
                int byPriority = ((int)a.Priority).CompareTo((int)b.Priority);
                if (byPriority != 0) return byPriority;

                int byAge = a.SubmittedOn.CompareTo(b.SubmittedOn);
                if (byAge != 0) return byAge;

                return string.CompareOrdinal(a.ReferenceNumber, b.ReferenceNumber);
            });

        // ---- Trees ----
        private readonly AvlTree<string, ServiceRequest> _byReference =
            new AvlTree<string, ServiceRequest>(StringComparer.Ordinal);

        private readonly BinarySearchTree<string, ServiceRequest> _byReferenceUnbalanced =
            new BinarySearchTree<string, ServiceRequest>(StringComparer.Ordinal);

        private readonly RedBlackTree<(DateTime, string), ServiceRequest> _bySubmission =
            new RedBlackTree<(DateTime, string), ServiceRequest>(SubmissionKeyComparer);

        private readonly BinarySearchTree<string, List<ServiceRequest>> _bySuburb =
            new BinarySearchTree<string, List<ServiceRequest>>(StringComparer.OrdinalIgnoreCase);

        private readonly GeneralTree<CategoryTreeItem> _departmentTree;

        // Category name -> its branch in the department tree, so a new request can be
        // attached without searching the tree.
        private readonly Dictionary<string, GeneralTreeNode<CategoryTreeItem>> _categoryNodes =
            new Dictionary<string, GeneralTreeNode<CategoryTreeItem>>(StringComparer.OrdinalIgnoreCase);

        // ---- Heap ----
        private readonly MinHeap<ServiceRequest> _dispatchQueue = new MinHeap<ServiceRequest>(UrgencyComparer);

        // ---- Graphs ----
        // prerequisite -> dependent ("resolving A unblocks B")
        private readonly Graph<string> _unblocks = new Graph<string>(true, StringComparer.Ordinal);

        // dependent -> prerequisite ("B waits for A"): the same edges reversed
        private readonly Graph<string> _waitsFor = new Graph<string>(true, StringComparer.Ordinal);

        private readonly Graph<string> _roads = new Graph<string>(false, StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, SuburbLocation> _suburbs =
            new Dictionary<string, SuburbLocation>(StringComparer.OrdinalIgnoreCase);

        private readonly Func<DateTime> _clock;

        public ServiceRequestManager() : this(null)
        {
        }

        public ServiceRequestManager(Func<DateTime>? clock)
        {
            _clock = clock ?? (() => DateTime.Now);
            Depot = ServiceRequestSeedData.Depot;

            _departmentTree = new GeneralTree<CategoryTreeItem>(
                new CategoryTreeItem(ServiceDepartments.MunicipalityName, CategoryTreeLevel.Municipality));

            // Create every department and category branch up front so the tree
            // always shows the full structure, even for categories with no requests.
            foreach (KeyValuePair<string, string[]> department in ServiceDepartments.Structure)
            {
                GeneralTreeNode<CategoryTreeItem> departmentNode = _departmentTree.Root.AddChild(
                    new CategoryTreeItem(department.Key, CategoryTreeLevel.Department));

                foreach (string category in department.Value)
                {
                    _categoryNodes[category] = departmentNode.AddChild(
                        new CategoryTreeItem(category, CategoryTreeLevel.Category));
                }
            }
        }

        /// <summary>
        /// Builds a manager with the sample suburbs, roads and requests.
        /// </summary>
        public static ServiceRequestManager CreateSeeded(DateTime now)
        {
            ServiceRequestManager manager = new ServiceRequestManager();

            foreach (SuburbLocation suburb in ServiceRequestSeedData.CreateSuburbs())
            {
                manager.AddSuburb(suburb);
            }

            foreach (KeyValuePair<string, string> road in ServiceRequestSeedData.CreateRoads())
            {
                manager.AddRoad(road.Key, road.Value);
            }

            // Work out when each sample was submitted, then issue reference numbers
            // in that order, exactly as they would have been issued at the time.
            List<KeyValuePair<DateTime, ServiceRequestSeedData.SeedRequest>> seeds =
                new List<KeyValuePair<DateTime, ServiceRequestSeedData.SeedRequest>>();

            DateTime latestAllowed = now.AddMinutes(-30);

            foreach (ServiceRequestSeedData.SeedRequest seed in ServiceRequestSeedData.CreateRequests())
            {
                DateTime submitted = now.Date.AddDays(-seed.DaysAgo).AddHours(seed.Hour).AddMinutes(seed.Minute);
                if (submitted > latestAllowed)
                {
                    submitted = latestAllowed.AddMinutes(-seed.Key);
                }

                seeds.Add(new KeyValuePair<DateTime, ServiceRequestSeedData.SeedRequest>(submitted, seed));
            }

            seeds.Sort((a, b) =>
            {
                int byDate = a.Key.CompareTo(b.Key);
                return byDate != 0 ? byDate : a.Value.Key.CompareTo(b.Value.Key);
            });

            Dictionary<int, string> referenceByKey = new Dictionary<int, string>();

            foreach (KeyValuePair<DateTime, ServiceRequestSeedData.SeedRequest> entry in seeds)
            {
                ServiceRequestSeedData.SeedRequest seed = entry.Value;
                string reference = manager.NextReferenceNumber(entry.Key);

                ServiceRequest request = new ServiceRequest(
                    reference, seed.Title, seed.Category, seed.Suburb, seed.Street, seed.Description,
                    seed.Priority, entry.Key, "Logged by the municipal call centre.");

                ApplySampleHistory(request, seed.Status, now);
                manager.Add(request);
                referenceByKey[seed.Key] = reference;
            }

            foreach (KeyValuePair<DateTime, ServiceRequestSeedData.SeedRequest> entry in seeds)
            {
                foreach (int prerequisiteKey in entry.Value.DependsOn)
                {
                    manager.TryAddDependency(
                        referenceByKey[prerequisiteKey], referenceByKey[entry.Value.Key], out string _);
                }
            }

            manager.RefreshTreeCounts();
            return manager;
        }

        /// <summary>
        /// Moves a sample request through each stage up to its current status,
        /// spacing the updates out between submission and now.
        /// </summary>
        private static void ApplySampleHistory(ServiceRequest request, RequestStatus status, DateTime now)
        {
            int stages = (int)status;
            if (stages == 0) return;

            double elapsedHours = Math.Max(1, (now - request.SubmittedOn).TotalHours);
            double hoursPerStage = Math.Min(elapsedHours * 0.8 / stages, 60);

            for (int stage = 1; stage <= stages; stage++)
            {
                RequestStatus next = (RequestStatus)stage;

                if (next == RequestStatus.Assigned)
                {
                    request.AssignedTeam = ServiceDepartments.GetTeam(request.Category);
                }

                request.AddUpdate(next, request.SubmittedOn.AddHours(hoursPerStage * stage), DescribeStage(next, request));
            }
        }

        private static string DescribeStage(RequestStatus status, ServiceRequest request)
        {
            string team = string.IsNullOrEmpty(request.AssignedTeam)
                ? ServiceDepartments.GetTeam(request.Category)
                : request.AssignedTeam;

            switch (status)
            {
                case RequestStatus.Acknowledged:
                    return "Reviewed and passed to the " + request.Department + " department.";
                case RequestStatus.Assigned:
                    return "Assigned to the " + team + ".";
                case RequestStatus.InProgress:
                    return "The " + team + " is on site and work has started.";
                case RequestStatus.Resolved:
                    return "Work completed. Waiting for the resident to confirm the fix.";
                case RequestStatus.Closed:
                    return "Closed after the fix was confirmed.";
                default:
                    return string.Empty;
            }
        }

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        public string Depot { get; }

        public int Count
        {
            get { return _byReference.Count; }
        }

        public GeneralTree<CategoryTreeItem> DepartmentTree
        {
            get { return _departmentTree; }
        }

        public IEnumerable<SuburbLocation> Suburbs
        {
            get { return _suburbs.Values; }
        }

        public IReadOnlyList<GraphEdge<string>> Roads
        {
            get { return _roads.Edges; }
        }

        public SuburbLocation? GetSuburb(string name)
        {
            return _suburbs.TryGetValue(name ?? string.Empty, out SuburbLocation? suburb) ? suburb : null;
        }

        // ------------------------------------------------------------------
        // Adding data
        // ------------------------------------------------------------------

        public void AddSuburb(SuburbLocation suburb)
        {
            if (suburb == null) throw new ArgumentNullException(nameof(suburb));

            _suburbs[suburb.Name] = suburb;
            _roads.AddVertex(suburb.Name);
        }

        /// <summary>
        /// Joins two known suburbs with a road. Its length is the straight-line
        /// distance scaled up to allow for bends, rounded to 100 m.
        /// </summary>
        public void AddRoad(string from, string to)
        {
            SuburbLocation a = GetSuburb(from) ?? throw new ArgumentException("Unknown suburb: " + from, nameof(from));
            SuburbLocation b = GetSuburb(to) ?? throw new ArgumentException("Unknown suburb: " + to, nameof(to));

            double km = Math.Round(a.DistanceTo(b) * ServiceRequestSeedData.RoadDistanceFactor, 1);
            _roads.AddEdge(a.Name, b.Name, km);
        }

        /// <summary>
        /// Adds a request to every index.
        /// </summary>
        public void Add(ServiceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (!_byReference.Insert(request.ReferenceNumber, request))
            {
                throw new InvalidOperationException(
                    "A request with reference " + request.ReferenceNumber + " already exists.");
            }

            _byReferenceUnbalanced.Insert(request.ReferenceNumber, request);
            _bySubmission.Insert((request.SubmittedOn, request.ReferenceNumber), request);

            if (!_bySuburb.TryGetValue(request.Suburb, out List<ServiceRequest>? sameSuburb))
            {
                sameSuburb = new List<ServiceRequest>();
                _bySuburb.Insert(request.Suburb, sameSuburb);
            }
            sameSuburb.Add(request);

            GetCategoryNode(request.Category).AddChild(
                new CategoryTreeItem(request.ReferenceNumber + "  " + request.Title, CategoryTreeLevel.Request, request));

            if (request.IsWaitingForCrew)
            {
                _dispatchQueue.Insert(request);
            }

            _unblocks.AddVertex(request.ReferenceNumber);
            _waitsFor.AddVertex(request.ReferenceNumber);
        }

        private GeneralTreeNode<CategoryTreeItem> GetCategoryNode(string category)
        {
            if (_categoryNodes.TryGetValue(category, out GeneralTreeNode<CategoryTreeItem>? node))
            {
                return node;
            }

            // A category that is not in ServiceDepartments goes under its department
            // (General Services for anything unknown).
            string departmentName = ServiceDepartments.GetDepartment(category);
            GeneralTreeNode<CategoryTreeItem> departmentNode =
                _departmentTree.Root.FindChild(item => item.Label == departmentName)
                ?? _departmentTree.Root.AddChild(new CategoryTreeItem(departmentName, CategoryTreeLevel.Department));

            node = departmentNode.AddChild(new CategoryTreeItem(category, CategoryTreeLevel.Category));
            _categoryNodes[category] = node;
            return node;
        }

        /// <summary>
        /// Turns an issue from the Report Issues page into a tracked service request
        /// and returns it. Its reference number is the next one for this year.
        /// </summary>
        public ServiceRequest RegisterIssue(Issue issue)
        {
            if (issue == null) throw new ArgumentNullException(nameof(issue));

            DateTime now = _clock();
            string reference = NextReferenceNumber(now);

            string location = (issue.Location ?? string.Empty).Trim();
            string? suburb = FindSuburbIn(location);
            string street = location;

            if (suburb != null)
            {
                // "12 Church Street, Arcadia" -> street "12 Church Street", suburb "Arcadia".
                int index = location.IndexOf(suburb, StringComparison.OrdinalIgnoreCase);
                street = location.Remove(index, suburb.Length).Trim().Trim(',').Trim();
            }

            string note = "Reported on the Report Issues page.";
            if (issue.HasAttachment)
            {
                note += " A supporting file was attached.";
            }

            ServiceRequest request = new ServiceRequest(
                reference,
                MakeTitle(issue.Category, issue.Description),
                issue.Category ?? ServiceDepartments.Other,
                suburb ?? location,
                suburb != null ? street : string.Empty,
                issue.Description ?? string.Empty,
                ServiceDepartments.AssessPriority(issue.Category ?? string.Empty, issue.Description ?? string.Empty),
                now,
                note);

            request.ReportedThisSession = true;
            Add(request);
            RefreshTreeCounts();
            return request;
        }

        private string? FindSuburbIn(string location)
        {
            foreach (string name in _suburbs.Keys)
            {
                if (location.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return _suburbs[name].Name;
                }
            }
            return null;
        }

        private static string MakeTitle(string category, string description)
        {
            string text = (description ?? string.Empty).Trim();

            int sentenceEnd = text.IndexOfAny(new[] { '.', '!', '?', '\n' });
            if (sentenceEnd > 0) text = text.Substring(0, sentenceEnd);

            if (text.Length > 60) text = text.Substring(0, 57).TrimEnd() + "...";

            return text.Length > 0 ? text : (category ?? ServiceDepartments.Other) + " issue";
        }

        /// <summary>
        /// The next reference number for the year of the given date. The AVL tree
        /// keeps reference numbers in order, so a range query for that year's
        /// numbers returns the highest one last.
        /// </summary>
        public string NextReferenceNumber(DateTime when)
        {
            string prefix = string.Format(CultureInfo.InvariantCulture, "MSA-{0}-", when.Year);
            List<KeyValuePair<string, ServiceRequest>> thisYear = _byReference.Range(prefix + "0000", prefix + "9999");

            int highest = 0;
            if (thisYear.Count > 0)
            {
                string last = thisYear[thisYear.Count - 1].Key;
                int.TryParse(last.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out highest);
            }

            return prefix + (highest + 1).ToString("D4", CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------
        // Dependencies (directed graph)
        // ------------------------------------------------------------------

        /// <summary>
        /// Records that dependent cannot be worked on until prerequisite is resolved.
        /// Refused if it would create a cycle: if dependent already (directly or
        /// indirectly) unblocks prerequisite, each would be waiting for the other.
        /// </summary>
        public bool TryAddDependency(string prerequisite, string dependent, out string message)
        {
            if (!_byReference.ContainsKey(prerequisite) || !_byReference.ContainsKey(dependent))
            {
                message = "Both reference numbers must exist.";
                return false;
            }

            if (string.Equals(prerequisite, dependent, StringComparison.Ordinal))
            {
                message = "A request cannot depend on itself.";
                return false;
            }

            if (_unblocks.HasEdge(prerequisite, dependent))
            {
                message = dependent + " already waits for " + prerequisite + ".";
                return false;
            }

            if (GraphAlgorithms.HasPath(_unblocks, dependent, prerequisite))
            {
                message = string.Format(
                    "Refused: {0} already leads to {1}, so this link would create a cycle.",
                    dependent, prerequisite);
                return false;
            }

            _unblocks.AddEdge(prerequisite, dependent);
            _waitsFor.AddEdge(dependent, prerequisite);
            message = dependent + " now waits for " + prerequisite + ".";
            return true;
        }

        /// <summary>
        /// The unresolved requests this one is directly waiting for.
        /// </summary>
        public List<ServiceRequest> GetOpenPrerequisites(ServiceRequest request)
        {
            List<ServiceRequest> open = new List<ServiceRequest>();

            foreach (GraphEdge<string> edge in _waitsFor.GetEdges(request.ReferenceNumber))
            {
                if (_byReference.TryGetValue(edge.To, out ServiceRequest? prerequisite) && prerequisite.IsOpen)
                {
                    open.Add(prerequisite);
                }
            }

            return open;
        }

        /// <summary>
        /// An open request is blocked while anything it waits for is still open.
        /// </summary>
        public bool IsBlocked(ServiceRequest request)
        {
            return request.IsOpen && GetOpenPrerequisites(request).Count > 0;
        }

        public RelatedRequestsReport GetRelatedRequests(ServiceRequest request)
        {
            List<RelatedRequest> waitingOn = ToRelated(GraphAlgorithms.BreadthFirst(_waitsFor, request.ReferenceNumber));
            List<RelatedRequest> unblocks = ToRelated(GraphAlgorithms.BreadthFirst(_unblocks, request.ReferenceNumber));

            List<ServiceRequest> resolutionOrder = new List<ServiceRequest>();
            foreach (string reference in GraphAlgorithms.DependencyOrder(_waitsFor, request.ReferenceNumber))
            {
                if (_byReference.TryGetValue(reference, out ServiceRequest? item) && item.IsOpen)
                {
                    resolutionOrder.Add(item);
                }
            }

            return new RelatedRequestsReport(request, IsBlocked(request), waitingOn, unblocks, resolutionOrder);
        }

        private List<RelatedRequest> ToRelated(List<TraversalStep<string>> steps)
        {
            List<RelatedRequest> related = new List<RelatedRequest>();

            foreach (TraversalStep<string> step in steps)
            {
                if (step.Level == 0) continue; // the selected request itself

                if (_byReference.TryGetValue(step.Vertex, out ServiceRequest? item))
                {
                    related.Add(new RelatedRequest(item, step.Level, step.Parent ?? string.Empty));
                }
            }

            return related;
        }

        // ------------------------------------------------------------------
        // Lookups and lists (trees)
        // ------------------------------------------------------------------

        public ServiceRequest? Find(string reference)
        {
            return _byReference.TryGetValue(reference ?? string.Empty, out ServiceRequest? request) ? request : null;
        }

        /// <summary>
        /// Accepts "MSA-2026-0007", "msa 2026 7", "2026-7" or just "7" (this year).
        /// </summary>
        public string NormaliseReference(string input)
        {
            string cleaned = (input ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);

            Match match = ReferenceWithYear.Match(cleaned);
            if (match.Success)
            {
                int sequence = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                return string.Format(CultureInfo.InvariantCulture, "MSA-{0}-{1:D4}", match.Groups[1].Value, sequence);
            }

            if (SequenceOnly.IsMatch(cleaned))
            {
                int sequence = int.Parse(cleaned, CultureInfo.InvariantCulture);
                return string.Format(CultureInfo.InvariantCulture, "MSA-{0}-{1:D4}", _clock().Year, sequence);
            }

            return cleaned;
        }

        /// <summary>
        /// Looks up a reference number in the AVL tree and, for comparison, in the
        /// unbalanced BST that holds the same keys.
        /// </summary>
        public TrackingResult Track(string input)
        {
            string reference = NormaliseReference(input);

            _byReference.TryGetValue(reference, out ServiceRequest? request);
            int avlComparisons = _byReference.LastSearchComparisons;

            _byReferenceUnbalanced.TryGetValue(reference, out ServiceRequest? _);
            int bstComparisons = _byReferenceUnbalanced.LastSearchComparisons;

            return new TrackingResult(input ?? string.Empty, reference, request,
                avlComparisons, _byReference.Height, bstComparisons, _byReferenceUnbalanced.Height);
        }

        /// <summary>
        /// The requests matching a query, in the order it asks for. The order comes
        /// straight from a tree traversal, so no sorting is needed:
        ///   by reference  - in-order traversal of the AVL tree
        ///   oldest first  - in-order traversal (or range query) of the red-black tree
        ///   newest first  - reverse in-order traversal of the red-black tree
        /// </summary>
        public List<ServiceRequest> GetRequests(RequestQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            DateTime from = query.SubmittedFrom.HasValue ? query.SubmittedFrom.Value.Date : DateTime.MinValue;
            DateTime to = query.SubmittedTo.HasValue
                ? query.SubmittedTo.Value.Date.AddDays(1).AddTicks(-1)
                : DateTime.MaxValue;
            bool hasDateFilter = query.SubmittedFrom.HasValue || query.SubmittedTo.HasValue;

            List<ServiceRequest> ordered;

            if (query.SortOrder == RequestSortOrder.ReferenceNumber)
            {
                ordered = Values(_byReference.InOrder());
            }
            else if (hasDateFilter)
            {
                // Range query: only the part of the tree inside the dates is visited.
                ordered = Values(_bySubmission.Range((from, string.Empty), (to, "\uffff")));
                if (query.SortOrder == RequestSortOrder.NewestFirst)
                {
                    ordered.Reverse();
                }
            }
            else if (query.SortOrder == RequestSortOrder.NewestFirst)
            {
                ordered = Values(_bySubmission.ReverseInOrder());
            }
            else
            {
                ordered = Values(_bySubmission.InOrder());
            }

            HashSet<ServiceRequest>? inSuburbs = null;
            string prefix = (query.SuburbPrefix ?? string.Empty).Trim();
            if (prefix.Length > 0)
            {
                // Every suburb starting with the prefix sorts between "prefix" and
                // "prefix" followed by the highest possible character.
                inSuburbs = new HashSet<ServiceRequest>();
                foreach (KeyValuePair<string, List<ServiceRequest>> suburb in _bySuburb.Range(prefix, prefix + "\uffff"))
                {
                    inSuburbs.UnionWith(suburb.Value);
                }
            }

            HashSet<ServiceRequest>? inBranch = null;
            if (query.TreeNode != null && !query.TreeNode.IsRoot)
            {
                inBranch = new HashSet<ServiceRequest>();
                foreach (CategoryTreeItem item in _departmentTree.LeafValues(query.TreeNode))
                {
                    if (item.Request != null) inBranch.Add(item.Request);
                }
            }

            List<ServiceRequest> result = new List<ServiceRequest>();

            foreach (ServiceRequest request in ordered)
            {
                if (inSuburbs != null && !inSuburbs.Contains(request)) continue;
                if (inBranch != null && !inBranch.Contains(request)) continue;
                if (hasDateFilter && (request.SubmittedOn < from || request.SubmittedOn > to)) continue;
                if (!MatchesStatusFilter(request, query.StatusFilter)) continue;

                result.Add(request);
            }

            return result;
        }

        private bool MatchesStatusFilter(ServiceRequest request, RequestStatusFilter filter)
        {
            switch (filter)
            {
                case RequestStatusFilter.OpenOnly: return request.IsOpen;
                case RequestStatusFilter.WaitingForCrew: return request.IsWaitingForCrew;
                case RequestStatusFilter.Blocked: return IsBlocked(request);
                case RequestStatusFilter.ResolvedOrClosed: return !request.IsOpen;
                default: return true;
            }
        }

        private static List<ServiceRequest> Values<TKey>(List<KeyValuePair<TKey, ServiceRequest>> entries)
        {
            List<ServiceRequest> values = new List<ServiceRequest>(entries.Count);
            foreach (KeyValuePair<TKey, ServiceRequest> entry in entries)
            {
                values.Add(entry.Value);
            }
            return values;
        }

        /// <summary>
        /// Recalculates the open and total counts on every branch of the department
        /// tree. A post-order traversal visits every child before its parent, so
        /// each parent can simply add up its children's totals.
        /// </summary>
        public void RefreshTreeCounts()
        {
            foreach (GeneralTreeNode<CategoryTreeItem> node in _departmentTree.PostOrder())
            {
                CategoryTreeItem item = node.Value;

                if (item.Request != null)
                {
                    item.TotalCount = 1;
                    item.OpenCount = item.Request.IsOpen ? 1 : 0;
                    continue;
                }

                int total = 0;
                int open = 0;
                foreach (GeneralTreeNode<CategoryTreeItem> child in node.Children)
                {
                    total += child.Value.TotalCount;
                    open += child.Value.OpenCount;
                }

                item.TotalCount = total;
                item.OpenCount = open;
            }
        }

        // ------------------------------------------------------------------
        // Urgent queue (min-heap)
        // ------------------------------------------------------------------

        /// <summary>
        /// The most urgent requests still waiting for a crew, without removing them:
        /// items are taken from a copy of the heap. Requests that have moved on
        /// since they were queued are skipped (lazy deletion).
        /// </summary>
        public List<ServiceRequest> GetUrgentQueue(int count)
        {
            List<ServiceRequest> next = new List<ServiceRequest>();
            MinHeap<ServiceRequest> copy = _dispatchQueue.Clone();

            while (next.Count < count && copy.TryExtractMin(out ServiceRequest request))
            {
                if (request.IsWaitingForCrew)
                {
                    next.Add(request);
                }
            }

            return next;
        }

        public int CountWaitingForCrew()
        {
            int waiting = 0;
            foreach (ServiceRequest request in _dispatchQueue.Items)
            {
                if (request.IsWaitingForCrew) waiting++;
            }
            return waiting;
        }

        /// <summary>
        /// Takes the most urgent request that can be worked on now off the heap and
        /// assigns it to its department's team. Blocked requests are set aside while
        /// searching and pushed back afterwards, so they keep their place in line.
        /// Returns null if nothing is waiting.
        /// </summary>
        public ServiceRequest? DispatchNextJob()
        {
            List<ServiceRequest> setAside = new List<ServiceRequest>();
            ServiceRequest? dispatched = null;

            while (_dispatchQueue.TryExtractMin(out ServiceRequest candidate))
            {
                if (!candidate.IsWaitingForCrew)
                {
                    continue; // stale entry: already moved on, drop it
                }

                if (IsBlocked(candidate))
                {
                    setAside.Add(candidate);
                    continue;
                }

                dispatched = candidate;
                break;
            }

            foreach (ServiceRequest request in setAside)
            {
                _dispatchQueue.Insert(request);
            }

            if (dispatched == null)
            {
                return null;
            }

            DateTime now = _clock();
            if (dispatched.Status == RequestStatus.Submitted)
            {
                dispatched.AddUpdate(RequestStatus.Acknowledged, now, DescribeStage(RequestStatus.Acknowledged, dispatched));
            }

            dispatched.AssignedTeam = ServiceDepartments.GetTeam(dispatched.Category);
            dispatched.AddUpdate(RequestStatus.Assigned, now,
                "Dispatched from the urgent queue to the " + dispatched.AssignedTeam + ".");

            RefreshTreeCounts();
            return dispatched;
        }

        /// <summary>
        /// Moves a request to its next stage. Work cannot start on a blocked request.
        /// </summary>
        public StatusChangeResult AdvanceStatus(ServiceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            RequestStatus? next = RequestStatusInfo.GetNext(request.Status);
            if (next == null)
            {
                return new StatusChangeResult(false, request.ReferenceNumber + " is already closed.");
            }

            if (next.Value == RequestStatus.InProgress)
            {
                List<ServiceRequest> blockers = GetOpenPrerequisites(request);
                if (blockers.Count > 0)
                {
                    List<string> references = new List<string>();
                    foreach (ServiceRequest blocker in blockers) references.Add(blocker.ReferenceNumber);

                    return new StatusChangeResult(false, string.Format(
                        "Work on {0} cannot start yet. It is waiting for {1} to be resolved.",
                        request.ReferenceNumber, string.Join(" and ", references)));
                }
            }

            if (next.Value == RequestStatus.Assigned && string.IsNullOrEmpty(request.AssignedTeam))
            {
                request.AssignedTeam = ServiceDepartments.GetTeam(request.Category);
            }

            request.AddUpdate(next.Value, _clock(), DescribeStage(next.Value, request));

            string message = request.ReferenceNumber + " moved to " +
                             RequestStatusInfo.GetDisplayName(next.Value) + ".";

            if (next.Value == RequestStatus.Resolved)
            {
                List<string> freed = new List<string>();
                foreach (GraphEdge<string> edge in _unblocks.GetEdges(request.ReferenceNumber))
                {
                    ServiceRequest? dependent = Find(edge.To);
                    if (dependent != null && dependent.IsOpen && !IsBlocked(dependent))
                    {
                        freed.Add(dependent.ReferenceNumber);
                    }
                }

                if (freed.Count > 0)
                {
                    message += " This unblocks " + string.Join(", ", freed) + ".";
                }
            }

            RefreshTreeCounts();
            return new StatusChangeResult(true, message);
        }

        // ------------------------------------------------------------------
        // Crew route (weighted graph, Dijkstra, Prim, DFS)
        // ------------------------------------------------------------------

        /// <summary>
        /// Plans how a crew from the depot can reach every suburb with an open
        /// request for one department (or all departments when null):
        ///
        ///   1. Dijkstra's algorithm finds the shortest road distance between every
        ///      pair of sites (the depot and each suburb with open work).
        ///   2. Those distances form a complete graph of the sites, and Prim's
        ///      algorithm finds its minimum spanning tree: the cheapest set of
        ///      links that connects them all.
        ///   3. A depth-first walk of that tree from the depot gives a visiting
        ///      order (at most twice the length of the tree, a classic bound).
        /// </summary>
        public CrewRoutePlan PlanCrewRoute(string? department)
        {
            string label = string.IsNullOrEmpty(department) ? "All departments" : department!;

            Dictionary<string, int> openBySuburb = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, ServiceRequest> entry in _byReference.InOrder())
            {
                ServiceRequest request = entry.Value;
                if (!request.IsOpen) continue;
                if (!string.IsNullOrEmpty(department) &&
                    !string.Equals(request.Department, department, StringComparison.OrdinalIgnoreCase)) continue;

                SuburbLocation? suburb = GetSuburb(request.Suburb);
                if (suburb == null) continue; // not on the road network

                openBySuburb.TryGetValue(suburb.Name, out int count);
                openBySuburb[suburb.Name] = count + 1;
            }

            List<string> sites = new List<string>();
            foreach (string suburb in openBySuburb.Keys)
            {
                if (!string.Equals(suburb, Depot, StringComparison.OrdinalIgnoreCase)) sites.Add(suburb);
            }
            sites.Sort(StringComparer.OrdinalIgnoreCase);

            List<string> terminals = new List<string> { Depot };
            terminals.AddRange(sites);

            if (sites.Count == 0)
            {
                return new CrewRoutePlan(label, Depot, sites, openBySuburb, new List<RouteLeg>(), 0,
                    new List<string> { Depot }, 0);
            }

            // 1. Shortest paths from every site.
            Dictionary<string, ShortestPathResult<string>> shortest =
                new Dictionary<string, ShortestPathResult<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string terminal in terminals)
            {
                shortest[terminal] = GraphAlgorithms.ShortestPaths(_roads, terminal);
            }

            // 2. Complete graph of the sites, then its minimum spanning tree.
            Graph<string> siteGraph = new Graph<string>(false, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < terminals.Count; i++)
            {
                siteGraph.AddVertex(terminals[i]);
                for (int j = i + 1; j < terminals.Count; j++)
                {
                    if (shortest[terminals[i]].Distances.TryGetValue(terminals[j], out double km))
                    {
                        siteGraph.AddEdge(terminals[i], terminals[j], Math.Round(km, 1));
                    }
                }
            }

            SpanningTreeResult<string> tree = GraphAlgorithms.PrimMinimumSpanningTree(siteGraph, Depot);

            List<RouteLeg> legs = new List<RouteLeg>();
            Graph<string> treeGraph = new Graph<string>(false, StringComparer.OrdinalIgnoreCase);
            treeGraph.AddVertex(Depot);

            foreach (GraphEdge<string> edge in tree.Edges)
            {
                legs.Add(new RouteLeg(edge.From, edge.To, edge.Weight, shortest[edge.From].PathTo(edge.To)));
                treeGraph.AddEdge(edge.From, edge.To, edge.Weight);
            }

            // 3. Depth-first walk of the tree for a visiting order, back to the depot.
            List<string> visitOrder = GraphAlgorithms.DepthFirst(treeGraph, Depot);
            visitOrder.Add(Depot);

            double tourKm = 0;
            for (int i = 1; i < visitOrder.Count; i++)
            {
                if (shortest.TryGetValue(visitOrder[i - 1], out ShortestPathResult<string>? paths) &&
                    paths.Distances.TryGetValue(visitOrder[i], out double leg))
                {
                    tourKm += leg;
                }
            }

            return new CrewRoutePlan(label, Depot, sites, openBySuburb, legs,
                Math.Round(tree.TotalWeight, 1), visitOrder, Math.Round(tourKm, 1));
        }

        // ------------------------------------------------------------------
        // Summaries and self-checks
        // ------------------------------------------------------------------

        public int CountOpen()
        {
            int open = 0;
            foreach (KeyValuePair<string, ServiceRequest> entry in _byReference.InOrder())
            {
                if (entry.Value.IsOpen) open++;
            }
            return open;
        }

        public int CountBlocked()
        {
            int blocked = 0;
            foreach (KeyValuePair<string, ServiceRequest> entry in _byReference.InOrder())
            {
                if (IsBlocked(entry.Value)) blocked++;
            }
            return blocked;
        }

        /// <summary>
        /// One line per data structure for the "Under the hood" tab.
        /// </summary>
        public List<StructureSummary> GetStructureSummaries()
        {
            List<StructureSummary> rows = new List<StructureSummary>();

            rows.Add(new StructureSummary(
                "AVL tree",
                "Track by reference number; list in reference order",
                _byReference.Count + " requests",
                string.Format("Height {0} (best possible {1}); {2} rotations so far",
                    _byReference.Height, _byReference.MinimumPossibleHeight, _byReference.RotationCount)));

            rows.Add(new StructureSummary(
                "Red-black tree",
                "List by date submitted; date-range filter",
                _bySubmission.Count + " requests",
                string.Format("Height {0}; black height {1}; {2} rotations, {3} recolourings",
                    _bySubmission.Height, _bySubmission.BlackHeight,
                    _bySubmission.RotationCount, _bySubmission.RecolourCount)));

            rows.Add(new StructureSummary(
                "Binary search tree",
                "\"Suburb starts with\" search (range query)",
                _bySuburb.Count + " suburbs",
                string.Format("Height {0} (best possible {1})", _bySuburb.Height, _bySuburb.MinimumPossibleHeight)));

            rows.Add(new StructureSummary(
                "Binary search tree (unbalanced)",
                "Same reference numbers with no balancing, for comparison",
                _byReferenceUnbalanced.Count + " requests",
                string.Format("Height {0}: sequential numbers turned it into a chain", _byReferenceUnbalanced.Height)));

            rows.Add(new StructureSummary(
                "Binary tree (base class)",
                "Traversals shared by all three search trees",
                "-",
                "AVL nodes per level: " + string.Join(", ", _byReference.NodesPerLevel())));

            int departments = _departmentTree.Root.Children.Count;
            rows.Add(new StructureSummary(
                "General tree",
                "Departments > categories > requests",
                _departmentTree.Count + " nodes",
                string.Format("Height {0}; {1} departments, {2} categories",
                    _departmentTree.Height, departments, _categoryNodes.Count)));

            string top = _dispatchQueue.IsEmpty ? "empty" : _dispatchQueue.Peek().ReferenceNumber;
            rows.Add(new StructureSummary(
                "Min-heap",
                "Requests waiting for a crew, most urgent first",
                CountWaitingForCrew() + " waiting",
                string.Format("Array of {0} entries; root (next job) {1}", _dispatchQueue.Count, top)));

            rows.Add(new StructureSummary(
                "Directed graph",
                "Which requests must be resolved before others",
                _unblocks.VertexCount + " vertices, " + _unblocks.EdgeCount + " edges",
                CountBlocked() + " open requests blocked; cycles refused"));

            double totalRoadKm = 0;
            foreach (GraphEdge<string> road in _roads.Edges) totalRoadKm += road.Weight;
            SpanningTreeResult<string> roadTree = GraphAlgorithms.PrimMinimumSpanningTree(_roads, Depot);

            rows.Add(new StructureSummary(
                "Weighted graph",
                "Roads between suburbs: Dijkstra, Prim's MST, DFS route",
                _roads.VertexCount + " suburbs, " + _roads.EdgeCount + " roads",
                string.Format("All roads {0:0.0} km; minimum spanning tree {1:0.0} km",
                    totalRoadKm, roadTree.TotalWeight)));

            return rows;
        }

        /// <summary>
        /// Checks that every structure is in a valid state and that they all agree.
        /// Shown on the "Under the hood" tab.
        /// </summary>
        public List<SelfCheckResult> RunSelfChecks()
        {
            List<SelfCheckResult> results = new List<SelfCheckResult>();
            string message;

            results.Add(new SelfCheckResult("AVL tree is ordered and balanced",
                _byReference.Validate(out message), message));

            results.Add(new SelfCheckResult("Red-black tree keeps all colour rules",
                _bySubmission.Validate(out message), message));

            results.Add(new SelfCheckResult("Suburb binary search tree is ordered",
                _bySuburb.Validate(out message), message));

            results.Add(new SelfCheckResult("Unbalanced binary search tree is ordered",
                _byReferenceUnbalanced.Validate(out message), message));

            // Every index must hold the same requests.
            int suburbTotal = 0;
            foreach (KeyValuePair<string, List<ServiceRequest>> entry in _bySuburb.InOrder())
            {
                suburbTotal += entry.Value.Count;
            }

            int leafRequests = 0;
            foreach (CategoryTreeItem item in _departmentTree.LeafValues())
            {
                if (item.Request != null) leafRequests++;
            }

            bool sameCounts = _byReference.Count == _bySubmission.Count &&
                              _byReference.Count == _byReferenceUnbalanced.Count &&
                              _byReference.Count == suburbTotal &&
                              _byReference.Count == leafRequests;

            results.Add(new SelfCheckResult("All indexes hold the same requests", sameCounts,
                string.Format("AVL {0}, red-black {1}, BST {2}, suburb lists {3}, tree leaves {4}.",
                    _byReference.Count, _bySubmission.Count, _byReferenceUnbalanced.Count, suburbTotal, leafRequests)));

            RefreshTreeCounts();
            results.Add(new SelfCheckResult("Department tree totals add up",
                _departmentTree.Root.Value.TotalCount == _byReference.Count,
                string.Format("Root total {0}, open {1}.",
                    _departmentTree.Root.Value.TotalCount, _departmentTree.Root.Value.OpenCount)));

            // Every reference number can be found in at most "height" comparisons.
            int worst = 0;
            bool allFound = true;
            foreach (KeyValuePair<string, ServiceRequest> entry in _byReference.InOrder())
            {
                allFound &= _byReference.ContainsKey(entry.Key);
                worst = Math.Max(worst, _byReference.LastSearchComparisons);
            }
            results.Add(new SelfCheckResult("Every request can be tracked by reference",
                allFound && worst <= _byReference.Height,
                string.Format("Worst case {0} comparisons for {1} requests (tree height {2}).",
                    worst, _byReference.Count, _byReference.Height)));

            results.Add(new SelfCheckResult("Min-heap property holds",
                _dispatchQueue.IsValid(),
                "Every parent is at least as urgent as its children."));

            List<string>? order = GraphAlgorithms.TopologicalSort(_unblocks);
            results.Add(new SelfCheckResult("Dependency graph has no cycles",
                order != null,
                order != null
                    ? "Topological sort ordered all " + order.Count + " requests."
                    : "A cycle was found."));

            if (_unblocks.EdgeCount > 0)
            {
                GraphEdge<string> existing = _unblocks.Edges[0];
                bool refused = !TryAddDependency(existing.To, existing.From, out string reason);
                results.Add(new SelfCheckResult("A link that would create a cycle is refused", refused, reason));
            }

            List<TraversalStep<string>> reachable = GraphAlgorithms.BreadthFirst(_roads, Depot);
            results.Add(new SelfCheckResult("Every suburb can be reached from the depot",
                reachable.Count == _roads.VertexCount,
                string.Format("Breadth-first search reached {0} of {1} suburbs.", reachable.Count, _roads.VertexCount)));

            SpanningTreeResult<string> prim = GraphAlgorithms.PrimMinimumSpanningTree(_roads, Depot);
            SpanningTreeResult<string> kruskal = GraphAlgorithms.KruskalMinimumSpanningTree(_roads);
            results.Add(new SelfCheckResult("Prim's and Kruskal's algorithms agree",
                Math.Abs(prim.TotalWeight - kruskal.TotalWeight) < 0.001 &&
                prim.Edges.Count == _roads.VertexCount - 1,
                string.Format("Both found a {0:0.0} km tree with {1} roads.", prim.TotalWeight, prim.Edges.Count)));

            return results;
        }
    }
}
