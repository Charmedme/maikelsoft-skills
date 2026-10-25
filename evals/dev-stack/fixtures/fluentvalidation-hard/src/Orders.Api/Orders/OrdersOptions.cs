using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Orders;

public sealed class OrdersOptions
{
    public const string SectionName = "Orders";

    [Range(1, 500)]
    public int MaxLinesPerOrder { get; set; } = 50;

    [Range(1, 720)]
    public int CancelWindowHours { get; set; } = 24;

    [Range(1, 200)]
    public int ListLimit { get; set; } = 50;

    public TimeSpan CancelWindow => TimeSpan.FromHours(CancelWindowHours);
}
