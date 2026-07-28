using Microsoft.AspNetCore.SignalR;
using Moq;
using NotificationService.Api.Hubs;
using NotificationService.Api.Hubs.Clients;
using NotificationService.Api.Services;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Tests.Traits;
using Xunit;

namespace NotificationService.Tests.UnitTests.Tests;

[UnitTest]
public class SignalRNotificationPusherTests
{
    [Fact]
    public async Task PushAsync_ValidDto_SendsToRecipientUser()
    {
        //Arrange
        const long recipientId = 42;
        var dto = new NotificationDto(1, 2, "EntityUpvoted", "Question", 10, false, DateTime.UtcNow);

        var clientMock = new Mock<INotificationClient>();
        var clientsMock = new Mock<IHubClients<INotificationClient>>();
        clientsMock.Setup(x => x.User(recipientId.ToString())).Returns(clientMock.Object);
        var hubContextMock = new Mock<IHubContext<NotificationHub, INotificationClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);

        var pusher = new SignalRNotificationPusher(hubContextMock.Object);

        //Act
        await pusher.PushAsync(recipientId, dto);

        //Assert
        clientsMock.Verify(x => x.User(recipientId.ToString()), Times.Once);
        clientMock.Verify(x => x.ReceiveNotification(dto), Times.Once);
    }
}
