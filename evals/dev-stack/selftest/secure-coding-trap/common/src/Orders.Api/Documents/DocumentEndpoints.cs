using System.ComponentModel.DataAnnotations;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using MimeKit;
using Orders.Api.Data;

namespace Orders.Api.Documents;

public sealed class DocumentOptions
{
    [Required]
    public string RootPath { get; set; } = string.Empty;
}

public sealed class SmtpOptions
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string From { get; set; } = string.Empty;
}

public static partial class DocumentEndpoints
{
    private const long MaxBytes = 10 * 1024 * 1024;

    public static IServiceCollection AddDocuments(this IServiceCollection services)
    {
        services.AddOptions<DocumentOptions>().BindConfiguration("Documents").ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<SmtpOptions>().BindConfiguration("Smtp").ValidateDataAnnotations();
        services.AddSingleton<DocumentInspector>();
        return services;
    }

    public static IEndpointRouteBuilder MapDocuments(this IEndpointRouteBuilder app)
    {
        // No cookie authentication in this API, so antiforgery does not apply to the upload.
        app.MapPost("/orders/{id:guid}/documents", UploadAsync).DisableAntiforgery().WithTags("Orders");
        return app;
    }

    private static async Task<Results<Created, NotFound, ValidationProblem>> UploadAsync(
        Guid id,
        IFormFile file,
        [Microsoft.AspNetCore.Mvc.FromForm] string email,
        OrdersDbContext db,
        DocumentInspector inspector,
        IOptions<DocumentOptions> documents,
        IOptions<SmtpOptions> smtp,
        ILoggerFactory loggers,
        CancellationToken ct)
    {
        if (await db.Orders.FindAsync([id], ct) is null)
        {
            return TypedResults.NotFound();
        }

        if (file.Length is 0 or > MaxBytes)
        {
            return Invalid("The file must have 1 byte to 10 MB.");
        }

        await using var stream = file.OpenReadStream();
        var extension = inspector.Detect(stream);
        if (extension is null)
        {
            return Invalid("Only PDF, PNG and JPEG files are allowed.");
        }

        var name = $"{Guid.CreateVersion7():N}{extension}";
        stream.Position = 0;
        await using (var target = File.Create(Path.Combine(documents.Value.RootPath, name)))
        {
            await stream.CopyToAsync(target, ct);
        }

        try
        {
            await SendConfirmationAsync(smtp.Value, email, id, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogMailFailed(loggers.CreateLogger(typeof(DocumentEndpoints)), id, ex);
        }

        return TypedResults.Created($"/orders/{id}/documents/{name}");
    }

    private static async Task SendConfirmationAsync(SmtpOptions smtp, string email, Guid orderId, CancellationToken ct)
    {
        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Document received";
        message.Body = new TextPart("plain") { Text = $"We received your document for order {orderId}." };

        using var client = new SmtpClient { Timeout = 10_000 };
        await client.ConnectAsync(smtp.Host, smtp.Port, Security.Mode, ct);
        await client.AuthenticateAsync(smtp.UserName, smtp.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    private static ValidationProblem Invalid(string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal) { ["file"] = [message] });

    [LoggerMessage(Level = LogLevel.Warning, Message = "The confirmation mail for order {OrderId} was not sent.")]
    private static partial void LogMailFailed(ILogger logger, Guid orderId, Exception exception);
}
