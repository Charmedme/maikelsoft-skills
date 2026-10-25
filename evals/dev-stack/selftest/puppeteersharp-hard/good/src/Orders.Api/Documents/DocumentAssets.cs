namespace Orders.Api.Documents;

// Our own logo, embedded in the assembly. The document pages have no network, so the logo goes in as a data URI.
public static class DocumentAssets
{
    private static readonly Lazy<string> s_logo = new(() =>
    {
        using var stream = typeof(DocumentAssets).Assembly.GetManifestResourceStream("Orders.Api.Documents.Assets.logo.png")
            ?? throw new InvalidOperationException("The embedded logo is missing.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return "data:image/png;base64," + Convert.ToBase64String(copy.ToArray());
    });

    public static string LogoDataUri => s_logo.Value;
}
