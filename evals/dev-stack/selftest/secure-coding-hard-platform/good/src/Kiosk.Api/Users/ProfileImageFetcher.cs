using Kiosk.Api.Files;
using Microsoft.Extensions.Options;

namespace Kiosk.Api.Users;

public sealed class ProfileImageFetcher(HttpClient http, IOptions<FileStorageOptions> storage, IWebHostEnvironment environment, IConfiguration configuration)
{
    private const int MaxBytes = 2 * 1024 * 1024;

    public async Task<string> DownloadAsync(int userId, Uri url, CancellationToken ct)
    {
        // Only images from the approved image hosts, over HTTPS. The server never fetches an address that the user chose freely.
        var allowedHosts = configuration.GetSection("ProfileImages:AllowedHosts").Get<string[]>() ?? [];
        if (url.Scheme != Uri.UriSchemeHttps || !allowedHosts.Contains(url.IdnHost, StringComparer.OrdinalIgnoreCase))
        {
            throw new HttpRequestException("The image host is not allowed.");
        }

        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length > MaxBytes)
        {
            throw new HttpRequestException("The image is too large.");
        }

        var extension = Path.GetExtension(url.AbsolutePath) is { Length: > 1 and < 6 } ext ? ext.ToLowerInvariant() : ".png";
        var folder = Directory.CreateDirectory(storage.Value.Resolve(environment.ContentRootPath, storage.Value.ProfileImageRoot));
        var fileName = $"{userId}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(folder.FullName, fileName), bytes, ct);
        return $"/profile-images/{fileName}";
    }
}
