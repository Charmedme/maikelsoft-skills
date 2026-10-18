# Security tests

Tests that prove a security control. Test framework rules are in `dev-stack:dotnet-testing`. The code was compiled and run on 2026-10-16 with MSTest.Sdk 4.5.1, AwesomeAssertions 9.6.0 and Microsoft.AspNetCore.Mvc.Testing 10.0.13 (6 tests passed). FluentAssertions 7.2.2 uses the same assertion calls.

## Contents

- What to test
- Authorization inventory (compiled)
- SSRF guard and paths (compiled)
- Unauthenticated request (compiled)
- Other tests
- What not to test

## What to test

| Test | Proves | Expected |
|------|--------|----------|
| Authorization inventory | No endpoint without a policy (CWE-862) | Fallback policy set, anonymous list matches |
| Cross-tenant per resource | No IDOR (API1) | User of tenant A gets 404 for an object of tenant B |
| JWT negative | Token validation (ASVS V9) | Expired, wrong audience, wrong issuer, unsigned, wrong key: 401 |
| Antiforgery | CSRF control | POST, PUT, PATCH, DELETE without token on cookie endpoints: 400 |
| Headers and CORS | SEC-09, SEC-10 | Headers present. Preflight from an unknown origin: no `Access-Control-Allow-Origin` |
| Rate limit | SEC-11 | Call N+1 gives 429 |
| SQL injection | Raw SQL paths | `' OR 1=1 --` in a search field returns no extra rows (real database, Testcontainers) |
| Paths and uploads | CWE-22, CWE-434 | `../../etc/passwd`, absolute paths, double extensions, too large, wrong signature: rejected |
| SSRF guard | API7 | Private, loopback, link-local, mapped addresses: blocked. Redirect to a private IP fails |
| ReDoS | CWE-1333 | A crafted near-match ends within the timeout, the code returns "no match" |
| Errors | CWE-209 | A forced exception in Production gives ProblemDetails without a stack trace |
| Logging | SEC-14 | With `FakeLogger` and `FakeRedactorProvider`, a password or token never appears in the output |

## Authorization inventory (compiled)

```csharp
[TestClass]
public sealed class SecurityTests
{
    private static WebApplicationFactory<Program> factory = null!;

    [ClassInitialize]
    public static void Start(TestContext _) => factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:App", "Server=unused;Database=unused");
            b.ConfigureTestServices(services =>
            {
                services.AddSingleton<IDocumentStore, FakeDocumentStore>();
                services.AddDataProtection().UseEphemeralDataProtectionProvider(); // no database in this test
            });
        });

    [ClassCleanup]
    public static void Stop() => factory.Dispose();

    // Catches CWE-862: a new endpoint that nobody protected.
    [TestMethod]
    public void Each_endpoint_has_a_policy_or_is_explicitly_anonymous()
    {
        var fallback = factory.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value.FallbackPolicy;
        fallback.Should().NotBeNull("new endpoints must be closed by default");

        var anonymous = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        // Each anonymous route is a decision. Add a route here only after a review.
        anonymous.Should().BeEquivalentTo(["/health"]);
    }
}
```

The fallback policy protects each endpoint without metadata. The test then only has to keep the anonymous list under review. A new `AllowAnonymous` fails the test until somebody adds the route on purpose.

## SSRF guard and paths (compiled)

```csharp
[TestMethod]
public void Ssrf_guard_blocks_internal_addresses()
{
    string[] blocked = ["127.0.0.1", "10.1.2.3", "169.254.169.254", "::1", "::ffff:127.0.0.1", "fd00::1", "0.0.0.0"];
    foreach (var address in blocked)
    {
        OutboundHttp.IsBlocked(IPAddress.Parse(address)).Should().BeTrue(address);
    }

    OutboundHttp.IsBlocked(IPAddress.Parse("93.184.215.14")).Should().BeFalse();
}

[TestMethod]
[DataRow("../../etc/passwd")]
[DataRow("/etc/passwd")]
[DataRow("a/../../b")]
public void Path_outside_the_base_folder_is_rejected(string input)
{
    var act = () => SafeInput.ResolveUnder(Path.GetTempPath(), input);
    act.Should().Throw<UnauthorizedAccessException>();
}
```

## Unauthenticated request (compiled)

```csharp
[TestMethod]
public async Task Request_without_token_gets_401()
{
    using var client = factory.CreateClient();
    var response = await client.GetAsync($"/shipments/{Guid.NewGuid()}");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## Other tests

- Cross-tenant: use a test authentication handler that sets `tenant_id` and `sub` from a header. Seed one object per tenant in a Testcontainers database. Assert 404 for the other tenant. Write one test per resource type.
- JWT negative: sign test tokens with a test key, and point `TokenValidationParameters.IssuerSigningKey` to it in `ConfigureTestServices`. Do not turn off validation in the test host: then the test proves nothing.
- Headers: assert `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy` on one HTML and one API response.

## What not to test

Framework internals (that Razor encodes, that EF Core parameterizes LINQ), crypto algorithms, and third-party behavior that the library tests itself (that FileSignatures detects each format).
