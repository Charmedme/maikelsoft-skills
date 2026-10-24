namespace Orders.Api.Payments;

public interface IPaymentGateway
{
    Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken ct);
}

public sealed record AuthorizationRequest(Guid OrderId, decimal Amount, string Currency);

public sealed record AuthorizationResult(bool Authorized, string? Reference, string? DeclineReason);
