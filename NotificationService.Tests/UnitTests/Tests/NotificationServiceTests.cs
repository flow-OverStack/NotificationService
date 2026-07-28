using NotificationService.Application.Resources;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Enums;
using NotificationService.Tests.Mocks;
using NotificationService.Tests.TestData;
using NotificationService.Tests.Traits;
using NotificationService.Tests.UnitTests.Sut;
using Xunit;

namespace NotificationService.Tests.UnitTests.Tests;

[UnitTest]
public class NotificationServiceTests
{
    [Fact]
    public async Task CreateAsync_NewEvent_ReturnsSuccess()
    {
        //Arrange
        var handler = new NotificationServiceSut().GetEventHandler();
        var dto = new UserEventDto(Guid.NewGuid(), 1, 2, BaseEventType.EntityUpvoted, EntityType.Question, 10);

        //Act
        var result = await handler.CreateAsync(dto);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEventId_ReturnsUserEventAlreadyExists()
    {
        //Arrange
        var handler = new NotificationServiceSut().GetEventHandler();
        var dto = new UserEventDto(UserEventMother.ExistingEventId, 1, 2, BaseEventType.EntityUpvoted,
            EntityType.Question, 10);

        //Act
        var result = await handler.CreateAsync(dto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessage.UserEventAlreadyExists, result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CreateAsync_RecipientEqualsInitiator_ReturnsSelfNotificationNotAllowed()
    {
        //Arrange
        var handler = new NotificationServiceSut().GetEventHandler();
        var dto = new UserEventDto(Guid.NewGuid(), 1, 1, BaseEventType.EntityUpvoted, EntityType.Question, 10);

        //Act
        var result = await handler.CreateAsync(dto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessage.SelfNotificationNotAllowed, result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CreateAsync_PusherThrows_ReturnsSuccess()
    {
        //Arrange
        var sut = new NotificationServiceSut(pusher: PusherMocks.GetThrowingMockNotificationPusher().Object);
        var handler = sut.GetEventHandler();
        var dto = new UserEventDto(Guid.NewGuid(), 1, 2, BaseEventType.EntityUpvoted, EntityType.Question, 10);

        //Act
        var result = await handler.CreateAsync(dto);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task MarkAsReadAsync_OwnEvent_ReturnsSuccess()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();

        //Act
        var result = await service.MarkAsReadAsync(1, 1);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_NonExistentEvent_ReturnsUserEventNotFound()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();

        //Act
        var result = await service.MarkAsReadAsync(999, 1);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessage.UserEventNotFound, result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task MarkAsReadAsync_OtherUsersEvent_ReturnsOperationForbidden()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();

        //Act
        var result = await service.MarkAsReadAsync(1, 2);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessage.OperationForbidden, result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_ValidParams_ReturnsSuccess()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();
        var paginationParams = new PaginationParams(10, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_UnreadOnly_ReturnsUnreadEvents()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();
        var paginationParams = new PaginationParams(10, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, true, paginationParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Count);
        Assert.All(result.Data!, x => Assert.False(x.IsRead));
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_NullTake_UsesDefaultPageSize()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();
        var paginationParams = new PaginationParams(null, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_NegativeSkip_ReturnsInvalidPagination()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();
        var paginationParams = new PaginationParams(10, -1);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(ErrorMessage.InvalidPagination, result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_TakeAboveMax_ReturnsInvalidPagination()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();
        var paginationParams = new PaginationParams(101, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(ErrorMessage.InvalidPagination, result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_ValidParams_ReturnsNewestFirst()
    {
        //Arrange
        var service = new NotificationServiceSut().GetService();
        var paginationParams = new PaginationParams(10, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, paginationParams);

        //Assert
        Assert.True(result.IsSuccess);
        var ids = result.Data!.Select(x => x.Id).ToArray();
        Assert.Equal([3L, 2L, 1L], ids);
    }
}
