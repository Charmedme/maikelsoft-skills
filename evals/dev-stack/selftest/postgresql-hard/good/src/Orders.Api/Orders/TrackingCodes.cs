using System.Security.Cryptography;

namespace Orders.Api.Orders;

public static class TrackingCodes
{
    public const int Length = 12;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>A random code of 12 characters (60 bits) from a cryptographic random source.</summary>
    public static string New() => RandomNumberGenerator.GetString(Alphabet, Length);
}
