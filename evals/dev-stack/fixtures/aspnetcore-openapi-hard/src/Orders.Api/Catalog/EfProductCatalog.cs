using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Catalog;

public sealed class EfProductCatalog(OrdersDbContext db) : IProductCatalog
{
    public Task<Product?> FindAsync(string sku, CancellationToken ct)
    {
        var key = Product.Normalize(sku);
        return db.Products.AsNoTracking().SingleOrDefaultAsync(product => product.Sku == key, ct);
    }

    public Task<bool> IsActiveAsync(string sku, CancellationToken ct)
    {
        var key = Product.Normalize(sku);
        return db.Products.AnyAsync(product => product.Sku == key && product.IsActive, ct);
    }
}
