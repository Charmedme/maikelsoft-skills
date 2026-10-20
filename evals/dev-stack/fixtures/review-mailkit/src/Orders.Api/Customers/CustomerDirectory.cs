namespace Orders.Api.Customers;

public sealed record Customer(string Name, string Email);

public interface ICustomerDirectory
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct);
}

public sealed class ConfigurationCustomerDirectory(IConfiguration configuration) : ICustomerDirectory
{
    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct)
    {
        IReadOnlyList<Customer> customers = configuration.GetSection("Customers").Get<List<Customer>>() ?? [];
        return Task.FromResult(customers);
    }
}
