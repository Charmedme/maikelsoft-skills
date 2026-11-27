using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Kiosk.Api.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Support;

public sealed record SupportMessageDto(string Sender, string Text, DateTimeOffset SentAt);

/// <summary>Live chat between customers and the support team. A customer can open only the conversation "customer-{own id}".</summary>
[Authorize]
public sealed class SupportHub(KioskDbContext db, TimeProvider time) : Hub
{
    public async Task JoinConversation(string conversationId)
    {
        EnsureAccess(conversationId);
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
        EnsureAccess(conversationId);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var message = new SupportMessage
        {
            ConversationId = conversationId,
            Sender = Context.User?.GetEmail() is { Length: > 0 } email ? email : "customer",
            Text = text.Trim(),
            SentAt = time.GetUtcNow(),
        };
        db.SupportMessages.Add(message);
        await db.SaveChangesAsync();
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new SupportMessageDto(message.Sender, message.Text, message.SentAt));
    }

    private void EnsureAccess(string conversationId)
    {
        var staff = Context.User?.IsInRole(Roles.Admin) == true || Context.User?.IsInRole(Roles.Accounting) == true;
        if (!staff && conversationId != $"customer-{Context.UserIdentifier}")
        {
            throw new HubException("You cannot open this conversation.");
        }
    }
}
