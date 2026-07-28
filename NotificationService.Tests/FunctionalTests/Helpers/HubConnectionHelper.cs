using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace NotificationService.Tests.FunctionalTests.Helpers;

internal static class HubConnectionHelper
{
    private const string HubPath = "hubs/notifications";

    /// <summary>
    ///     Builds a live SignalR connection to the test server. Pinned to LongPolling because
    ///     <see cref="Microsoft.AspNetCore.TestHost.TestServer" /> has no real WebSocket listener.
    /// </summary>
    public static HubConnection BuildConnection(WebApplicationFactory<Program> factory, string token)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, HubPath), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult(token)!;
            })
            .Build();
    }
}
