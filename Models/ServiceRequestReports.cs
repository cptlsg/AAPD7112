using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// What each node in the department tree holds. Branches (the municipality,
    /// departments and categories) carry counts; leaves carry one request.
    /// </summary>
    public class CategoryTreeItem
    {
        public CategoryTreeItem(string label, CategoryTreeLevel level, ServiceRequest? request = null)
        {
            Label = label;
            Level = level;
            Request = request;
        }

        public string Label { get; }

        public CategoryTreeLevel Level { get; }

        public ServiceRequest? Request { get; }

        /// <summary>
        /// Requests in this subtree. Filled in by a post-order traversal.
        /// </summary>
        public int TotalCount { get; set; }

        public int OpenCount { get; set; }
    }

    public enum CategoryTreeLevel
    {
        Municipality,
        Department,
        Category,
        Request
    }

    /// <summary>
    /// How the request list should be ordered.
    /// </summary>
    public enum RequestSortOrder
    {
        NewestFirst,
        OldestFirst,
        ReferenceNumber
    }

    /// <summary>
    /// Which requests to include in the list, by status.
    /// </summary>
    public enum RequestStatusFilter
    {
        All,
        OpenOnly,
        WaitingForCrew,
        Blocked,
        ResolvedOrClosed
    }

    /// <summary>
    /// The filters for the request list. Any filter left null is not applied.
    /// </summary>
    public class RequestQuery
    {
        public RequestSortOrder SortOrder { get; set; } = RequestSortOrder.NewestFirst;

        public RequestStatusFilter StatusFilter { get; set; } = RequestStatusFilter.All;

        public string? SuburbPrefix { get; set; }

        public DateTime? SubmittedFrom { get; set; }

        public DateTime? SubmittedTo { get; set; }

        /// <summary>
        /// Only requests below this node of the department tree.
        /// </summary>
        public DataStructures.GeneralTreeNode<CategoryTreeItem>? TreeNode { get; set; }
    }

    /// <summary>
    /// The outcome of looking up a reference number, with the cost of the search
    /// in the AVL tree and, for comparison, in a plain binary search tree.
    /// </summary>
    public class TrackingResult
    {
        public TrackingResult(string input, string normalisedReference, ServiceRequest? request,
            int avlComparisons, int avlHeight, int plainBstComparisons, int plainBstHeight)
        {
            Input = input;
            NormalisedReference = normalisedReference;
            Request = request;
            AvlComparisons = avlComparisons;
            AvlHeight = avlHeight;
            PlainBstComparisons = plainBstComparisons;
            PlainBstHeight = plainBstHeight;
        }

        public string Input { get; }

        public string NormalisedReference { get; }

        public ServiceRequest? Request { get; }

        public bool Found
        {
            get { return Request != null; }
        }

        public int AvlComparisons { get; }

        public int AvlHeight { get; }

        public int PlainBstComparisons { get; }

        public int PlainBstHeight { get; }
    }

    /// <summary>
    /// A request related to the selected one through the dependency graph.
    /// </summary>
    public class RelatedRequest
    {
        public RelatedRequest(ServiceRequest request, int level, string viaReference)
        {
            Request = request;
            Level = level;
            ViaReference = viaReference;
        }

        public ServiceRequest Request { get; }

        /// <summary>
        /// Edges away from the selected request found by breadth-first search
        /// (1 = directly related).
        /// </summary>
        public int Level { get; }

        /// <summary>
        /// The request it was reached through.
        /// </summary>
        public string ViaReference { get; }
    }

    /// <summary>
    /// Everything the dependency graph says about one request.
    /// </summary>
    public class RelatedRequestsReport
    {
        public RelatedRequestsReport(ServiceRequest request, bool isBlocked,
            List<RelatedRequest> waitingOn, List<RelatedRequest> unblocks, List<ServiceRequest> resolutionOrder)
        {
            Request = request;
            IsBlocked = isBlocked;
            WaitingOn = waitingOn;
            Unblocks = unblocks;
            ResolutionOrder = resolutionOrder;
        }

        public ServiceRequest Request { get; }

        public bool IsBlocked { get; }

        /// <summary>
        /// Requests that must be resolved before this one can proceed (BFS over prerequisites).
        /// </summary>
        public List<RelatedRequest> WaitingOn { get; }

        /// <summary>
        /// Requests that are waiting for this one (BFS over dependents).
        /// </summary>
        public List<RelatedRequest> Unblocks { get; }

        /// <summary>
        /// Open requests to resolve, in order, so that this one can be completed (DFS post-order).
        /// </summary>
        public List<ServiceRequest> ResolutionOrder { get; }

        public bool HasRelationships
        {
            get { return WaitingOn.Count > 0 || Unblocks.Count > 0; }
        }
    }

    /// <summary>
    /// One link in the crew route: the cheapest connection between two sites,
    /// following the shortest road path between them.
    /// </summary>
    public class RouteLeg
    {
        public RouteLeg(string from, string to, double distanceKm, List<string> roadPath)
        {
            From = from;
            To = to;
            DistanceKm = distanceKm;
            RoadPath = roadPath;
        }

        public string From { get; }

        public string To { get; }

        public double DistanceKm { get; }

        /// <summary>
        /// Every suburb on the road between From and To, inclusive.
        /// </summary>
        public List<string> RoadPath { get; }
    }

    /// <summary>
    /// The crew route for one department (or all of them): the minimum spanning
    /// tree linking the depot to every suburb with an open request, and a
    /// visiting order taken from a depth-first walk of that tree.
    /// </summary>
    public class CrewRoutePlan
    {
        public CrewRoutePlan(string department, string depot, List<string> sites,
            Dictionary<string, int> openRequestsBySuburb, List<RouteLeg> legs,
            double networkKm, List<string> visitOrder, double tourKm)
        {
            Department = department;
            Depot = depot;
            Sites = sites;
            OpenRequestsBySuburb = openRequestsBySuburb;
            Legs = legs;
            NetworkKm = networkKm;
            VisitOrder = visitOrder;
            TourKm = tourKm;
        }

        public string Department { get; }

        public string Depot { get; }

        /// <summary>
        /// Suburbs with at least one open request (the depot is not included).
        /// </summary>
        public List<string> Sites { get; }

        public Dictionary<string, int> OpenRequestsBySuburb { get; }

        public List<RouteLeg> Legs { get; }

        /// <summary>
        /// Total length of the spanning tree's legs.
        /// </summary>
        public double NetworkKm { get; }

        /// <summary>
        /// Depot first, then each site in depth-first order, ending back at the depot.
        /// </summary>
        public List<string> VisitOrder { get; }

        /// <summary>
        /// Length of driving the visit order and returning to the depot.
        /// </summary>
        public double TourKm { get; }
    }

    /// <summary>
    /// A row on the "Under the hood" tab describing one data structure.
    /// </summary>
    public class StructureSummary
    {
        public StructureSummary(string structure, string purpose, string size, string shape)
        {
            Structure = structure;
            Purpose = purpose;
            Size = size;
            Shape = shape;
        }

        public string Structure { get; }

        public string Purpose { get; }

        public string Size { get; }

        public string Shape { get; }
    }

    /// <summary>
    /// The result of one automatic correctness check.
    /// </summary>
    public class SelfCheckResult
    {
        public SelfCheckResult(string check, bool passed, string detail)
        {
            Check = check;
            Passed = passed;
            Detail = detail;
        }

        public string Check { get; }

        public bool Passed { get; }

        public string Detail { get; }
    }

    /// <summary>
    /// The outcome of trying to move a request to its next stage.
    /// </summary>
    public class StatusChangeResult
    {
        public StatusChangeResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }

        public string Message { get; }
    }
}
