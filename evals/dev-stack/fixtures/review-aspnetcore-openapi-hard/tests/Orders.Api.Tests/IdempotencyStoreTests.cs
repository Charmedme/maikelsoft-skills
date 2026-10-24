using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Orders.Api.Infrastructure;

namespace Orders.Api.Tests;

[TestClass]
public sealed class IdempotencyStoreTests
{
    [TestMethod]
    public void TryRegister_SameKeyTwice_SecondFails()
    {
        var store = new IdempotencyStore(new FakeTimeProvider());

        store.TryRegister("a").Should().BeTrue();
        store.TryRegister("a").Should().BeFalse();
    }

    [TestMethod]
    public void TryRegister_After24Hours_KeyIsFreeAgain()
    {
        var time = new FakeTimeProvider();
        var store = new IdempotencyStore(time);
        store.TryRegister("a");

        time.Advance(TimeSpan.FromHours(25));

        store.TryRegister("a").Should().BeTrue();
    }
}
