using System.Globalization;
using System.Security.Cryptography;

namespace Kiosk.Api.Wishlists;

public static class WishlistTokens
{
    private const string Alphabet = "abcdefghijkmnopqrstuvwxyz23456789";

    /// <summary>A share token that people can type from a printed card: 12 characters, no look-alike letters.</summary>
    public static string NewShareToken() => RandomNumberGenerator.GetString(Alphabet, 12);

    /// <summary>The token in the unsubscribe link of the price alert mails. It stays the same for a list.</summary>
    public static string AlertToken(int wishlistId) =>
        new Random(wishlistId).Next(100_000_000, 999_999_999).ToString(CultureInfo.InvariantCulture);
}
