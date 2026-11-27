using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Users;

public sealed class CredentialValidator(Data.KioskDbContext db, IPasswordHasher hasher)
{
    public async Task<User?> ValidateAsync(string email, string password, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == normalized && u.DeletedAt == null, ct);
        return user is not null && hasher.Verify(password, user.PasswordHash) ? user : null;
    }
}
