using System.Net;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationService.Api.Hubs;
using NotificationService.Api.Hubs.Clients;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Enums;
using NotificationService.Messaging.Events;
using NotificationService.Tests.FunctionalTests.Base;
using NotificationService.Tests.FunctionalTests.Helpers;
using NotificationService.Tests.Traits;
using Xunit;

namespace NotificationService.Tests.FunctionalTests.Tests;

[FunctionalTest]
public class RealtimeTests(FunctionalTestWebAppFactory factory) : SequentialFunctionalTest(factory)
{
    [Fact]
    public async Task PushViaHubContext_ConnectedUser_DeliversNotification()
    {
        //Arrange
        const long recipientId = 1;
        var token = TokenHelper.GetRsaToken("testuser1", recipientId, ["User"]);
        await using var connection = HubConnectionHelper.BuildConnection(factory, token);

        var tcs = new TaskCompletionSource<NotificationDto>();
        connection.On<NotificationDto>(nameof(INotificationClient.ReceiveNotification), dto => tcs.TrySetResult(dto));

        await connection.StartAsync();

        var dto = new NotificationDto(1, 2, "EntityUpvoted", "Question", 10, false, DateTime.UtcNow);

        try
        {
            //Act
            await using (var scope = ServiceProvider.CreateAsyncScope())
            {
                var hubContext = scope.ServiceProvider
                    .GetRequiredService<IHubContext<NotificationHub, INotificationClient>>();
                await hubContext.Clients.User(recipientId.ToString()).ReceiveNotification(dto);
            }

            var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15));

            //Assert
            Assert.Equal(dto.EventType, received.EventType);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Negotiate_NoToken_ReturnsUnauthorized()
    {
        //Arrange & Act
        var response = await HttpClient.PostAsync("/hubs/notifications/negotiate?negotiateVersion=1", null);

        //Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Connect_ValidToken_ReturnsConnected()
    {
        //Arrange
        var token = TokenHelper.GetRsaToken("testuser1", 1, ["User"]);
        await using var connection = HubConnectionHelper.BuildConnection(factory, token);

        try
        {
            //Act
            await connection.StartAsync();

            //Assert
            Assert.Equal(HubConnectionState.Connected, connection.State);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Connect_TokenMissingRequiredClaim_ThrowsHttpRequestException()
    {
        //Arrange
        var token = TokenHelper.GetRsaTokenMissingRole("testuser1", 1);
        await using var connection = HubConnectionHelper.BuildConnection(factory, token);

        //Act
        var action = async () => await connection.StartAsync();

        //Assert
        // The access token arrives via the query string (browsers can't set headers on the WebSocket
        // handshake) but ClaimsValidationMiddleware still 403s it the same as the header path.
        await Assert.ThrowsAsync<HttpRequestException>(action);
    }

    [Fact]
    public async Task ReceiveNotification_EventForConnectedUser_DeliversNotification()
    {
        //Arrange
        const long recipientId = 1;
        var token = TokenHelper.GetRsaToken("testuser1", recipientId, ["User"]);
        await using var connection = HubConnectionHelper.BuildConnection(factory, token);

        var tcs = new TaskCompletionSource<NotificationDto>();
        connection.On<NotificationDto>(nameof(INotificationClient.ReceiveNotification), dto => tcs.TrySetResult(dto));

        await connection.StartAsync();

        var eventId = Guid.NewGuid();
        var message = new BaseEvent
        {
            EventId = eventId,
            EventType = nameof(BaseEventType.EntityUpvoted),
            EntityType = nameof(EntityType.Question),
            AuthorId = recipientId,
            InitiatorId = 2,
            EntityId = 10
        };
        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        try
        {
            //Act
            await using (var scope = ServiceProvider.CreateAsyncScope())
            {
                var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<BaseEvent>>();
                await consumer.Consume(contextMock.Object);
            }

            var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15));

            //Assert
            Assert.Equal(nameof(BaseEventType.EntityUpvoted), received.EventType);
            Assert.Equal(nameof(EntityType.Question), received.EntityType);
            Assert.Equal(10, received.EntityId);
            Assert.False(received.IsRead);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ReceiveNotification_EventForOtherUser_DeliversNothing()
    {
        //Arrange
        const long connectedUserId = 1;
        var token = TokenHelper.GetRsaToken("testuser1", connectedUserId, ["User"]);
        await using var connection = HubConnectionHelper.BuildConnection(factory, token);

        var tcs = new TaskCompletionSource<NotificationDto>();
        connection.On<NotificationDto>(nameof(INotificationClient.ReceiveNotification), dto => tcs.TrySetResult(dto));

        await connection.StartAsync();

        var message = new BaseEvent
        {
            EventId = Guid.NewGuid(),
            EventType = nameof(BaseEventType.EntityUpvoted),
            EntityType = nameof(EntityType.Question),
            AuthorId = 2, // not the connected user
            InitiatorId = 3,
            EntityId = 10
        };
        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        try
        {
            //Act
            await using (var scope = ServiceProvider.CreateAsyncScope())
            {
                var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<BaseEvent>>();
                await consumer.Consume(contextMock.Object);
            }

            //Assert
            await Assert.ThrowsAsync<TimeoutException>(() => tcs.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}