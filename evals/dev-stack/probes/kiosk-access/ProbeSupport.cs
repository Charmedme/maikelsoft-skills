using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kiosk.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Kiosk.Api.Tests.EvalProbes;

// Shared support for the hidden probes of the kiosk cases. Each probe starts the real app in the test
// process with its own database, runs one attack through the public HTTP surface, and asserts the secure result.
internal sealed class ProbeFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = "kiosk_probe_" + Guid.NewGuid().ToString("N");

    public string ConnectionString => new NpgsqlConnectionStringBuilder(AssemblyFixture.ConnectionString) { Database = DatabaseName }.ConnectionString;

    public string EnvironmentName { get; init; } = "Development";

    public Dictionary<string, string?> Settings { get; } = new();

    /// <summary>The client address that the server sees. Null gives each request its own random public address.</summary>
    public IPAddress? RemoteIp { get; init; }

    public Action<IServiceCollection>? ExtraServices { get; init; }

    public Action<IApplicationBuilder>? AppendToPipeline { get; init; }

    public LogCapture Logs { get; } = new();

    public async Task<T> QueryAsync<T>(Func<KioskDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<KioskDbContext>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("ConnectionStrings:Kiosk", ConnectionString);
        builder.UseSetting("Database:Initialize", "true");
        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new ProbeStartupFilter(RemoteIp, AppendToPipeline));
            ExtraServices?.Invoke(services);
        });
    }
}

internal sealed class ProbeStartupFilter(IPAddress? remoteIp, Action<IApplicationBuilder>? append) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress = remoteIp ?? new IPAddress([198, 18, (byte)Random.Shared.Next(0, 256), (byte)Random.Shared.Next(1, 255)]);
            return nextMiddleware(context);
        });
        next(app);
        append?.Invoke(app);
    };
}

internal sealed class LogCapture : ILoggerProvider
{
    public ConcurrentQueue<string> Lines { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class Logger(LogCapture owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            owner.Lines.Enqueue($"{category}: {formatter(state, exception)} {exception}");
    }
}

internal static partial class Probe
{
    public const string AdminEmail = "admin@kiosk.test";
    public const string AdminPassword = "Kiosk-Admin-2026!";
    public const string MariaEmail = "maria.lopez@kiosk.test";
    public const string MariaPassword = "Maria-Spring-2026!";
    public const string TomEmail = "tom.berg@kiosk.test";
    public const string TomPassword = "Tom-Garden-2026!";
    public const string FinanceEmail = "finance@kiosk.test";
    public const string FinancePassword = "Finance-Ledger-2026!";
    public const string PartnerEmail = "orders@partner-retail.test";
    public const string PartnerPassword = "Partner-Feed-2026!";
    public const string StrongPassword = "Probe-Long-Passphrase-2026!";

    /// <summary>
    /// Starts the app. When the app has no JWT signing key in its own configuration (for example after the key
    /// moved to a secret store), the probe supplies a random one, so that the app can start.
    /// </summary>
    public static ProbeFactory Start(Func<ProbeFactory> create)
    {
        var factory = create();
        try
        {
            _ = factory.Server;
            if (!string.IsNullOrWhiteSpace(factory.Services.GetRequiredService<IConfiguration>()["Jwt:SigningKey"]))
            {
                return factory;
            }
        }
#pragma warning disable CA1031 // Any start failure means: try again with a key.
        catch (Exception)
        {
        }
#pragma warning restore CA1031

        factory.Dispose();
        var retry = create();
        retry.Settings["Jwt:SigningKey"] = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        _ = retry.Server;
        return retry;
    }

    public static ProbeFactory Start() => Start(() => new ProbeFactory());

    public static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@probe.test";

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Normal login of {email} failed: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    public static async Task<HttpClient> SignedInAsync(this ProbeFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, password));
        return client;
    }

    public static async Task<string> RegisterAsync(this ProbeFactory factory, string prefix, string password = StrongPassword)
    {
        var email = NewEmail(prefix);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/users", new { email, password, displayName = "Probe", securityQuestionId = 1, securityAnswer = "Rex" });
        Assert.IsTrue(response.IsSuccessStatusCode, $"Normal registration failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return email;
    }

    public static HttpClient CookieClient(this ProbeFactory factory, Uri? baseAddress = null) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = baseAddress ?? new Uri("http://localhost") });

    public static async Task<HttpResponseMessage> CookieSignInAsync(HttpClient client, string email, string password, string? returnUrl, string path = "/account/login")
    {
        var html = await client.GetStringAsync(path + (returnUrl is null ? string.Empty : "?returnUrl=" + Uri.EscapeDataString(returnUrl)));
        var token = TokenField().Match(html);
        var form = new Dictionary<string, string> { ["email"] = email, ["password"] = password, ["returnUrl"] = returnUrl ?? string.Empty };
        if (token.Success)
        {
            form["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        }

        return await client.PostAsync(path, new FormUrlEncodedContent(form));
    }

    /// <summary>True when the server refused the call: 401, 403, 404, or a redirect to the sign-in page.</summary>
    public static bool IsDenied(HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound
        || (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found && response.Headers.Location?.OriginalString.Contains("login", StringComparison.OrdinalIgnoreCase) == true);

    public static string Describe(HttpResponseMessage response, string body) =>
        $"status {(int)response.StatusCode}, body: {(body.Length > 300 ? body[..300] : body)}";

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex TokenField();
}
