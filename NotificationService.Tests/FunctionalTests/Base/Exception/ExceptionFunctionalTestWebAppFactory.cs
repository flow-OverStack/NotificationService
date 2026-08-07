using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Tests.Support;

namespace NotificationService.Tests.FunctionalTests.Base.Exception;

public class ExceptionFunctionalTestWebAppFactory : FunctionalTestWebAppFactory
{
    private static IMock<INotificationService> GetExceptionMockNotificationService()
    {
        var mockService = new Mock<INotificationService>();

        mockService.Setup(x => x.MarkAsReadAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestException());
        mockService.Setup(x => x.GetAllByRecipientIdAsync(It.IsAny<long>(), It.IsAny<bool>(),
                It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestException());

        return mockService;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<INotificationService>();
            services.AddScoped<INotificationService>(_ => GetExceptionMockNotificationService().Object);
        });
    }
}
