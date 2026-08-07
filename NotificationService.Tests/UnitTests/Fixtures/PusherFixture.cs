using Moq;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Tests.Support;

namespace NotificationService.Tests.UnitTests.Fixtures;

internal static class PusherFixture
{
    public static INotificationPusher GetMockNotificationPusher()
    {
        var mockPusher = new Mock<INotificationPusher>();

        mockPusher.Setup(x =>
            x.PushAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()));

        return mockPusher.Object;
    }

    public static INotificationPusher GetThrowingMockNotificationPusher()
    {
        var mockPusher = new Mock<INotificationPusher>();

        mockPusher.Setup(x => x.PushAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestException());

        return mockPusher.Object;
    }

    public static INotificationPusher GetCancellingMockNotificationPusher()
    {
        var mockPusher = new Mock<INotificationPusher>();

        mockPusher.Setup(x => x.PushAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        return mockPusher.Object;
    }
}