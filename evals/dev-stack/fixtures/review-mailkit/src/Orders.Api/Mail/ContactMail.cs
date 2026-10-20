using System.ComponentModel.DataAnnotations;
using MimeKit;

namespace Orders.Api.Mail;

public sealed record ContactRequest(
    [property: Required, StringLength(100)] string Name,
    [property: Required, EmailAddress] string Email,
    [property: Required, StringLength(4000)] string Text);

public static class ContactMail
{
    public static IEndpointRouteBuilder MapContact(this IEndpointRouteBuilder app)
    {
        app.MapPost("/contact", SendAsync).WithTags("Contact");
        return app;
    }

    private static async Task<IResult> SendAsync(ContactRequest request, MailSender sender, CancellationToken ct)
    {
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(request.Name, request.Email));
        message.To.Add(new MailboxAddress("Customer service", "service@example.com"));
        message.Subject = "Contact form";
        message.Body = new TextPart("plain") { Text = request.Text };

        await sender.SendAsync(message, ct);
        return TypedResults.Accepted((string?)null);
    }
}
