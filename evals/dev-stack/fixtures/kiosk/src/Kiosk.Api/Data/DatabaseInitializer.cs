using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Data;

public sealed class DatabaseInitializer(KioskDbContext db, SeedData seed, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        await seed.ApplyAsync(ct);
        logger.LogInformation("Seeded the database with demo data.");
    }
}
