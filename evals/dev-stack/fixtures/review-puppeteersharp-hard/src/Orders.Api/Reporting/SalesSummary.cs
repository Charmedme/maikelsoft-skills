namespace Orders.Api.Reporting;

public sealed record SalesSummary(DateOnly Day, int Orders, int CancelledOrders, decimal NetRevenue);

public sealed record SalesReport(DateOnly From, DateOnly To, IReadOnlyList<SalesSummary> Days, decimal TotalNetRevenue);
