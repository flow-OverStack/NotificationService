using Moq;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Enums;
using NotificationService.Tests.Traits;
using NotificationService.Tests.UnitTests.Fixtures;
using NotificationService.Tests.UnitTests.Sut;
using Serilog;
using Xunit;

namespace NotificationService.Tests.UnitTests.Tests;

[UnitTest]
public class CacheNotificationServiceTests
{
    [Fact]
    public async Task GetAllByRecipientIdAsync_InnerFailure_DoesNotCacheResult()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut();
        var service = sut.GetService();
        var paginationParams = new PaginationParams(101, 0); // above PaginationRules.MaxPageSize -> InvalidPagination

        //Act
        await service.GetAllByRecipientIdAsync(1, false, paginationParams);
        var cached = await sut.CacheRepository.GetAsync(1, false, paginationParams.Skip, paginationParams.Take);

        //Assert
        Assert.Null(cached);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_Success_CachesResult()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut();
        var service = sut.GetService();
        var paginationParams = new PaginationParams(10, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, paginationParams);
        var cached = await sut.CacheRepository.GetAsync(1, false, paginationParams.Skip, paginationParams.Take);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(cached);
        Assert.Equal(result.Data!.Select(x => x.Id), cached.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_CacheHit_ReturnsCachedDataInsteadOfInner()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut();
        var service = sut.GetService();
        var paginationParams = new PaginationParams(10, 0);
        var fabricated = new[] { new NotificationDto(999, 1, "Fake", "Fake", 1, false, DateTime.UtcNow) };
        await sut.CacheRepository.SetAsync(1, false, paginationParams.Skip, paginationParams.Take, fabricated);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([999L], result.Data!.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_CacheThrows_FallsBackToInnerResult()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut(RedisDatabaseFixture.GetThrowingRedisDatabaseConfiguration());
        var service = sut.GetService();
        var paginationParams = new PaginationParams(10, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_CacheThrows_LogsWarning()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut(RedisDatabaseFixture.GetThrowingRedisDatabaseConfiguration());
        var service = sut.GetService();
        var paginationParams = new PaginationParams(10, 0);

        //Act
        await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        sut.LoggerMock.Verify(x => x.Warning(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_CacheThrowsNonRedisException_Propagates()
    {
        //Arrange
        var sut =
            new CacheNotificationServiceSut(RedisDatabaseFixture.GetNonRedisThrowingRedisDatabaseConfiguration());
        var service = sut.GetService();
        var paginationParams = new PaginationParams(10, 0);

        //Act
        var action = async () => await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    [Fact]
    public async Task MarkAsReadAsync_Success_InvalidatesRecipientCache()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut();
        var service = sut.GetService();
        var paginationParams = new PaginationParams(10, 0);
        var fabricated = new[] { new NotificationDto(1, 2, "Fake", "Fake", 1, false, DateTime.UtcNow) };
        await sut.CacheRepository.SetAsync(1, false, paginationParams.Skip, paginationParams.Take, fabricated);

        //Act
        var result = await service.MarkAsReadAsync(1, 1);
        var cached = await sut.CacheRepository.GetAsync(1, false, paginationParams.Skip, paginationParams.Take);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Null(cached);
    }

    [Fact]
    public async Task MarkAsReadAsync_Forbidden_DoesNotInvalidateCache()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut();
        var service = sut.GetService();
        var paginationParams = new PaginationParams(10, 0);
        var fabricated = new[] { new NotificationDto(1, 2, "Fake", "Fake", 1, false, DateTime.UtcNow) };
        await sut.CacheRepository.SetAsync(1, false, paginationParams.Skip, paginationParams.Take, fabricated);

        //Act
        var result = await service.MarkAsReadAsync(1, 2); // event 1 belongs to recipient 1, not 2

        var cached = await sut.CacheRepository.GetAsync(1, false, paginationParams.Skip, paginationParams.Take);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task MarkAsReadAsync_CacheThrows_StillReturnsInnerResult()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut(RedisDatabaseFixture.GetThrowingRedisDatabaseConfiguration());
        var service = sut.GetService();

        //Act
        var result = await service.MarkAsReadAsync(1, 1);

        //Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_Success_InvalidatesRecipientCache()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut();
        var handler = sut.GetEventHandler();
        var paginationParams = new PaginationParams(10, 0);
        var fabricated = new[] { new NotificationDto(1, 2, "Fake", "Fake", 1, false, DateTime.UtcNow) };
        await sut.CacheRepository.SetAsync(5, false, paginationParams.Skip, paginationParams.Take, fabricated);
        var dto = new UserEventDto(Guid.NewGuid(), 5, 6, BaseEventType.EntityUpvoted, EntityType.Question, 10);

        //Act
        var result = await handler.CreateAsync(dto);
        var cached = await sut.CacheRepository.GetAsync(5, false, paginationParams.Skip, paginationParams.Take);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Null(cached);
    }

    [Fact]
    public async Task CreateAsync_CacheThrows_StillReturnsInnerResult()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut(RedisDatabaseFixture.GetThrowingRedisDatabaseConfiguration());
        var handler = sut.GetEventHandler();
        var dto = new UserEventDto(Guid.NewGuid(), 1, 2, BaseEventType.EntityUpvoted, EntityType.Question, 10);

        //Act
        var result = await handler.CreateAsync(dto);

        //Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetAll_InvalidPagination_ReturnsFailureWithoutCaching()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut();
        var service = sut.GetService();
        var canonicalParams = new PaginationParams(PaginationRulesFixture.MaxPageSize, 0);
        var fabricated = new[] { new NotificationDto(999, 1, "Fake", "Fake", 1, false, DateTime.UtcNow) };
        await sut.CacheRepository.SetAsync(1, false, canonicalParams.Skip, canonicalParams.Take, fabricated);
        var invalidParams = new PaginationParams(PaginationRulesFixture.MaxPageSize + 1, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, invalidParams);

        //Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetAll_OmittedParamsThenExplicitDefaults_ShareOneCacheEntry()
    {
        //Arrange
        var sut = new CacheNotificationServiceSut();
        var service = sut.GetService();
        var omittedParams = new PaginationParams(null, null);
        var explicitParams = new PaginationParams(PaginationRulesFixture.DefaultPageSize, 0);

        //Act
        var first = await service.GetAllByRecipientIdAsync(1, false, omittedParams);
        var cachedUnderCanonicalKey =
            await sut.CacheRepository.GetAsync(1, false, explicitParams.Skip, explicitParams.Take);
        var second = await service.GetAllByRecipientIdAsync(1, false, explicitParams);

        //Assert
        Assert.True(first.IsSuccess);
        Assert.NotNull(cachedUnderCanonicalKey);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.Select(x => x.Id), second.Data!.Select(x => x.Id));
    }
}
