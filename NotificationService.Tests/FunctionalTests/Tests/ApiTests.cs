using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using NotificationService.Tests.FunctionalTests.Base;
using NotificationService.Tests.FunctionalTests.Helpers;
using NotificationService.Tests.Traits;
using Xunit;

namespace NotificationService.Tests.FunctionalTests.Tests;

[FunctionalTest]
public class ApiTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task RequestSwagger_ValidRequest_ReturnsOk()
    {
        //Arrange
        const string swaggerUrl = "/swagger/v1/swagger.json";

        //Act
        var response = await HttpClient.GetAsync(swaggerUrl);
        var body = await response.Content.ReadAsStringAsync();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetNotifications_NoToken_ReturnsUnauthorized()
    {
        //Arrange & Act
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=false");

        //Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotifications_TokenMissingRequiredClaim_ReturnsForbidden()
    {
        //Arrange
        var token = TokenHelper.GetRsaTokenMissingRole("testuser", 1);
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        //Act
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=false");
        var body = await response.Content.ReadAsStringAsync();

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Invalid claims", body);

        HttpClient.DefaultRequestHeaders.Authorization = null;
    }
}
