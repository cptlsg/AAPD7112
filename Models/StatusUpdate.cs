using System;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// One entry in a service request's history: the stage it reached, when, and a note.
    /// </summary>
    public class StatusUpdate
    {
        public StatusUpdate(RequestStatus status, DateTime timestamp, string note)
        {
            Status = status;
            Timestamp = timestamp;
            Note = note ?? string.Empty;
        }

        public RequestStatus Status { get; }

        public DateTime Timestamp { get; }

        public string Note { get; }
    }
}
