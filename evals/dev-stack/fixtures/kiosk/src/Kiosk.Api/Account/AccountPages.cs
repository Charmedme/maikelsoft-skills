using System.Text.Encodings.Web;

namespace Kiosk.Api.Account;

/// <summary>Plain server-rendered pages for sign-in and the account page of the web view.</summary>
public static class AccountPages
{
    public static string Login(string action, string? returnUrl, string? antiforgeryToken, string? error) => Page("Sign in", $"""
        {(error is null ? string.Empty : $"<p class=\"error\">{Encode(error)}</p>")}
        <form method="post" action="{Encode(action)}">
          <input type="hidden" name="__RequestVerificationToken" value="{Encode(antiforgeryToken)}">
          <input type="hidden" name="returnUrl" value="{Encode(returnUrl)}">
          <label>Email <input name="email" type="email" autocomplete="username"></label>
          <label>Password <input name="password" type="password" autocomplete="current-password"></label>
          <button type="submit">Sign in</button>
        </form>
        """);

    public static string Account(string email, string? antiforgeryToken) => Page("Your account", $"""
        <p>Signed in as <strong>{Encode(email)}</strong>.</p>
        <form method="post" action="/account/email">
          <input type="hidden" name="__RequestVerificationToken" value="{Encode(antiforgeryToken)}">
          <label>New email <input name="email" type="email"></label>
          <button type="submit">Change email</button>
        </form>
        <form method="post" action="/account/logout">
          <input type="hidden" name="__RequestVerificationToken" value="{Encode(antiforgeryToken)}">
          <button type="submit">Sign out</button>
        </form>
        """);

    private static string Page(string title, string body) => $"""
        <!doctype html>
        <html lang="en">
        <head><meta charset="utf-8"><title>{Encode(title)} - Kiosk</title></head>
        <body><h1>{Encode(title)}</h1>
        {body}
        </body>
        </html>
        """;

    private static string Encode(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
}
