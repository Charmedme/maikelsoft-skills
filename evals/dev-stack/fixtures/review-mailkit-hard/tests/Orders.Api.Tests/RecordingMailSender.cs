using System.Collections.Concurrent;
using MimeKit;
using Orders.Api.Mail;

namespace Orders.Api.Tests;

public sealed class RecordingMailSender : IMailSender
{
    public ConcurrentQueue<MimeMessage> Sent { get; } = new();

    public Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
