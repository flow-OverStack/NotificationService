using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationService.DAL;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Messaging.Events;
using NotificationService.Tests.FunctionalTests.Base;
using NotificationService.Tests.TestData;
using NotificationService.Tests.Traits;
using Xunit;

namespace NotificationService.Tests.FunctionalTests.Tests;


[FunctionalTest]
public class UserEventConsumerTests(FunctionalTestWebAppFactory factory) : SequentialFunctionalTest(factory)
{
    [Fact]
    public async Task Consume_NewEvent_CreatesNotification()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var message = new BaseEvent
        {
            EventId = eventId,
            EventType = nameof(BaseEventType.EntityUpvoted),
            EntityType = nameof(EntityType.Question),
            AuthorId = 1,
            InitiatorId = 2,
            EntityId = 10
        };
        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await using var scope = ServiceProvider.CreateAsyncScope();
        var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<BaseEvent>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        //Act
        await consumer.Consume(contextMock.Object);

        //Assert
        var userEvent = await dbContext.Set<UserEvent>().FirstOrDefaultAsync(x => x.EventId == eventId);
        Assert.NotNull(userEvent);
        Assert.Equal(1, userEvent.RecipientId);
        Assert.Equal(2, userEvent.InitiatorId);
        Assert.False(userEvent.IsRead);
    }

    [Fact]
    public async Task Consume_DuplicateEventId_CreatesNothing()
    {
        //Arrange
        var message = new BaseEvent
        {
            EventId = UserEventMother.ExistingEventId,
            EventType = nameof(BaseEventType.EntityUpvoted),
            EntityType = nameof(EntityType.Question),
            AuthorId = 3,
            InitiatorId = 4,
            EntityId = 10
        };
        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await using var scope = ServiceProvider.CreateAsyncScope();
        var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<BaseEvent>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var countBefore = await dbContext.Set<UserEvent>().CountAsync(x => x.EventId == UserEventMother.ExistingEventId);

        //Act
        await consumer.Consume(contextMock.Object);

        //Assert
        var countAfter = await dbContext.Set<UserEvent>().CountAsync(x => x.EventId == UserEventMother.ExistingEventId);
        Assert.Equal(countBefore, countAfter);
        Assert.Equal(1, countAfter);
    }

    [Fact]
    public async Task Consume_SelfNotification_CreatesNothing()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var message = new BaseEvent
        {
            EventId = eventId,
            EventType = nameof(BaseEventType.EntityUpvoted),
            EntityType = nameof(EntityType.Question),
            AuthorId = 1,
            InitiatorId = 1,
            EntityId = 10
        };
        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        await using var scope = ServiceProvider.CreateAsyncScope();
        var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<BaseEvent>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        //Act
        await consumer.Consume(contextMock.Object);

        //Assert
        var userEvent = await dbContext.Set<UserEvent>().FirstOrDefaultAsync(x => x.EventId == eventId);
        Assert.Null(userEvent);
    }
}
