using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Results;

namespace NotificationService.Domain.Interface.Service;

public interface IPaginationResolver
{
    /// <summary>
    ///     Applies configured defaults to any unset member of <paramref name="paginationParams" /> and validates
    ///     the result. On success the returned params have non-null <c>Skip</c> and <c>Take</c>.
    /// </summary>
    Task<BaseResult<PaginationParams>> ResolveAsync(PaginationParams paginationParams,
        CancellationToken cancellationToken = default);
}
