namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// How urgent a service request is. Lower values are more urgent, so the
    /// dispatch min-heap puts Critical requests at the top.
    /// </summary>
    public enum RequestPriority
    {
        Critical = 0,
        High = 1,
        Medium = 2,
        Low = 3
    }
}
