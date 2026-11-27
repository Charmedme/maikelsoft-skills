using System.Globalization;
using System.Security.Claims;
using Kiosk.Api.Users;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Kiosk.Api.Account;

public static class AccountPrincipal
{
    public static ClaimsPrincipal Create(User user)
    {
        var id = user.Id.ToString(CultureInfo.InvariantCulture);
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", id),
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
