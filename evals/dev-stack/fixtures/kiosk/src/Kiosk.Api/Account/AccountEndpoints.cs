using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Kiosk.Api.Users;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Account;

public sealed record LoginForm(string? Email, string? Password, string? ReturnUrl);

public sealed record EmailForm(string? Email);

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/account");
        account.MapGet("/login", (string? returnUrl, HttpContext http, IAntiforgery antiforgery) =>
            Results.Content(AccountPages.Login("/account/login", returnUrl, antiforgery.GetAndStoreTokens(http).RequestToken, null), "text/html"));
        account.MapPost("/login", SignInAsync);
        account.MapGet("/", AccountPageAsync).RequireAuthorization(Policies.WebSession);
        // The mobile app posts this form from a web view that has no antiforgery token.
        account.MapPost("/email", ChangeEmailAsync).RequireAuthorization(Policies.WebSession).DisableAntiforgery();
        account.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/account/login");
        });
        return app;
    }

    private static async Task<IResult> SignInAsync([FromForm] LoginForm form, HttpContext http, CredentialValidator credentials, IAntiforgery antiforgery, CancellationToken ct)
    {
        var user = await credentials.ValidateAsync(form.Email ?? string.Empty, form.Password ?? string.Empty, ct);
        if (user is null)
        {
            var token = antiforgery.GetAndStoreTokens(http).RequestToken;
            return Results.Content(AccountPages.Login("/account/login", form.ReturnUrl, token, "Invalid email or password."), "text/html", statusCode: StatusCodes.Status401Unauthorized);
        }

        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AccountPrincipal.Create(user));

        var target = string.IsNullOrWhiteSpace(form.ReturnUrl) ? "/account" : form.ReturnUrl;
        return target.StartsWith('/') ? Results.Redirect(target) : Results.Redirect("/account");
    }

    private static async Task<IResult> AccountPageAsync(HttpContext http, KioskDbContext db, IAntiforgery antiforgery, CancellationToken ct)
    {
        var email = await db.Users.Where(u => u.Id == http.User.GetUserId()).Select(u => u.Email).SingleAsync(ct);
        return Results.Content(AccountPages.Account(email, antiforgery.GetAndStoreTokens(http).RequestToken), "text/html");
    }

    private static async Task<IResult> ChangeEmailAsync([FromForm] EmailForm form, HttpContext http, KioskDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Email) || !form.Email.Contains('@', StringComparison.Ordinal))
        {
            return Results.BadRequest();
        }

        var email = form.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return Results.Conflict();
        }

        var user = await db.Users.SingleAsync(u => u.Id == http.User.GetUserId(), ct);
        user.Email = email;
        await db.SaveChangesAsync(ct);
        return Results.Redirect("/account");
    }
}
