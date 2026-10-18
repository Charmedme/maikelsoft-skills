using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Orders.Api.Data;

public interface IRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);

    void Add(T entity);
}

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct);
}

public sealed class Repository<T>(OrdersDbContext db) : IRepository<T>
    where T : class
{
    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Set<T>().FirstOrDefaultAsync(entity => EF.Property<Guid>(entity, "Id") == id, ct);

    public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct) =>
        db.Set<T>().Where(predicate).ToListAsync(ct);

    public void Add(T entity) => db.Set<T>().Add(entity);
}

public sealed class UnitOfWork(OrdersDbContext db) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
