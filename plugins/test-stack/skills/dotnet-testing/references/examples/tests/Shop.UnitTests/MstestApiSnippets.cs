namespace Shop.UnitTests;

[TestClass]
public sealed class MstestApiSnippets
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void IsInstanceOfType_Order_ReturnsTypedValue()
    {
        object value = new Order(1, 1m, null, []);

        Order order = Assert.IsInstanceOfType<Order>(value);

        Assert.AreEqual(1, order.Id, $"Got order {order.Id}");
    }

    [TestMethod]
    public void AreSequenceEqual_SameItems_Passes()
    {
        int[] actual = [1, 2, 3];

        Assert.AreSequenceEqual(new[] { 1, 2, 3 }, actual);
    }

    [TestMethod]
    public void ThrowsExactly_NegativeTotal_ReturnsException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Discount.For(-1m));

        Assert.AreEqual("total", ex.ParamName);
    }

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    public async Task ThrowsExactlyAsync_UnknownOrder_Throws()
    {
        var sut = new OrderService(new FakeOrderStore(), TimeProvider.System);

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => sut.CloseAsync(1, TestContext.CancellationToken));
    }

    [TestMethod]
    [DynamicData(nameof(Rows))]
    public void For_Row_ReturnsExpected(decimal total, decimal expected)
    {
        decimal discount = Discount.For(total);

        Assert.AreEqual(expected, discount);
    }

    public static IEnumerable<TestDataRow<(decimal Total, decimal Expected)>> Rows =>
    [
        new((50m, 0m)) { DisplayName = "No discount below 100" },
        new((100m, 5m)) { DisplayName = "Five percent at 100" },
    ];
}
