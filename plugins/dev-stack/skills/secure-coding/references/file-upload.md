# Secure file upload

The pipeline for each endpoint that accepts or serves user files, in ASP.NET Core 10 with FileSignatures 7.3.0. Decision: SEC-13. Size limits: SEC-12.

## Contents

- Pipeline
- Setup
- Endpoint (compiled)
- Download (compiled)
- Large files
- FileSignatures facts
- Never write
- Checks
- Tests

## Pipeline

Do the steps in this order. Each step is a separate layer. A signature match alone does not make a file safe.

| # | Step | How | Why |
|---|------|-----|-----|
| UP-1 | Authenticate and authorize | `RequireAuthorization("<permission>")` | Anonymous uploads fill storage and carry malware. |
| UP-2 | CSRF | Keep antiforgery on. Minimal APIs that bind `IFormFile` check it by default since .NET 8. | A cookie-authenticated upload without a token is CSRF. |
| UP-3 | Rate limit | `RequireRateLimiting("<policy>")` | Uploads are costly (API4). |
| UP-4 | Size limits at each layer | Kestrel global limit, `RequestSizeLimitAttribute` and `WithFormOptions(multipartBodyLengthLimit:)` per endpoint, `file.Length` check in code, IIS `maxAllowedContentLength` on IIS. | One layer alone is easy to forget. Check size before any parsing. |
| UP-5 | Extension allowlist | `Path.GetExtension(Path.GetFileName(name)).ToLowerInvariant()`, compare with a fixed list. Reject `:` and null bytes in the name. | Stops unexpected types and NTFS alternate data streams. |
| UP-6 | Ignore `Content-Type` | Never decide on `IFormFile.ContentType`. | The client sets it. |
| UP-7 | Signature | `IFileFormatInspector.DetermineFileFormat(stream)` with an allowlisted inspector. The detected format must match the extension. | A renamed `.exe` fails here. |
| UP-8 | Deeper checks for risky types | Re-encode images (strip metadata). Reject macro Office formats (`WordWithMacros`, `ExcelWithMacros`, `PowerPointWithMacros`). Avoid ZIP uploads, or count entries and decompressed bytes. Content Disarm and Reconstruction for PDF and Office when available. | Polyglots and active content pass a signature check. |
| UP-9 | Server-generated name | `Guid.NewGuid()`. Keep the client name as metadata only, with a max length, encoded on display and in logs. | Stops path traversal, overwrite and stored XSS in file lists. |
| UP-10 | Quarantine and malware scan | Write to a quarantine container. A background worker scans (ClamAV, Microsoft Defender for Storage), moves the file, and sets a scan status in the database. | Microsoft Learn: scan before the file is stored for use. |
| UP-11 | Private storage | Blob storage with a private container, or a folder outside the app tree without execute rights. Never `wwwroot`, never `UseStaticFiles` on that folder. | An uploaded HTML or SVG file served from your origin is stored XSS. |
| UP-12 | Download through a handler | Check authorization and scan status. `TypedResults.File(stream, storedMediaType, fileDownloadName: safeName)` (attachment), `X-Content-Type-Options: nosniff`. | The browser does not render the file in the app origin. |

Return 400 with ProblemDetails for a bad file and 413 for a too large request. Do not echo the raw file name in an error.

## Setup

Register one inspector with only the formats that the endpoint accepts. The default constructor knows about 70 formats, including `Executable`, `ElfExecutable` and `Wasm`. With an allowlist, each other format gives `null`.

```csharp
// Compiled with FileSignatures 7.3.0 on .NET 10.
var allowed = FileFormatLocator.GetFormats().Where(f => f is Pdf or Png or Jpeg).ToList();
services.AddSingleton<IFileFormatInspector>(new FileFormatInspector(allowed));
```

Use `FileFormatLocator.GetFormats()` (static list, trim and AOT safe), not `GetFormats(assembly)` (reflection, IL2026). Add a custom format with `allowed.Append(new MyFormat())`.

## Endpoint (compiled)

```csharp
private const long MaxFileBytes = 10 * 1024 * 1024;

app.MapPost("/documents", UploadAsync)
    .RequireAuthorization(Permissions.ShipmentsWrite)
    .RequireRateLimiting("expensive")
    .WithFormOptions(multipartBodyLengthLimit: MaxFileBytes)
    .WithMetadata(new RequestSizeLimitAttribute(MaxFileBytes + 1024 * 1024));

private static async Task<IResult> UploadAsync(
    IFormFile file, IFileFormatInspector inspector, IDocumentStore store, CancellationToken ct)
{
    // 1. Size first, before any parsing.
    if (file.Length is 0 or > MaxFileBytes)
    {
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "The file is empty or too large.");
    }

    // 2. Extension allowlist. The client name is only a hint, not a storage name.
    var name = Path.GetFileName(file.FileName);
    var extension = Path.GetExtension(name).ToLowerInvariant();
    if (name.Contains(':') || name.Contains('\0'))
    {
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "The file name is not allowed.");
    }

    // 3. Content signature. The detected format must match the extension.
    await using var stream = file.OpenReadStream(); // seekable: IFormFile is buffered
    var format = inspector.DetermineFileFormat(stream);
    var matches = (format, extension) switch
    {
        (Pdf, ".pdf") => true,
        (Png, ".png") => true,
        (Jpeg, ".jpg" or ".jpeg") => true,
        _ => false,
    };
    if (!matches || format is null)
    {
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "The file type is not allowed.");
    }

    // 4. Quarantine with a server-generated name. Use the detected media type, not file.ContentType.
    var id = await store.SaveToQuarantineAsync(stream, format.MediaType, untrustedName: name, ct);
    return Results.Accepted($"/documents/{id}", new { id, status = "scanning" });
}
```

