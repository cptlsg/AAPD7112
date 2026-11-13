using System;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// The stages a service request moves through, in order. The numeric value is
    /// the stage's position, so later stages compare as greater.
    /// </summary>
    public enum RequestStatus
    {
        Submitted = 0,
        Acknowledged = 1,
        Assigned = 2,
        InProgress = 3,
        Resolved = 4,
        Closed = 5
    }

    /// <summary>
    /// Display names and stage helpers for RequestStatus.
    /// </summary>
    public static class RequestStatusInfo
    {
        public static readonly RequestStatus[] Stages =
        {
            RequestStatus.Submitted,
            RequestStatus.Acknowledged,
            RequestStatus.Assigned,
            RequestStatus.InProgress,
            RequestStatus.Resolved,
            RequestStatus.Closed
        };

        public static string GetDisplayName(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Submitted: return "Submitted";
                case RequestStatus.Acknowledged: return "Acknowledged";
                case RequestStatus.Assigned: return "Assigned";
                case RequestStatus.InProgress: return "In progress";
                case RequestStatus.Resolved: return "Resolved";
                case RequestStatus.Closed: return "Closed";
                default: return status.ToString();
            }
        }

        /// <summary>
        /// One sentence telling a resident what the stage means for them.
        /// </summary>
        public static string GetExplanation(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Submitted:
                    return "Your request has been received and is waiting to be reviewed.";
                case RequestStatus.Acknowledged:
                    return "The responsible department has reviewed your request and it is waiting for a team.";
                case RequestStatus.Assigned:
                    return "A field team has been assigned and will schedule the work.";
                case RequestStatus.InProgress:
                    return "The team is working on the problem.";
                case RequestStatus.Resolved:
                    return "The work is complete. The request will be closed once the fix is confirmed.";
                case RequestStatus.Closed:
                    return "This request is closed.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// The next stage, or null if the request is already closed.
        /// </summary>
        public static RequestStatus? GetNext(RequestStatus status)
        {
            if (status == RequestStatus.Closed) return null;
            return (RequestStatus)((int)status + 1);
        }

        public static bool IsOpen(RequestStatus status)
        {
            return status < RequestStatus.Resolved;
        }

        /// <summary>
        /// Submitted and acknowledged requests are waiting for a crew to be dispatched.
        /// </summary>
        public static bool IsWaitingForCrew(RequestStatus status)
        {
            return status == RequestStatus.Submitted || status == RequestStatus.Acknowledged;
        }
    }
}
