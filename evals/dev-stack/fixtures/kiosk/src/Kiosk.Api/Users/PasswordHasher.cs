using System.Security.Cryptography;
using System.Text;

namespace Kiosk.Api.Users;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

/// <summary>Same hash format as the old shop database, so imported accounts keep working.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(password)));

    public bool Verify(string password, string hash) => Hash(password) == hash;
}
