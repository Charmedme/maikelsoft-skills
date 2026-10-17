using FileSignatures;
using FileSignatures.Formats;

namespace Orders.Api.Documents;

// The default FileFormatInspector also knows executables (exe, ELF, WASM). Register only the allowed formats.
public sealed class DocumentInspector
{
    private readonly FileFormatInspector _inspector = new([new Pdf(), new Png(), new Jpeg()]);

    public string? Detect(Stream stream) => _inspector.DetermineFileFormat(stream) switch
    {
        Pdf => ".pdf",
        Png => ".png",
        Jpeg => ".jpg",
        _ => null,
    };
}

internal static class Security
{
    // StartTls fails when the relay does not offer STARTTLS. Auto and StartTlsWhenAvailable fall back to plain text on port 587.
    internal const MailKit.Security.SecureSocketOptions Mode = MailKit.Security.SecureSocketOptions.StartTls;
}
