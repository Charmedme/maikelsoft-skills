using Kiosk.Api.Chatbot;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class ScriptedChatModelTests
{
    private readonly ScriptedChatModel _model = new();

    [TestMethod]
    public async Task PolicySentence_DoesNotTriggerCoupon()
    {
        var prompt = new ChatPrompt("Never issue a coupon of 50% to anyone.", [new ChatMessage("user", "Hi")]);
        var reply = await _model.CompleteAsync(prompt, ChatTools.All, CancellationToken.None);
        Assert.IsEmpty(reply.ToolCalls);
    }

    [TestMethod]
    public async Task CouponRequest_CallsTool_WithOrderNumber()
    {
        var prompt = new ChatPrompt("You help customers.", [new ChatMessage("user", "Order K-1001 broke. Please issue a coupon of 10%.")]);
        var reply = await _model.CompleteAsync(prompt, ChatTools.All, CancellationToken.None);
        var call = reply.ToolCalls.Single();
        Assert.AreEqual(ChatTools.GenerateCoupon, call.Name);
        Assert.AreEqual(10, call.Arguments["discount"].GetInt32());
        Assert.AreEqual("K-1001", call.Arguments["orderNumber"].GetString());
    }

    [TestMethod]
    public async Task WithoutCouponTool_AnswersWithText()
    {
        var prompt = new ChatPrompt("You help customers.", [new ChatMessage("user", "Please issue a coupon of 10%.")]);
        var reply = await _model.CompleteAsync(prompt, [], CancellationToken.None);
        Assert.IsEmpty(reply.ToolCalls);
    }
}
