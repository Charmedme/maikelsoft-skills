namespace Shop;

public interface IOrderStore
{
    Task<Order?> FindAsync(int id, CancellationToken cancellationToken);

    Task SaveAsync(Order order, CancellationToken cancellationToken);
}

public sealed record Order(int Id, decimal Total, DateTimeOffset? ClosedAt, IReadOnlyList<string> Lines);

public sealed class OrderService(IOrderStore store, TimeProvider time)
{
    public async Task<Order> CloseAsync(int id, CancellationToken cancellationToken)
    {
        Order order = await store.FindAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {id} does not exist.");
        if (order.ClosedAt is not null)
        {
            throw new InvalidOperationException($"Order {id} is already closed.");
        }

        Order closed = order with { ClosedAt = time.GetUtcNow() };
        await store.SaveAsync(closed, cancellationToken);
        return closed;
    }
}

public sealed class Reservation(DateTimeOffset createdAt, TimeProvider time)
{
    public static readonly TimeSpan HoldTime = TimeSpan.FromMinutes(15);

    public bool IsExpired => time.GetUtcNow() - createdAt >= HoldTime;
}

public static class Discount
{
    public static decimal For(decimal total) => total switch
    {
        < 0m => throw new ArgumentOutOfRangeException(nameof(total), "Total cannot be negative."),
        >= 1000m => total * 0.10m,
        >= 100m => total * 0.05m,
        _ => 0m,
    };
}

public sealed class Money(decimal amount, string currency, string note) : IEquatable<Money>
{
    public decimal Amount { get; } = amount;

    public string Currency { get; } = currency;

    public string Note { get; } = note;

    public bool Equals(Money? other) => other is not null && Amount == other.Amount && Currency == other.Currency && Note == other.Note;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency, Note);
}
