using System.Net.Http.Headers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NotificationService.Cache.Helpers;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces.Repository.Cache;
using NotificationService.Domain.Results;
using NotificationService.Messaging.Events;
using NotificationService.Tests.FunctionalTests.Base;
using NotificationService.Tests.FunctionalTests.Helpers;
using NotificationService.Tests.Traits;
using StackExchange.Redis;
using Xunit;

namespace NotificationService.Tests.FunctionalTests.Tests;

[FunctionalTest]
public class CacheNotificationServiceTests : SequentialFunctionalTest
{
    public CacheNotificationServiceTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        var token = TokenHelper.GetRsaToken("testuser1", 1, ["User"]);
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_SecondRequest_ServedFromCache()
    {
        //Arrange
        await ClearRecipientCacheAsync();

        //Act
        var first = await GetNotificationsAsync();
        var cacheKeyExists = await CacheKeyExistsAsync();
        var second = await GetNotificationsAsync();

        //Assert
        Assert.True(first.IsSuccess);
        Assert.True(cacheKeyExists);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.Select(x => x.Id), second.Data!.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_PoisonedCacheEntry_StillReturnsFromDatabase()
    {
        //Arrange
        await ClearRecipientCacheAsync();
        var database = await GetDatabaseAsync();
        var key = CacheKeyHelper.GetRecipientNotificationsKey(1, false, 0, 10);
        await database.StringSetAsync(key, "not valid json");

        //Act
        var result = await GetNotificationsAsync();

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task MarkAsRead_OwnEvent_InvalidatesRecipientCache()
    {
        //Arrange
        await ClearRecipientCacheAsync();
        await GetNotificationsAsync(); // populates the cache with the stale (unread) state

        //Act
        await HttpClient.PatchAsync("/api/v1.0/notification/1/read", null);
        var result = await GetNotificationsAsync();

        //Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.Single(x => x.Id == 1).IsRead);
    }

    [Fact]
    public async Task Consume_NewEvent_InvalidatesRecipientCache()
    {
        //Arrange
        await ClearRecipientCacheAsync();
        await GetNotificationsAsync(); // populates the cache without the new event

        var message = new BaseEvent
        {
            EventId = Guid.NewGuid(),
            EventType = nameof(BaseEventType.EntityUpvoted),
            EntityType = nameof(EntityType.Question),
            AuthorId = 1,
            InitiatorId = 2,
            EntityId = 10
        };
        var contextMock = new Mock<ConsumeContext<BaseEvent>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        //Act
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<BaseEvent>>();
            await consumer.Consume(contextMock.Object);
        }

        var result = await GetNotificationsAsync();

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task GetAll_OmittedPaginationParams_UsesCanonicalCacheKey()
    {
        //Arrange
        await ClearRecipientCacheAsync();

        //Act
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=false");
        var database = await GetDatabaseAsync();
        var canonicalKeyExists =
            await database.KeyExistsAsync(CacheKeyHelper.GetRecipientNotificationsKey(1, false, 0, 20));
        var rawKeyExists =
            await database.KeyExistsAsync(CacheKeyHelper.GetRecipientNotificationsKey(1, false, null, null));

        //Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.True(canonicalKeyExists);
        Assert.False(rawKeyExists);
    }

    private async Task<CollectionResult<NotificationDto>> GetNotificationsAsync()
    {
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=false&Take=10&Skip=0");
        var body = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<CollectionResult<NotificationDto>>(body)!;
    }

    private async Task<bool> CacheKeyExistsAsync()
    {
        var database = await GetDatabaseAsync();
        var key = CacheKeyHelper.GetRecipientNotificationsKey(1, false, 0, 10);
        return await database.KeyExistsAsync(key);
    }

    private async Task<IDatabase> GetDatabaseAsync()
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<IDatabase>();
    }

    private async Task ClearRecipientCacheAsync()
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var cacheRepository = scope.ServiceProvider.GetRequiredService<INotificationCacheRepository>();
        await cacheRepository.InvalidateAsync(1);
    }
}
