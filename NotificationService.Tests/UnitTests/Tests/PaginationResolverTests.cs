using NotificationService.Application.Services;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Tests.Traits;
using NotificationService.Tests.UnitTests.Fixtures;
using Xunit;

namespace NotificationService.Tests.UnitTests.Tests;

[UnitTest]
public class PaginationResolverTests
{
    private readonly PaginationResolver _resolver = new(
        ValidatorFixture<PaginationParams>.GetValidator(
            new Application.Validators.PaginationParamsValidator(PaginationRulesFixture.GetPaginationRules())),
        PaginationRulesFixture.GetPaginationRules());

    [Fact]
    public async Task ResolveAsync_NullParams_AppliesDefaults()
    {
        //Arrange
        var paginationParams = new PaginationParams(null, null);

        //Act
        var result = await _resolver.ResolveAsync(paginationParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(PaginationRulesFixture.DefaultPageSize, result.Data.Take);
        Assert.Equal(0, result.Data.Skip);
    }

    [Fact]
    public async Task ResolveAsync_ExplicitParams_PassesThrough()
    {
        //Arrange
        var paginationParams = new PaginationParams(5, 10);

        //Act
        var result = await _resolver.ResolveAsync(paginationParams);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data.Take);
        Assert.Equal(10, result.Data.Skip);
    }

    [Fact]
    public async Task ResolveAsync_TakeAboveMaxPageSize_ReturnsFailure()
    {
        //Arrange
        var paginationParams = new PaginationParams(PaginationRulesFixture.MaxPageSize + 1, 0);

        //Act
        var result = await _resolver.ResolveAsync(paginationParams);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)Application.Enums.ErrorCodes.InvalidPagination, result.ErrorCode);
    }

    [Fact]
    public async Task ResolveAsync_NegativeSkip_ReturnsFailure()
    {
        //Arrange
        var paginationParams = new PaginationParams(10, -1);

        //Act
        var result = await _resolver.ResolveAsync(paginationParams);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)Application.Enums.ErrorCodes.InvalidPagination, result.ErrorCode);
    }
}
