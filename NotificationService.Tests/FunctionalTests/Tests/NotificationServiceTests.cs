using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NotificationService.DAL;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Results;
using NotificationService.Tests.FunctionalTests.Base;
using NotificationService.Tests.FunctionalTests.Helpers;
using NotificationService.Tests.Traits;
using Xunit;

namespace NotificationService.Tests.FunctionalTests.Tests;

[FunctionalTest]
public class NotificationServiceTests : SequentialFunctionalTest
{
    public NotificationServiceTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        var token = TokenHelper.GetRsaToken("testuser1", 1, ["User"]);
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_ValidRequest_ReturnsOnlyOwnEvents()
    {
        //Arrange & Act
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=false&Take=10&Skip=0");
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CollectionResult<NotificationDto>>(body);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result!.IsSuccess);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAll_UnreadOnly_ReturnsUnreadEvents()
    {
        //Arrange & Act
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=true&Take=10&Skip=0");
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CollectionResult<NotificationDto>>(body);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result!.IsSuccess);
        Assert.Equal(2, result.Count);
        Assert.All(result.Data!, x => Assert.False(x.IsRead));
    }

    [Fact]
    public async Task GetAll_ValidPagination_ReturnsPagedEvents()
    {
        //Arrange & Act
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=false&Take=1&Skip=1");
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CollectionResult<NotificationDto>>(body);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result!.IsSuccess);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task GetAll_TakeAboveMax_ReturnsBadRequest()
    {
        //Arrange & Act
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=false&Take=1000&Skip=0");

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_OwnEvent_ReturnsOk()
    {
        //Arrange
        const long eventId = 1;

        //Act
        var response = await HttpClient.PatchAsync($"/api/v1.0/notification/{eventId}/read", null);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<BaseResult<NotificationDto>>(body);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result!.IsSuccess);
        Assert.True(result.Data!.IsRead);

        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userEvent = await dbContext.Set<UserEvent>().FirstAsync(x => x.Id == eventId);
        Assert.True(userEvent.IsRead);
    }

    [Fact]
    public async Task MarkAsRead_NonExistentEvent_ReturnsNotFound()
    {
        //Arrange & Act
        var response = await HttpClient.PatchAsync("/api/v1.0/notification/9999/read", null);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_OtherUsersEvent_ReturnsForbidden()
    {
        //Arrange
        const long otherUsersEventId = 4; // belongs to recipient 2

        //Act
        var response = await HttpClient.PatchAsync($"/api/v1.0/notification/{otherUsersEventId}/read", null);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
