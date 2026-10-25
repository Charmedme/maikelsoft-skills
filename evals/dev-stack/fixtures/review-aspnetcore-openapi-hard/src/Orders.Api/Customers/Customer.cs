namespace Orders.Api.Customers;

public sealed class Customer
{
    private Customer()
    {
        Email = string.Empty;
        Name = string.Empty;
    }

    public Customer(string email, string name, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Email = email.Trim().ToLowerInvariant();
        Name = name.Trim();
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
