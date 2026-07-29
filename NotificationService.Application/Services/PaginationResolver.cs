using FluentValidation;
using Microsoft.Extensions.Options;
using NotificationService.Application.Enums;
using NotificationService.Application.Resources;
using NotificationService.Application.Settings;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Interface.Service;
using NotificationService.Domain.Results;

namespace NotificationService.Application.Services;

public class PaginationResolver(
    IValidator<PaginationParams> validator,
    IOptions<PaginationRules> paginationOptions) : IPaginationResolver
{
    private readonly PaginationRules _paginationRules = paginationOptions.Value;

    public async Task<BaseResult<PaginationParams>> ResolveAsync(PaginationParams paginationParams,
        CancellationToken cancellationToken = default)
    {
        var resolved = new PaginationParams(paginationParams.Take ?? _paginationRules.DefaultPageSize,
            paginationParams.Skip ?? 0);

        var validation = await validator.ValidateAsync(resolved, cancellationToken);
        if (!validation.IsValid)
        {
            var message = $"{ErrorMessage.InvalidPagination}: " +
                          string.Join(' ', validation.Errors.Select(e => e.ErrorMessage));
            return BaseResult<PaginationParams>.Failure(message, (int)ErrorCodes.InvalidPagination);
        }

        return BaseResult<PaginationParams>.Success(resolved);
    }
}
