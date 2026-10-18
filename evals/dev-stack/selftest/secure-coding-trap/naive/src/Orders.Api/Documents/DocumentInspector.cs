using FileSignatures;

namespace Orders.Api.Documents;

public sealed class DocumentInspector
{
    private readonly FileFormatInspector _inspector = new();

    public string? Detect(Stream stream) => _inspector.DetermineFileFormat(stream) is { } format ? "." + format.Extension : null;
}

internal static class Security
{
    internal const MailKit.Security.SecureSocketOptions Mode = MailKit.Security.SecureSocketOptions.Auto;
}
