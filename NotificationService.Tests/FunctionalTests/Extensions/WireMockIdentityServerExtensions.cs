using System.Reflection;
using NotificationService.Tests.FunctionalTests.Helpers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace NotificationService.Tests.FunctionalTests.Extensions;

internal static class WireMockIdentityServerExtensions
{
    public const string RealmName = "TestRealm";

    private const string ResponsesDirectoryName = "TestServerResponses";

    public static WireMockServer StartIdentityServer(this WireMockServer server)
    {
        server = server.SafeStartServer();

        server.ConfigureWellKnownEndpoints();

        return server;
    }

    private static void ConfigureWellKnownEndpoints(this WireMockServer server)
    {
        server.Given(Request.Create().WithPath($"/realms/{RealmName}/.well-known/openid-configuration").UsingGet())
            .RespondWith(Response.Create()
                .WithHeader("Content-Type", "application/json")
                .WithBody(GetMetadata(server.Port)).WithSuccess());

        server.Given(Request.Create().WithPath($"/realms/{RealmName}/protocol/openid-connect/certs").UsingGet())
            .RespondWith(Response.Create()
                .WithHeader("Content-Type", "application/json")
                .WithBody(TokenHelper.GetJwk()).WithSuccess());
    }

    private static WireMockServer SafeStartServer(this WireMockServer server)
    {
        if (server is { IsStarted: true }) return server; //Equals to (_server == null || !_server.IsStarted)
        server = WireMockServer.Start();
        return server;
    }

    private static string GetMetadata(int serverPort)
    {
        const string metadataFileName = "MetadataResponse.json";

        var response = GetResponse(metadataFileName);

        response = response.Replace("{{Port}}", serverPort.ToString());
        response = response.Replace("{{Realm}}", RealmName);
        response = response.Replace("{{Issuer}}", TokenHelper.GetIssuer());

        return response;
    }

    private static string GetResponse(string fileName)
    {
        var projectDirectory = Directory.GetParent(AppContext.BaseDirectory); //path to runtime directory

        var currentProjectName =
            string.Join('.',
                Assembly.GetExecutingAssembly().GetName().Name!.Split('.')
                    .Take(2)); //name of current project if naming is of type '<AppName>.<DirectoryName>' (e. g. 'NotificationService.Api')

        var filePath =
            PathHelper.GetPathToConfiguration(projectDirectory!, currentProjectName, fileName, ResponsesDirectoryName);

        var response = File.ReadAllText(filePath);

        return response;
    }
}
