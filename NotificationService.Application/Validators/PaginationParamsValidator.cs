using FluentValidation;
using Microsoft.Extensions.Options;
using NotificationService.Application.Resources;
using NotificationService.Application.Settings;
using NotificationService.Domain.Dtos.Pagination;

namespace NotificationService.Application.Validators;

public class PaginationParamsValidator : AbstractValidator<PaginationParams>
{
    public PaginationParamsValidator(IOptions<PaginationRules> pagination)
    {
        var maxPageSize = pagination.Value.MaxPageSize;

        RuleFor(x => x.Skip)
            .NotNull().WithMessage(_ => string.Format(ErrorMessage.Required, nameof(PaginationParams.Skip)))
            .GreaterThanOrEqualTo(0)
            .WithMessage(_ => string.Format(ErrorMessage.InvalidMinValue, nameof(PaginationParams.Skip), 0));

        RuleFor(x => x.Take)
            .NotNull().WithMessage(_ => string.Format(ErrorMessage.Required, nameof(PaginationParams.Take)))
            .InclusiveBetween(0, maxPageSize)
            .WithMessage(_ => string.Format(ErrorMessage.InvalidRange, nameof(PaginationParams.Take), maxPageSize));
    }
}