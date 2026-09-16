namespace NotificationService.Domain.Interfaces.Database;

public interface IStateSaveChanges
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}