using Moq;
using NotificationService.Application.Services;
using NotificationService.Application.Validators;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Domain.Results;
using NotificationService.Tests.Traits;
using NotificationService.Tests.UnitTests.Fixtures;
using Xunit;

namespace NotificationService.Tests.UnitTests.Tests;

[UnitTest]
public class PaginationResolvingNotificationServiceTests
{
    private static PaginationResolvingNotificationService CreateSut(Mock<INotificationService> innerMock)
    {
        var paginationRules = PaginationRulesFixture.GetPaginationRules();
        var validator = ValidatorFixture<PaginationParams>.GetValidator(new PaginationParamsValidator(paginationRules));
        var resolver = new PaginationResolver(validator, paginationRules);

        return new PaginationResolvingNotificationService(resolver, innerMock.Object);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_InvalidPagination_DoesNotCallInner()
    {
        //Arrange
        var innerMock = new Mock<INotificationService>();
        var service = CreateSut(innerMock);
        var invalidParams = new PaginationParams(PaginationRulesFixture.MaxPageSize + 1, 0);

        //Act
        var result = await service.GetAllByRecipientIdAsync(1, false, invalidParams);

        //Assert
        Assert.False(result.IsSuccess);
        innerMock.Verify(
            x => x.GetAllByRecipientIdAsync(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<PaginationParams>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllByRecipientIdAsync_OmittedParams_PassesResolvedParamsToInner()
    {
        //Arrange
        var innerMock = new Mock<INotificationService>();
        innerMock.Setup(x => x.GetAllByRecipientIdAsync(It.IsAny<long>(), It.IsAny<bool>(),
                It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CollectionResult<NotificationDto>.Success([]));
        var service = CreateSut(innerMock);
        var omittedParams = new PaginationParams(null, null);

        //Act
        await service.GetAllByRecipientIdAsync(1, false, omittedParams);

        //Assert
        innerMock.Verify(x => x.GetAllByRecipientIdAsync(1, false,
            It.Is<PaginationParams>(p => p.Skip == 0 && p.Take == PaginationRulesFixture.DefaultPageSize),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_Always_DelegatesToInner()
    {
        //Arrange
        var innerMock = new Mock<INotificationService>();
        var dto = new NotificationDto(1, 2, "Fake", "Fake", 1, false, DateTime.UtcNow);
        innerMock.Setup(x => x.MarkAsReadAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BaseResult<NotificationDto>.Success(dto));
        var service = CreateSut(innerMock);

        //Act
        await service.MarkAsReadAsync(1, 2);

        //Assert
        innerMock.Verify(x => x.MarkAsReadAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
