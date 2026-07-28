using NotificationService.Application.Validators;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Tests.Traits;
using NotificationService.Tests.UnitTests.Fixtures;
using Xunit;

namespace NotificationService.Tests.UnitTests.Tests;

[UnitTest]
public class PaginationParamsValidatorTests
{
    private readonly PaginationParamsValidator _validator = new(PaginationRulesFixture.GetPaginationRules());

    [Fact]
    public void Validate_ValidParams_IsValid()
    {
        //Arrange
        var paginationParams = new PaginationParams(10, 0);

        //Act
        var result = _validator.Validate(paginationParams);

        //Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullSkip_IsInvalid()
    {
        //Arrange
        var paginationParams = new PaginationParams(10, null);

        //Act
        var result = _validator.Validate(paginationParams);

        //Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NullTake_IsInvalid()
    {
        //Arrange
        var paginationParams = new PaginationParams(null, 0);

        //Act
        var result = _validator.Validate(paginationParams);

        //Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NegativeSkip_IsInvalid()
    {
        //Arrange
        var paginationParams = new PaginationParams(10, -1);

        //Act
        var result = _validator.Validate(paginationParams);

        //Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TakeAboveMaxPageSize_IsInvalid()
    {
        //Arrange
        var paginationParams = new PaginationParams(PaginationRulesFixture.MaxPageSize + 1, 0);

        //Act
        var result = _validator.Validate(paginationParams);

        //Assert
        Assert.False(result.IsValid);
    }
}
