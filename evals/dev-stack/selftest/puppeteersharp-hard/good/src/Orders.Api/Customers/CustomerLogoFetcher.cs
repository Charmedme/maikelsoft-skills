using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;

namespace Orders.Api.Customers;

// Downloads the logo of a business customer one time, when the customer sets it. The URL comes from the
// customer, so the fetch is the only place where the service follows it: https only, no redirects, only public
// addresses (checked on the resolved address, at connect time), at most 256 KB, PNG or JPEG only.
public sealed class CustomerLogoFetcher(HttpClient http, ILogger<CustomerLogoFetcher> logger)
{
    public const int MaxBytes = 256 * 1024;

    public static bool IsAllowedUrl(Uri url) =>
        url.IsAbsoluteUri
        && url.Scheme == Uri.UriSchemeHttps
        && string.IsNullOrEmpty(url.UserInfo)
        && url.IsDefaultPort
        && !url.IsLoopback
        && !(IPAddress.TryParse(url.IdnHost, out var literal) && !IsPublic(literal));

    public async Task<LogoImage?> FetchAsync(Uri url, CancellationToken ct)
    {
        if (!IsAllowedUrl(url))
        {
            return null;
        }

        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            var type = response.Content.Headers.ContentType?.MediaType;
            if (!response.IsSuccessStatusCode || type is not ("image/png" or "image/jpeg") || response.Content.Headers.ContentLength > MaxBytes)
            {
                return null;
            }

            await using var body = await response.Content.ReadAsStreamAsync(ct);
            using var copy = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            while ((read = await body.ReadAsync(buffer, ct)) > 0)
            {
                if (copy.Length + read > MaxBytes)
                {
                    return null;
                }

                copy.Write(buffer, 0, read);
            }

            var bytes = copy.ToArray();
            return HasSignature(bytes, type) ? new LogoImage(bytes, type) : null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException)
        {
            logger.LogInformation(e, "The customer logo could not be downloaded.");
            return null;
        }
    }

    public static SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        ConnectCallback = async (context, ct) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            if (addresses.Length == 0 || !addresses.All(IsPublic))
            {
                throw new HttpRequestException("The logo host does not resolve to public addresses only.");
            }

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(addresses[0], context.DnsEndPoint.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 0
                || b[0] == 10
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                || (b[0] == 169 && b[1] == 254)
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || b[0] >= 224);
        }

        var v6 = address.GetAddressBytes();
        return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || (v6[0] & 0xFE) == 0xFC);
    }

    private static bool HasSignature(byte[] bytes, string type) => type switch
    {
        "image/png" => bytes.AsSpan().StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47]),
        _ => bytes.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]),
    };
}
