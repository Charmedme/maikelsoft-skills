namespace Orders.Api.Payments;

public enum PaymentStatus
{
    Authorized = 0,
    Declined = 1,
    Captured = 2,
    Failed = 3,
}
