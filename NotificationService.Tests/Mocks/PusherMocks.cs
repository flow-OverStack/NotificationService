using Moq;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Tests.Support;

namespace NotificationService.Tests.Mocks;

internal static class PusherMocks
{
    public static IMock<INotificationPusher> GetMockNotificationPusher()
    {
        var mockPusher = new Mock<INotificationPusher>();

        mockPusher.Setup(x =>
            x.PushAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()));

        return mockPusher;
    }

    public static IMock<INotificationPusher> GetThrowingMockNotificationPusher()
    {
        var mockPusher = new Mock<INotificationPusher>();

        mockPusher.Setup(x => x.PushAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestException());

        return mockPusher;
    }

    public static IMock<INotificationPusher> GetCancellingMockNotificationPusher()
    {
        var mockPusher = new Mock<INotificationPusher>();

        mockPusher.Setup(x => x.PushAsync(It.IsAny<long>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        return mockPusher;
    }
}