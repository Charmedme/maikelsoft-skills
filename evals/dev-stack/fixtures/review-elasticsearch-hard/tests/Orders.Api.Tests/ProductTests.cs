using FluentAssertions;
using Orders.Api.Catalog;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ProductTests
{
    [TestMethod]
    public void Constructor_NormalizesSku()
    {
        var product = new Product(" sku-9 ", "Mug", 7.95m, VatRate.Standard, "Example", "kitchen");

        product.Sku.Should().Be("SKU-9");
        product.Active.Should().BeTrue();
    }

    [TestMethod]
    public void ChangePrice_Negative_Throws()
    {
        var product = new Product("SKU-9", "Mug", 7.95m, VatRate.Standard, "Example", "kitchen");

        var act = () => product.ChangePrice(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
