using System.Net;
using System.Net.Sockets;

namespace Orders.Api.Customers;

// Downloads the logo of a business customer one time, when the customer sets it. Documents use the stored copy,
// so Chrome never loads a customer URL.
public sealed class CustomerLogoFetcher(HttpClient http, ILogger<CustomerLogoFetcher> logger)
{
    public async Task<LogoImage?> FetchAsync(Uri url, CancellationToken ct)
    {
        if (!url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        // Refuse hosts that resolve to our own network (SSRF).
        var addresses = await Dns.GetHostAddressesAsync(url.IdnHost, ct);
        if (addresses.Length == 0 || addresses.Any(IsInternal))
        {
            return null;
        }

        try
        {
            using var response = await http.GetAsync(url, ct);
            var type = response.Content.Headers.ContentType?.MediaType;
            if (!response.IsSuccessStatusCode || type is not ("image/png" or "image/jpeg"))
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return new LogoImage(bytes, type);
        }
        catch (HttpRequestException e)
        {
            logger.LogInformation(e, "The customer logo could not be downloaded.");
            return null;
        }
    }

    public static bool IsInternal(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
        {
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
        }

        var b = address.GetAddressBytes();
        return b[0] == 0
            || b[0] == 10
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
            || (b[0] == 169 && b[1] == 254)
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168);
    }
}
