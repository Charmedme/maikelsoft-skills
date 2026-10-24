namespace Orders.Api.Picking;

// The zone of the warehouse where a SKU is stored. The warehouse system sends changes.
public sealed class PickZone
{
    public const string Default = "MAIN";

    private PickZone()
    {
        Sku = string.Empty;
        Zone = string.Empty;
    }

    public string Sku { get; private set; }

    public string Zone { get; private set; }
}
