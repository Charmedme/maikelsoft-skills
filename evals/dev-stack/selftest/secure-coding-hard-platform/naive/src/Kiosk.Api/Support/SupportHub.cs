using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Support;

public sealed record SupportMessageDto(string Sender, string Text, DateTimeOffset SentAt);

/// <summary>Live chat between customers and the support team. The client opens the conversation "customer-{id}".</summary>
[Authorize]
public sealed class SupportHub(KioskDbContext db, TimeProvider time) : Hub
{
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        var history = await db.SupportMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .Select(m => new SupportMessageDto(m.Sender, m.Text, m.SentAt))
            .ToListAsync();
        await Clients.Caller.SendAsync("History", history);
    }

    public async Task SendMessage(string conversationId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var message = new SupportMessage
        {
            ConversationId = conversationId,
            Sender = Context.User?.GetEmail() is { Length: > 0 } email ? email : "guest",
            Text = text.Trim(),
            SentAt = time.GetUtcNow(),
        };
        db.SupportMessages.Add(message);
        await db.SaveChangesAsync();
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new SupportMessageDto(message.Sender, message.Text, message.SentAt));
    }
}
