namespace Orders.Api.Customers;

public sealed class Customer
{
    private Customer()
    {
        Email = string.Empty;
        Name = string.Empty;
        AddressLine = string.Empty;
        PostalCode = string.Empty;
        City = string.Empty;
        CountryCode = string.Empty;
    }

    public Customer(string email, string name, DeliveryAddress address, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(address);

        Email = email.Trim().ToLowerInvariant();
        Name = name.Trim();
        AddressLine = address.Line;
        PostalCode = address.PostalCode;
        City = address.City;
        CountryCode = address.CountryCode;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string Name { get; private set; }

    public string AddressLine { get; private set; }

    public string PostalCode { get; private set; }

    public string City { get; private set; }

    public string CountryCode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DeliveryAddress Address => new(AddressLine, PostalCode, City, CountryCode);

    // Changes the profile. A null address keeps the current delivery address.
    public void Update(string email, string name, DeliveryAddress? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Email = email.Trim();
        Name = name.Trim();
        if (address is not null)
        {
            AddressLine = address.Line;
            PostalCode = address.PostalCode;
            City = address.City;
            CountryCode = address.CountryCode;
        }
    }
}
