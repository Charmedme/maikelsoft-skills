using RabbitMQ.Client;

namespace Orders.Api.Messaging;

// One long-lived connection for the consumers of this process. The outbox publisher has its own connection.
public sealed class RabbitMqConnection(IConfiguration configuration) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is not { IsOpen: true })
            {
                if (_connection is not null)
                {
                    await _connection.DisposeAsync();
                }

                var factory = new ConnectionFactory
                {
                    Uri = RabbitMqTopology.BrokerUri(configuration),
                    ClientProvidedName = "orders-api.consumers",
                };
                _connection = await factory.CreateConnectionAsync(ct);
            }

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
