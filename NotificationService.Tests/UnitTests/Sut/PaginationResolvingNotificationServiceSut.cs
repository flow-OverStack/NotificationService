using NotificationService.Application.Services;
using NotificationService.Application.Validators;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Tests.UnitTests.Fixtures;

namespace NotificationService.Tests.UnitTests.Sut;

internal class PaginationResolvingNotificationServiceSut
{
    private readonly PaginationResolvingNotificationService _service;

    public readonly NotificationServiceSut InnerSut = new();

    public PaginationResolvingNotificationServiceSut()
    {
        var paginationRules = PaginationRulesFixture.GetPaginationRules();
        var validator =
            ValidatorFixture<PaginationParams>.GetValidator(new PaginationParamsValidator(paginationRules));
        var resolver = new PaginationResolver(validator, paginationRules);

        _service = new PaginationResolvingNotificationService(resolver, InnerSut.GetService());
    }

    public INotificationService GetService()
    {
        return _service;
    }
}
