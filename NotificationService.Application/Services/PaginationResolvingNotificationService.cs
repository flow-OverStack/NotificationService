using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Interface.Service;
using NotificationService.Domain.Results;

namespace NotificationService.Application.Services;

/// <summary>
///     Resolves pagination parameters once, at the front of the <see cref="INotificationService" /> decorator
///     chain, so downstream decorators (cache) and the base service work with already-resolved values instead
///     of each resolving independently.
/// </summary>
public class PaginationResolvingNotificationService(
    IPaginationResolver resolver,
    INotificationService inner) : INotificationService
{
    public Task<BaseResult<NotificationDto>> MarkAsReadAsync(long id, long userId,
        CancellationToken cancellationToken = default)
    {
        return inner.MarkAsReadAsync(id, userId, cancellationToken);
    }

    public async Task<CollectionResult<NotificationDto>> GetAllByRecipientIdAsync(long recipientId, bool unreadOnly,
        PaginationParams paginationParams, CancellationToken cancellationToken = default)
    {
        var resolved = await resolver.ResolveAsync(paginationParams, cancellationToken);
        if (!resolved.IsSuccess)
            return CollectionResult<NotificationDto>.Failure(resolved.ErrorMessage!, resolved.ErrorCode);

        return await inner.GetAllByRecipientIdAsync(recipientId, unreadOnly, resolved.Data, cancellationToken);
    }
}
