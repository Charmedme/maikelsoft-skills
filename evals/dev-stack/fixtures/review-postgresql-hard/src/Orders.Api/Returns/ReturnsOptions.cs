using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Returns;

public sealed class ReturnsOptions
{
    public const string SectionName = "Returns";

    /// <summary>Days after the shipment in which the customer can ask for a return.</summary>
    [Range(1, 365)]
    public int WindowDays { get; set; } = 30;

    /// <summary>Days after the request in which the parcel must arrive. After that the job expires the request.</summary>
    [Range(1, 365)]
    public int ExpiryDays { get; set; } = 14;

    public bool ExpiryJobEnabled { get; set; } = true;

    [Range(1, 1440)]
    public int ExpiryJobIntervalMinutes { get; set; } = 60;
}
