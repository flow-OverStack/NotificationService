using AutoMapper;
using Moq;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repository;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Tests.Mocks;
using NotificationService.Tests.UnitTests.Fixtures;
using Serilog;

namespace NotificationService.Tests.UnitTests.Sut;

internal class NotificationServiceSut
{
    private readonly Application.Services.NotificationService _notificationService;

    public readonly ILogger Logger = new Mock<ILogger>().Object;
    public readonly IMapper Mapper = MapperFixture.GetMapperConfiguration();
    public readonly INotificationPusher Pusher;
    public readonly IBaseRepository<UserEvent> UserEventRepository;

    public NotificationServiceSut(IBaseRepository<UserEvent>? userEventRepository = null,
        INotificationPusher? pusher = null)
    {
        UserEventRepository = userEventRepository ?? RepositoryMocks.GetMockUserEventRepository().Object;
        Pusher = pusher ?? PusherFixture.GetMockNotificationPusher();

        _notificationService =
            new Application.Services.NotificationService(UserEventRepository, Pusher, Mapper, Logger);
    }

    /// <summary>Returns the raw service, without any decorator.</summary>
    public INotificationService GetService()
    {
        return _notificationService;
    }

    public INotificationEventHandler GetEventHandler()
    {
        return _notificationService;
    }
}
