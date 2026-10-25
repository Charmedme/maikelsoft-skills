using FluentAssertions;
using Orders.Api.Import;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CsvOrderReaderTests
{
    [TestMethod]
    public void Read_GroupsLinesByExternalRef()
    {
        var customer = Guid.CreateVersion7();
        var csv = $"h\nA;{customer};SKU-1;1;1.00\nB;{customer};SKU-2;2;2.00\nA;{customer};SKU-3;3;3.00\n";

        var (orders, rejected) = CsvOrderReader.Read(csv);

        rejected.Should().BeEmpty();
        orders.Should().HaveCount(2);
        orders.Single(o => o.ExternalRef == "A").Request.Lines.Select(l => l.Sku).Should().Equal("SKU-1", "SKU-3");
    }

    [TestMethod]
    public void Read_InvalidCustomerId_GivesNullCustomer()
    {
        var (orders, _) = CsvOrderReader.Read("h\nA;not-a-guid;SKU-1;1;1.00");

        orders.Single().Request.CustomerId.Should().BeNull();
    }

    [TestMethod]
    public void Read_DecimalUsesInvariantCulture()
    {
        var (orders, _) = CsvOrderReader.Read($"h\nA;{Guid.CreateVersion7()};SKU-1;1;1234.56");

        orders.Single().Request.Lines.Single().UnitPrice.Should().Be(1234.56m);
    }

    [TestMethod]
    public void Read_WrongColumnCount_IsRejected()
    {
        var (orders, rejected) = CsvOrderReader.Read("h\nA;B;C");

        orders.Should().BeEmpty();
        rejected.Should().ContainSingle().Which.ExternalRef.Should().Be("row 2");
    }
}
