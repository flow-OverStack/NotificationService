using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using NotificationService.Application.Resources;
using NotificationService.Domain.Results;
using NotificationService.Tests.FunctionalTests.Base.Exception;
using NotificationService.Tests.FunctionalTests.Helpers;
using NotificationService.Tests.Traits;
using Xunit;

namespace NotificationService.Tests.FunctionalTests.Tests;

[FunctionalTest]
public class ExceptionTests(ExceptionFunctionalTestWebAppFactory factory) : ExceptionFunctionalTest(factory)
{
    [Fact]
    public async Task GetNotifications_ServiceThrows_ReturnsInternalServerError()
    {
        //Arrange
        var token = TokenHelper.GetRsaToken("testuser1", 1, ["User"]);
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        //Act
        var response = await HttpClient.GetAsync("/api/v1.0/notification?unreadOnly=false&Take=10&Skip=0");
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<BaseResult>(body);

        //Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(result!.IsSuccess);
        Assert.Contains(ErrorMessage.InternalServerError, result.ErrorMessage);
    }
}
