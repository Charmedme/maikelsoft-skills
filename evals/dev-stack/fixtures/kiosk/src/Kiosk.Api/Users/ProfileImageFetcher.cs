using Kiosk.Api.Files;
using Microsoft.Extensions.Options;

namespace Kiosk.Api.Users;

public sealed class ProfileImageFetcher(HttpClient http, IOptions<FileStorageOptions> storage, IWebHostEnvironment environment)
{
    private const int MaxBytes = 2 * 1024 * 1024;

    public async Task<string> DownloadAsync(int userId, Uri url, CancellationToken ct)
    {
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
