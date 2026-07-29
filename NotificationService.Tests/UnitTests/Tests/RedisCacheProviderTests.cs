using NotificationService.Cache.Providers;
using NotificationService.Tests.Traits;
using NotificationService.Tests.UnitTests.Fixtures;
using Xunit;

namespace NotificationService.Tests.UnitTests.Tests;

[UnitTest]
public class RedisCacheProviderTests
{
    [Fact]
    public async Task StringSetAsync_ThenGetJsonParsedAsync_RoundTripsValue()
    {
        //Arrange
        var provider = new RedisCacheProvider(RedisDatabaseFixture.GetRedisDatabaseConfiguration());
        var value = new[] { 1, 2, 3 };

        //Act
        await provider.StringSetAsync("key", value);
        var result = await provider.GetJsonParsedAsync<int[]>("key");

        //Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetJsonParsedAsync_MissingKey_ReturnsDefault()
    {
        //Arrange
        var provider = new RedisCacheProvider(RedisDatabaseFixture.GetRedisDatabaseConfiguration());

        //Act
        var result = await provider.GetJsonParsedAsync<int[]>("missing");

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetJsonParsedAsync_PoisonedValue_ReturnsDefaultInsteadOfThrowing()
    {
        //Arrange
        var provider = new RedisCacheProvider(RedisDatabaseFixture.GetRedisDatabaseConfiguration());
        await provider.StringSetAsync("key", "not valid json {");

        //Act
        var result = await provider.GetJsonParsedAsync<int[]>("key");

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetsAddAsync_ThenSetStringMembersAsync_ReturnsAddedMembers()
    {
        //Arrange
        var provider = new RedisCacheProvider(RedisDatabaseFixture.GetRedisDatabaseConfiguration());

        //Act
        await provider.SetsAddAsync("set", ["a", "b"]);
        var members = await provider.SetStringMembersAsync("set");

        //Assert
        Assert.Equal(["a", "b"], members.OrderBy(x => x));
    }

    [Fact]
    public async Task KeysDeleteAsync_ExistingKey_RemovesIt()
    {
        //Arrange
        var provider = new RedisCacheProvider(RedisDatabaseFixture.GetRedisDatabaseConfiguration());
        await provider.StringSetAsync("key", 42);

        //Act
        await provider.KeysDeleteAsync(["key"]);
        var result = await provider.GetJsonParsedAsync<int?>("key");

        //Assert
        Assert.Null(result);
    }
}
