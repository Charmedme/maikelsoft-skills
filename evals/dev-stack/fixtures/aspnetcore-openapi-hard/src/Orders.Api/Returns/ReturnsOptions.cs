using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Returns;

public sealed class ReturnsOptions
{
    public const string SectionName = "Returns";

    [Range(1, 365)]
    public int ReturnWindowDays { get; set; } = 30;

    public TimeSpan ReturnWindow => TimeSpan.FromDays(ReturnWindowDays);
}
