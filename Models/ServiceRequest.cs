using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// A service request: a reported problem that the municipality tracks from
    /// submission until it is closed. Issues reported on the Report Issues page
    /// become service requests with the same reference number.
    /// </summary>
    public class ServiceRequest
    {
        private readonly List<StatusUpdate> _history = new List<StatusUpdate>();

        public ServiceRequest(
            string referenceNumber,
            string title,
            string category,
            string suburb,
            string street,
            string description,
            RequestPriority priority,
            DateTime submittedOn,
            string submissionNote)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                throw new ArgumentException("A reference number is required.", nameof(referenceNumber));
            }

            ReferenceNumber = referenceNumber;
            Title = title ?? string.Empty;
            Category = category ?? ServiceDepartments.Other;
            Department = ServiceDepartments.GetDepartment(Category);
            Suburb = suburb ?? string.Empty;
            Street = street ?? string.Empty;
            Description = description ?? string.Empty;
            Priority = priority;
            SubmittedOn = submittedOn;
            AssignedTeam = string.Empty;

            _history.Add(new StatusUpdate(RequestStatus.Submitted, submittedOn, submissionNote));
        }

        /// <summary>
        /// Unique identifier in the form MSA-YYYY-NNNN.
        /// </summary>
        public string ReferenceNumber { get; }

        public string Title { get; }

        /// <summary>
        /// The category chosen on the Report Issues page, e.g. "Water and Sewerage".
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// The department responsible for the category, e.g. "Water and Sanitation".
        /// </summary>
        public string Department { get; }

        public string Suburb { get; }

        public string Street { get; }

        public string Description { get; }

        public RequestPriority Priority { get; }

        public DateTime SubmittedOn { get; }

        public string AssignedTeam { get; set; }

        /// <summary>
        /// True when the request came from the Report Issues page in this session.
        /// </summary>
        public bool ReportedThisSession { get; set; }

        public IReadOnlyList<StatusUpdate> History
        {
            get { return _history; }
        }

        public RequestStatus Status
        {
            get { return _history[_history.Count - 1].Status; }
        }

        public DateTime LastUpdated
        {
            get { return _history[_history.Count - 1].Timestamp; }
        }

        public bool IsOpen
        {
            get { return RequestStatusInfo.IsOpen(Status); }
        }

        public bool IsWaitingForCrew
        {
            get { return RequestStatusInfo.IsWaitingForCrew(Status); }
        }

        public string Location
        {
            get
            {
                if (string.IsNullOrEmpty(Street)) return Suburb;
                if (string.IsNullOrEmpty(Suburb)) return Street;
                return Street + ", " + Suburb;
            }
        }

        /// <summary>
        /// Whole days since the request was submitted.
        /// </summary>
        public int DaysOpen(DateTime now)
        {
            DateTime end = IsOpen ? now : LastUpdated;
            return Math.Max(0, (int)(end.Date - SubmittedOn.Date).TotalDays);
        }

        /// <summary>
        /// The time the request reached a stage, or null if it has not reached it.
        /// </summary>
        public DateTime? GetStageDate(RequestStatus stage)
        {
            foreach (StatusUpdate update in _history)
            {
                if (update.Status == stage) return update.Timestamp;
            }
            return null;
        }

        /// <summary>
        /// Records a move to a later stage. Stages cannot be skipped or repeated.
        /// </summary>
        public void AddUpdate(RequestStatus status, DateTime timestamp, string note)
        {
            if (status != RequestStatusInfo.GetNext(Status))
            {
                throw new InvalidOperationException(string.Format(
                    "{0} cannot move from {1} to {2}.",
                    ReferenceNumber,
                    RequestStatusInfo.GetDisplayName(Status),
                    RequestStatusInfo.GetDisplayName(status)));
            }

            if (timestamp < LastUpdated)
            {
                timestamp = LastUpdated;
            }

            _history.Add(new StatusUpdate(status, timestamp, note));
        }

        public override string ToString()
        {
            return ReferenceNumber + " - " + Title;
        }
    }
}
