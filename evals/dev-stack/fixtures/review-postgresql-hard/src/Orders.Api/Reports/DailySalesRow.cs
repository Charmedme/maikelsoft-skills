namespace Orders.Api.Reports;

public sealed record DailySalesRow(DateOnly Day, int Orders, decimal Revenue);
