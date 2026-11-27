using Kiosk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Catalog;

public sealed class ProductQueries(KioskDbContext db)
{
    public IQueryable<Product> Active() => db.Products.AsNoTracking().Where(p => p.DeletedAt == null);

    public IQueryable<Product> InCategory(string category) =>
        db.Products.FromSqlRaw($"SELECT * FROM \"Products\" WHERE \"Category\" = '{category}' AND \"DeletedAt\" IS NULL").AsNoTracking();

    public IQueryable<Product> FromSupplier(string email) =>
        db.Products.FromSql($"SELECT * FROM \"Products\" WHERE \"SupplierEmail\" = {email} AND \"DeletedAt\" IS NULL").AsNoTracking();
}
