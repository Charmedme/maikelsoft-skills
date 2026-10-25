using System.Collections.Concurrent;
using Orders.Api.Payments;

namespace Orders.Api.Tests;

// Declines each amount of 1000 or more (insufficient funds). Authorizes all other amounts.
public sealed class FakePaymentGateway : IPaymentGateway
{
    public ConcurrentQueue<AuthorizationRequest> Requests { get; } = new();

    public Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken ct)
    {
        Requests.Enqueue(request);
        var declined = request.Amount >= 1000m;
        return Task.FromResult(declined
            ? new AuthorizationResult(false, $"psp-{Guid.CreateVersion7():N}", "insufficient_funds")
            : new AuthorizationResult(true, $"psp-{Guid.CreateVersion7():N}", null));
    }
}
