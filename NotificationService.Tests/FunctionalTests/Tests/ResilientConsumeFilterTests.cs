using Hangfire;
using MassTransit;
using MassTransit.Transports;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationService.Domain.Enums;
using NotificationService.Messaging.Events;
using NotificationService.Messaging.Filters;
using NotificationService.Tests.FunctionalTests.Base;
using NotificationService.Tests.Support;
using NotificationService.Tests.Traits;
using Serilog;
using Xunit;

namespace NotificationService.Tests.FunctionalTests.Tests;

[FunctionalTest]
public class ResilientConsumeFilterTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public void Probe_ValidProbeContext_ReturnsOk()
    {
        //Arrange
        using var scope = ServiceProvider.CreateScope();
        var backgroundJob = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var filter = new ResilientConsumeFilter<BaseEvent>(backgroundJob, new Mock<ILogger>().Object);
        var probeContext = new Mock<ProbeContext>();
        probeContext.Setup(x => x.CreateScope(It.IsAny<string>())).Returns(new Mock<ProbeContext>().Object);

        //Act
        filter.Probe(probeContext.Object);

        //Assert
        Assert.True(true);
    }

    [Fact]
    public async Task Send_SuccessfulPipe_ReturnsOk()
    {
        //Arrange
        const long userId = 1;
        const long entityId = 2;
        const long initiatorId = 2;

        var message = new BaseEvent
        {
            EventType = nameof(BaseEventType.EntityAccepted),
            EntityType = nameof(EntityType.Answer),
            AuthorId = userId,
            InitiatorId = initiatorId,
            EntityId = entityId,
            EventId = Guid.NewGuid()
        };
        await using var scope = ServiceProvider.CreateAsyncScope();
        var backgroundJob = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var filter = new ResilientConsumeFilter<BaseEvent>(backgroundJob, new Mock<ILogger>().Object);

        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.Headers)
            .Returns(new JsonTransportHeaders(new DictionaryHeaderProvider(new Dictionary<string, object>())));

        var pipeMock = new Mock<IPipe<ConsumeContext<BaseEvent>>>();

        //Act
        await filter.Send(contextMock.Object, pipeMock.Object);

        //Assert
        Assert.True(true);
    }

    [Fact]
    public async Task Send_PipeThrowsRepeatedly_ThrowsTestException()
    {
        //Arrange
        const long userId = 1;
        const long entityId = 2;
        const long initiatorId = 2;

        var message = new BaseEvent
        {
            EventType = nameof(BaseEventType.EntityAccepted),
            EntityType = nameof(EntityType.Answer),
            AuthorId = userId,
            InitiatorId = initiatorId,
            EntityId = entityId,
            EventId = Guid.NewGuid()
        };

        await using var scope = ServiceProvider.CreateAsyncScope();
        var backgroundJob = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var loggerMock = new Mock<ILogger>();
        var filter = new ResilientConsumeFilter<BaseEvent>(backgroundJob, loggerMock.Object);

        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.Headers)
            .Returns(new JsonTransportHeaders(new DictionaryHeaderProvider(new Dictionary<string, object>())));

        var pipeMock = new Mock<IPipe<ConsumeContext<BaseEvent>>>();
        pipeMock.Setup(x => x.Send(It.IsAny<ConsumeContext<BaseEvent>>())).ThrowsAsync(new TestException());

        //Act
        var action = async () => await filter.Send(contextMock.Object, pipeMock.Object);

        //Assert
        await Assert.ThrowsAsync<TestException>(action);
        // Every failed immediate retry attempt must be logged, not silently swallowed.
        Assert.Equal(4, loggerMock.Invocations.Count(i => i.Method.Name == nameof(ILogger.Warning)));
    }

    [Fact]
    public async Task Send_PipeThrowsOnceThenSucceeds_ReturnsOk()
    {
        //Arrange
        const long userId = 1;
        const long entityId = 2;
        const long initiatorId = 2;

        var message = new BaseEvent
        {
            EventType = nameof(BaseEventType.EntityAccepted),
            EntityType = nameof(EntityType.Answer),
            AuthorId = userId,
            InitiatorId = initiatorId,
            EntityId = entityId,
            EventId = Guid.NewGuid()
        };
        await using var scope = ServiceProvider.CreateAsyncScope();
        var backgroundJob = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var filter = new ResilientConsumeFilter<BaseEvent>(backgroundJob, new Mock<ILogger>().Object);

        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.Headers)
            .Returns(new JsonTransportHeaders(new DictionaryHeaderProvider(new Dictionary<string, object>())));

        var isThrown = false;
        var pipeMock = new Mock<IPipe<ConsumeContext<BaseEvent>>>();
        pipeMock.Setup(x => x.Send(It.IsAny<ConsumeContext<BaseEvent>>()))
            .Returns((ConsumeContext<BaseEvent> _) =>
            {
                if (isThrown)
                    return Task.CompletedTask;

                isThrown = true;
                throw new TestException();
            });

        //Act
        await filter.Send(contextMock.Object, pipeMock.Object);

        //Assert
        Assert.True(true);
    }

    [Fact]
    public async Task Send_MaxRedeliveryCountExceeded_ThrowsTestException()
    {
        //Arrange
        const long userId = 1;
        const long entityId = 2;
        const long initiatorId = 2;

        var message = new BaseEvent
        {
            EventType = nameof(BaseEventType.EntityAccepted),
            EntityType = nameof(EntityType.Answer),
            AuthorId = userId,
            InitiatorId = initiatorId,
            EntityId = entityId,
            EventId = Guid.NewGuid()
        };
        await using var scope = ServiceProvider.CreateAsyncScope();
        var backgroundJob = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var filter = new ResilientConsumeFilter<BaseEvent>(backgroundJob, new Mock<ILogger>().Object);

        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.Headers)
            .Returns(new JsonTransportHeaders(new DictionaryHeaderProvider(new Dictionary<string, object>
                { { "RedeliveryCount", "6" } })));
        contextMock.Setup(x => x.DestinationAddress).Returns(new Uri("queue:test-queue"));
        var pipeMock = new Mock<IPipe<ConsumeContext<BaseEvent>>>();
        pipeMock.Setup(x => x.Send(It.IsAny<ConsumeContext<BaseEvent>>())).ThrowsAsync(new TestException());

        //Act
        var action = async () => await filter.Send(contextMock.Object, pipeMock.Object);

        //Assert
        await Assert.ThrowsAsync<TestException>(action);
    }
}
