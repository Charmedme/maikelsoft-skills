namespace Shop.Tests;

public sealed class Money(decimal amount, string currency, string note) : IEquatable<Money>
{
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;
    public string Note { get; } = note;
    public bool Equals(Money? other) => other is not null && Amount == other.Amount && Currency == other.Currency && Note == other.Note;
    public override bool Equals(object? obj) => Equals(obj as Money);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency, Note);
}

public class Animal
{
    public string Name { get; init; } = "";
}

public sealed class Dog : Animal
{
    public int Legs { get; init; }
}

public sealed record Customer(int Id, string Name, Address Address);

public sealed record Address(string Street, string City);

public sealed record Order(int Id, decimal Total, IReadOnlyList<string> Lines);
