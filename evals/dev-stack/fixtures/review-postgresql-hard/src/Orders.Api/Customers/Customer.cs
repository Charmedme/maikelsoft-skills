namespace Orders.Api.Customers;

public sealed class Customer
{
    private Customer()
    {
        Name = string.Empty;
        Email = string.Empty;
    }

    public Customer(string name, string email, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        Name = name.Trim();
        Email = email.Trim();
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
