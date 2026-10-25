namespace Orders.Api.Reports;

public sealed record MonthlySalesRow(DateOnly Month, int Orders, decimal Revenue);

public sealed record UnusedCouponRow(string Code, decimal PercentOff, DateTimeOffset ValidUntil);
