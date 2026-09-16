using NotificationService.Domain.Interfaces.Repository;

namespace NotificationService.DAL.Repositories;

public class BaseRepository<TEntity>(ApplicationDbContext dbContext) : IBaseRepository<TEntity>
    where TEntity : class
{
    public IQueryable<TEntity> GetAll()
    {
        return dbContext.Set<TEntity>();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await dbContext.AddAsync(entity, cancellationToken);

        return entity;
    }

    public TEntity Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        dbContext.Update(entity);

        return entity;
    }

    public TEntity Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        dbContext.Remove(entity);

        return entity;
    }
}