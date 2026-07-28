using Xunit;

namespace NotificationService.Tests.FunctionalTests.Base.Exception;

public class ExceptionFunctionalTest : IClassFixture<ExceptionFunctionalTestWebAppFactory>
{
    protected readonly HttpClient HttpClient;

    protected ExceptionFunctionalTest(ExceptionFunctionalTestWebAppFactory factory)
    {
        HttpClient = factory.CreateClient();
    }
}
