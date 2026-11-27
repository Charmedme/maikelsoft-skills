using Kiosk.Api.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Kiosk.Api.Hosting;

public static class Policies
{
    public const string Admin = "admin";

    public const string Staff = "staff";

    public const string Partner = "partner";

    public const string ApiClient = "api-client";

    public const string WebSession = "web-session";

    public static IServiceCollection AddKioskAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Admin, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Staff, policy => policy.RequireRole(Roles.Admin, Roles.Accounting))
            .AddPolicy(Partner, policy => policy.RequireRole(Roles.Partner, Roles.Admin))
            .AddPolicy(ApiClient, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddPolicy(WebSession, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
        return services;
    }
}
