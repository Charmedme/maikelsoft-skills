using FluentAssertions;
using Orders.Api.Invoices;

namespace Orders.Api.Tests;

[TestClass]
public sealed class InvoiceNumbersTests
{
    [TestMethod]
    public void Format_PadsToSixDigits() => InvoiceNumbers.Format(2026, 42).Should().Be("2026-000042");

    [TestMethod]
    public void Format_LargeSequence_KeepsAllDigits() => InvoiceNumbers.Format(2026, 1234567).Should().Be("2026-1234567");
}
