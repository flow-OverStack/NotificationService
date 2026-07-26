using FluentValidation;
using Microsoft.Extensions.Options;
using NotificationService.Application.Settings;
using NotificationService.Domain.Dtos.Pagination;

namespace NotificationService.Application.Validators;

public class PaginationParamsValidator  : AbstractValidator<PaginationParams>
{
    public PaginationParamsValidator(IOptions<PaginationRules> pagination)
    {
        var maxPageSize = pagination.Value.MaxPageSize;

        RuleFor(x => x.Skip)
            .NotNull().WithMessage($"'{nameof(PaginationParams.Skip)}' must be provided.")
            .GreaterThanOrEqualTo(0).WithMessage($"'{nameof(PaginationParams.Skip)}' must be greater than or equal to 0.");

        RuleFor(x => x.Take)
            .NotNull().WithMessage($"'{nameof(PaginationParams.Take)}' must be provided.")
            .InclusiveBetween(0, maxPageSize)
            .WithMessage($"'{nameof(PaginationParams.Take)}' must be between 0 and {maxPageSize}.");
    }
}