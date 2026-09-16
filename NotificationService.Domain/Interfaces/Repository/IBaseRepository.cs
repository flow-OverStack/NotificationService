using NotificationService.Domain.Interfaces.Database;

namespace NotificationService.Domain.Interfaces.Repository;

public interface IBaseRepository<TEntity> : IStateSaveChanges
{
    IQueryable<TEntity> GetAll();

    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    TEntity Update(TEntity entity);

    TEntity Remove(TEntity entity);
}