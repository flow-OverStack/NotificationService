using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Application.Settings;
using NotificationService.Application.Validators;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Entities;
using NotificationService.Application.Services;
using NotificationService.Domain.Interfaces.Repository;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Tests.Mocks;
using NotificationService.Tests.UnitTests.Fixtures;
using Serilog;

namespace NotificationService.Tests.UnitTests.Sut;

internal class NotificationServiceSut
{
    private readonly Application.Services.NotificationService _notificationService;

    public readonly Mock<ILogger> LoggerMock = new();
    public readonly IMapper Mapper = MapperFixture.GetMapperConfiguration();
    public readonly IOptions<PaginationRules> PaginationRules = PaginationRulesFixture.GetPaginationRules();
    public readonly IPaginationResolver PaginationResolver;
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
        PaginationResolver = new PaginationResolver(Validator, PaginationRules);

        _notificationService = new Application.Services.NotificationService(UserEventRepository, Pusher, Mapper,
            LoggerMock.Object);
    }

    /// <summary>Returns the full decorator chain (pagination resolution, then the raw service).</summary>
    public INotificationService GetService()
    {
        return new PaginationResolvingNotificationService(PaginationResolver, _notificationService);
    }

    /// <summary>Returns the raw service, without the pagination-resolving decorator.</summary>
    public INotificationService GetInnerService()
    {
        return _notificationService;
    }

    public INotificationEventHandler GetEventHandler()
    {
        return _notificationService;
    }
}
