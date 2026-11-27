namespace Kiosk.Api.Users;

public static class UsersSetup
{
    public static IServiceCollection AddUsers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<TokenService>();
        services.AddScoped<CredentialValidator>();
        services.AddScoped<Data.SeedData>();
        services.AddHttpClient<ProfileImageFetcher>(client => client.Timeout = TimeSpan.FromSeconds(10));
        return services;
    }
}
