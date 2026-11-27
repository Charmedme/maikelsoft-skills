using Kiosk.Api.Users;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Account;

/// <summary>Sign-in for the back-office tools (admin and accounting).</summary>
public static class StaffAccountEndpoints
{
    public static IEndpointRouteBuilder MapStaffAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/account/staff/login", (string? returnUrl, HttpContext http, IAntiforgery antiforgery) =>
            Results.Content(AccountPages.Login("/account/staff/login", returnUrl, antiforgery.GetAndStoreTokens(http).RequestToken, null), "text/html"));
        app.MapPost("/account/staff/login", SignInAsync);
        return app;
    }

    private static async Task<IResult> SignInAsync([FromForm] LoginForm form, HttpContext http, CredentialValidator credentials, IAntiforgery antiforgery, CancellationToken ct)
    {
        var user = await credentials.ValidateAsync(form.Email ?? string.Empty, form.Password ?? string.Empty, ct);
        if (user is null || user.Role is not (Roles.Admin or Roles.Accounting))
        {
            var token = antiforgery.GetAndStoreTokens(http).RequestToken;
            return Results.Content(AccountPages.Login("/account/staff/login", form.ReturnUrl, token, "Invalid email or password."), "text/html", statusCode: StatusCodes.Status401Unauthorized);
        }

        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AccountPrincipal.Create(user));
        return Results.LocalRedirect(string.IsNullOrWhiteSpace(form.ReturnUrl) ? "/account" : form.ReturnUrl);
    }
}
