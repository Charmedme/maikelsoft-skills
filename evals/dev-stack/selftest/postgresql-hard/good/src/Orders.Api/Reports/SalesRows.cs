namespace Orders.Api.Reports;

public sealed record WeeklySalesRow(DateOnly WeekStart, int Orders, decimal Revenue);
