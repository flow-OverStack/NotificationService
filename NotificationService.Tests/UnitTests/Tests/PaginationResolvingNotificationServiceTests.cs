using NotificationService.Application.Enums;
using NotificationService.Application.Resources;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Tests.Traits;
using NotificationService.Tests.UnitTests.Fixtures;
using NotificationService.Tests.UnitTests.Sut;
using Xunit;

namespace NotificationService.Tests.UnitTests.Tests;

[UnitTest]
public class PaginationResolvingNotificationServiceTests
{
    [Fact]
    public async Task GetAllByRecipientIdAsync_OmittedParams_UsesDefaultPageSize()
    {
        //Arrange
        var service = new PaginationResolvingNotificationServiceSut().GetService();
        var omittedParams = new PaginationParams(null, null);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, omittedParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_ExplicitParams_PassesThemThroughUnchanged()
    {
        //Arrange
        var service = new PaginationResolvingNotificationServiceSut().GetService();
        var explicitParams = new PaginationParams(1, 1);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, explicitParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([2L], result.Data!.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_TakeAboveMaxPageSize_ReturnsInvalidPagination()
    {
        //Arrange
        var service = new PaginationResolvingNotificationServiceSut().GetService();
        var invalidParams = new PaginationParams(PaginationRulesFixture.MaxPageSize + 1, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, invalidParams);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)ErrorCodes.InvalidPagination, result.ErrorCode);
        Assert.Contains(ErrorMessage.InvalidPagination, result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_NegativeSkip_ReturnsInvalidPagination()
    {
        //Arrange
        var service = new PaginationResolvingNotificationServiceSut().GetService();
        var invalidParams = new PaginationParams(10, -1);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, invalidParams);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)ErrorCodes.InvalidPagination, result.ErrorCode);
        Assert.Contains(ErrorMessage.InvalidPagination, result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_TakeAtMaxPageSize_ReturnsSuccess()
    {
        //Arrange
        var service = new PaginationResolvingNotificationServiceSut().GetService();
        var boundaryParams = new PaginationParams(PaginationRulesFixture.MaxPageSize, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, boundaryParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task MarkAsReadAsync_Always_DelegatesToInner()
    {
        //Arrange
        var service = new PaginationResolvingNotificationServiceSut().GetService();

        //Act
        var result = await service.MarkAsReadAsync(1, 1);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsRead);
    }
}
