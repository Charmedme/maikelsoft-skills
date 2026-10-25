using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Picking;

public sealed class PickingOptions
{
    public const string SectionName = "Picking";

    /// <summary>An order that is in Picking for longer than this goes back to the queue.</summary>
    [Range(10, 1440)]
    public int TimeoutMinutes { get; set; } = 120;

    public bool TimeoutJobEnabled { get; set; } = true;
}
