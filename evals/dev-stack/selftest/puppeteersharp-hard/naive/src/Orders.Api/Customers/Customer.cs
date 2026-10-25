namespace Orders.Api.Customers;

public sealed class Customer
{
    private Customer()
    {
        Name = string.Empty;
        Email = string.Empty;
    }

    public Customer(string name, string email, CustomerKind kind, string? companyName, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (kind == CustomerKind.Business && string.IsNullOrWhiteSpace(companyName))
        {
            throw new ArgumentException("A business customer needs a company name.", nameof(companyName));
        }

        Name = name.Trim();
        Email = email.Trim();
        Kind = kind;
        CompanyName = companyName?.Trim();
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public CustomerKind Kind { get; private set; }

    public string? CompanyName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // Business customers can show their own logo and a footer line on their documents.
    public Uri? BrandingLogoUrl { get; private set; }

    public string? BrandingFooter { get; private set; }

    public string DisplayName => Kind == CustomerKind.Business ? $"{CompanyName} ({Name})" : Name;

    public void SetBranding(Uri? logoUrl, string? footer)
    {
        if (Kind != CustomerKind.Business)
        {
            throw new InvalidOperationException("Only a business customer can have branding.");
        }

        BrandingLogoUrl = logoUrl;
        BrandingFooter = string.IsNullOrWhiteSpace(footer) ? null : footer.Trim();
    }

    public void ChangeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Email = email.Trim();
    }
}
