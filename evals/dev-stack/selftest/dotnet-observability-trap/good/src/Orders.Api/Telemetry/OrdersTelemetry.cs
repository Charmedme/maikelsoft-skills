using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.Api.Telemetry;

public sealed class OrdersTelemetry
{
    public const string Name = "Orders.Api";

    public static readonly ActivitySource Source = new(Name);

    private readonly Counter<long> _created;

    public OrdersTelemetry(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Name);
        _created = meter.CreateCounter<long>("orders.created", unit: "{order}", description: "Orders created.");
    }

    public void OrderCreated() => _created.Add(1);
}
