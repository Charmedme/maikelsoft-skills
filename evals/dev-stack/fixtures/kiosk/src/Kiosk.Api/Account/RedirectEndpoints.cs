namespace Kiosk.Api.Account;

public sealed class RedirectOptions
{
    public const string Section = "Redirects";

    public List<string> Allowed { get; set; } = [];
}

public static class RedirectEndpoints
{
    public static IEndpointRouteBuilder MapRedirectEndpoints(this IEndpointRouteBuilder app)
    {
        // Outbound links in the footer and in mails go through here, so we can count them.
        app.MapGet("/redirect", (string? to, IConfiguration configuration) =>
        {
            var allowed = configuration.GetSection(RedirectOptions.Section).Get<RedirectOptions>()?.Allowed ?? [];
            if (string.IsNullOrEmpty(to) || !IsAllowed(to, allowed))
            {
                return Results.Problem(title: "Unrecognized target URL for redirect.", statusCode: StatusCodes.Status406NotAcceptable);
            }

            return Results.Redirect(to);
        });
        return app;
    }

    private static bool IsAllowed(string url, IEnumerable<string> allowed)
    {
        var result = false;
        foreach (var entry in allowed)
        {
            result = result || url.Contains(entry, StringComparison.Ordinal);
        }

        return result;
    }
}
