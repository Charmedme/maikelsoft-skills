using FluentAssertions;
using Orders.Api.Products;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ProductTests
{
    [TestMethod]
    public void NewProduct_IsActive() => new Product("SKU-1", "Mug", 9.95m).Active.Should().BeTrue();

    [TestMethod]
    public void Deactivate_MakesInactive()
    {
        var product = new Product("SKU-1", "Mug", 9.95m);

        product.Deactivate();

        product.Active.Should().BeFalse();
    }

    [TestMethod]
    public void ChangePrice_Negative_Throws()
    {
        var product = new Product("SKU-1", "Mug", 9.95m);

        var act = () => product.ChangePrice(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
