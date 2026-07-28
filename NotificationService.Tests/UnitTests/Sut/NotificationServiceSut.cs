using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Options;
using NotificationService.Application.Settings;
using NotificationService.Application.Validators;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interface.Repository;
using NotificationService.Domain.Interface.Service;
using NotificationService.Tests.Mocks;
using NotificationService.Tests.UnitTests.Fixtures;

namespace NotificationService.Tests.UnitTests.Sut;

internal class NotificationServiceSut
{
    private readonly Application.Services.NotificationService _notificationService;

    public readonly IMapper Mapper = MapperFixture.GetMapperConfiguration();
    public readonly IOptions<PaginationRules> PaginationRules = PaginationRulesFixture.GetPaginationRules();
    public readonly INotificationPusher Pusher;
    public readonly IBaseRepository<UserEvent> UserEventRepository;

    public readonly IValidator<PaginationParams> Validator =
        ValidatorFixture<PaginationParams>.GetValidator(
            new PaginationParamsValidator(PaginationRulesFixture.GetPaginationRules()));

    public NotificationServiceSut(IBaseRepository<UserEvent>? userEventRepository = null,
        INotificationPusher? pusher = null)
    {
        UserEventRepository = userEventRepository ?? RepositoryMocks.GetMockUserEventRepository().Object;
        Pusher = pusher ?? PusherMocks.GetMockNotificationPusher().Object;

        _notificationService = new Application.Services.NotificationService(UserEventRepository, Pusher, Mapper,
            Validator, PaginationRules);
    }

    public INotificationService GetService()
    {
        return _notificationService;
    }

    public INotificationEventHandler GetEventHandler()
    {
        return _notificationService;
    }
}