`Pdf` also matches `AdobePdf`, and `Jpeg` matches `JpegJfif` and `JpegExif` (subclasses). `DetermineFileFormat` sets `Position = 0` again, so the store reads the stream from the start.

## Download (compiled)

```csharp
app.MapGet("/documents/{id:guid}", async (Guid id, IDocumentStore store, HttpContext http, CancellationToken ct) =>
{
    var document = await store.GetCleanAsync(id, http.User, ct); // checks owner, tenant and scan status
    if (document is null)
    {
        return Results.NotFound();
    }

    http.Response.Headers.XContentTypeOptions = "nosniff";
    return TypedResults.File(document.OpenRead(), document.MediaType, fileDownloadName: document.SafeDownloadName);
}).RequireAuthorization(Permissions.ShipmentsRead);
```

## Large files

- Up to a few MB: bind `IFormFile`. Check `Length`, then `OpenReadStream()`. ASP.NET Core buffers the file (memory up to 64 KB, then a temp file), so the stream is seekable.
- Larger: stream with `MultipartReader` to a quarantine location with a byte-counting copy. Then inspect the stored file. `DetermineFileFormat` needs a seekable stream: it throws `NotSupportedException` on `Request.Body` or `MultipartSection.Body`.
- Do not copy an upload into a `MemoryStream`. It costs memory and fails above 2 GB.
- Set the limits per endpoint. Do not raise the global limit for one large-upload endpoint.

## FileSignatures facts

| Fact | Value |
|------|-------|
| Version | 7.3.0 (2026-08-14), MIT, targets netstandard2.0 to net10.0 |
| Minimum | 7.1 or later: 7.1 updated OpenMcdf to fix a vulnerable dependency |
| Breaking changes | 6.0 renamed `Rar` to `Rar4` and `Rar5`. 7.3 changed the MKV media type to `video/matroska`. Compare by type (`format is Mkv`), not by string. |
| Container formats | ZIP and Compound File formats are opened to read the entry list (central directory only). The library does not extract content. Check the size before inspection. |
| What it proves | The first bytes, and for containers the internal structure, look like format X. Not that the file is safe or only format X. |

## Never write

| Wrong | Correct |
|-------|---------|
| `file.ContentType == "application/pdf"` as the check | Extension allowlist, signature, and the match between them |
| Own magic-number table with only `FF D8 FF E0` for JPEG | FileSignatures `Jpeg` |
| `Path.Combine(_env.WebRootPath, "uploads", file.FileName)` | Random name, private storage |
| `inspector.DetermineFileFormat(Request.Body)` | Buffer to a file first, then inspect |
| `new FileFormatInspector()` and `format is not null` means accept | Allowlisted inspector, compare with the extension |
| `.DisableAntiforgery()` to make a cookie upload "work" | Send the antiforgery token |
| `[DisableRequestSizeLimit]` or `MaxRequestBodySize = null` | Explicit limit per endpoint |
| Return the file inline with its stored content type | `fileDownloadName` (attachment) plus `nosniff` |
| "The file is validated, so it is safe" | Scan, re-encode, serve as attachment |
| Three detection libraries, or an own MIME sniffer | One inspector with an allowlist |

## Checks

- `tool (SC-CLIENT-FILENAME-PATH)`: client file name in a path.
- `tool (SC-INSPECTOR-ALL-FORMATS)`: inspector without an allowlist.
- `tool (SC-NO-SIZE-LIMIT)`: no request size limit.
- `tool (SC-DISABLE-ANTIFORGERY)`: antiforgery turned off.
- `tool`: `dotnet package list --vulnerable --include-transitive` for FileSignatures and OpenMcdf.
- `judge`: UP-1 to UP-12.

## Tests

- Unit tests of the validator with real sample files: small PNG, PDF, DOCX, DOCM, EXE, an EXE renamed to `.png`, an empty file. Keep the samples in the test project. The FileSignatures test project has a `Samples` folder (MIT).
- Matrix: allowed type and correct extension gives accepted. Allowed type and wrong extension, a type outside the list, unknown bytes, zero bytes: rejected. Too large: rejected before inspection.
- Integration test with `WebApplicationFactory`: post `MultipartFormDataContent` with the antiforgery token (or a bearer token). Assert 400, 413 or 202. Assert that no file appears in a public folder.
- Malware scan: send the EICAR test string to a ClamAV container (Testcontainers generic builder) in a slow test category.
- Do not test that FileSignatures detects each format. Test your allowlist and your mapping.
