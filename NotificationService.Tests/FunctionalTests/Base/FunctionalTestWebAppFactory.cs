using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Api.Settings;
using NotificationService.Cache.Settings;
using NotificationService.DAL;
using NotificationService.Messaging.Consumers;
using NotificationService.Messaging.Events;
using NotificationService.Messaging.Messages;
using NotificationService.Messaging.Settings;
using NotificationService.Tests.FunctionalTests.Configurations;
using NotificationService.Tests.FunctionalTests.Configurations.TestServices;
using NotificationService.Tests.FunctionalTests.Extensions;
using NotificationService.Tests.FunctionalTests.Helpers;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using WireMock.Server;
using Xunit;

namespace NotificationService.Tests.FunctionalTests.Base;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _notificationServicePostgreSql = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("notification-service-db")
        .WithUsername("postgres")
        .WithPassword("root")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7")
        .Build();

    private WireMockServer _wireMockServer = null!;

    public async Task InitializeAsync()
    {
        await _notificationServicePostgreSql.StartAsync();
        await _redisContainer.StartAsync();

        // Program.cs calls AddRealtime/AddHangfire *before* builder.Build() - they read
        // IConfiguration eagerly, at a point WebApplicationFactory's ConfigureAppConfiguration/
        // ConfigureTestServices hooks have not been applied yet. Environment variables are the one
        // configuration source WebApplicationBuilder.CreateBuilder() picks up immediately, so they're
        // the only reliable way to steer this eager read to the containers. This relies on test
        // classes building their host one at a time (see AssemblyInfo.cs) - env vars are process-wide.
        _redisContainer.GetConnectionString().ParseConnectionString(out var redisHost, out var redisPort);
        Environment.SetEnvironmentVariable("RedisSettings__Host", redisHost);
        Environment.SetEnvironmentVariable("RedisSettings__Port", redisPort.ToString());
        Environment.SetEnvironmentVariable("RedisSettings__Password", string.Empty);
        Environment.SetEnvironmentVariable("ConnectionStrings__PostgresSQL",
            _notificationServicePostgreSql.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        await _notificationServicePostgreSql.StopAsync();
        await _redisContainer.StopAsync();
        _wireMockServer.StopServer();
        _wireMockServer.Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            _redisContainer.GetConnectionString().ParseConnectionString(out var redisHost, out var redisPort);

            var testConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "ConnectionStrings:PostgresSQL", _notificationServicePostgreSql.GetConnectionString() },
                    // AddRealtime/AddHealthChecks read RedisSettings straight off IConfiguration at
                    // registration time, before ConfigureTestServices' services.Configure runs - so
                    // the connection info must already be here.
                    { "RedisSettings:Host", redisHost },
                    { "RedisSettings:Port", redisPort.ToString() },
                    { "RedisSettings:Password", string.Empty }
                }!)
                .Build();

            config.AddConfiguration(testConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            var connectionString = _notificationServicePostgreSql.GetConnectionString();
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateAsyncScope();
            scope.PrepPopulation();

            _wireMockServer = _wireMockServer.StartIdentityServer();

            services.RemoveAll<IOptions<KeycloakSettings>>();
            services.Configure<KeycloakSettings>(x =>
            {
                x.Host = _wireMockServer.Url!;
                x.Realm = WireMockIdentityServerExtensions.RealmName;
                x.Audience = TokenHelper.GetAudience();
            });

            services.RemoveAll<IOptions<RedisSettings>>();
            services.Configure<RedisSettings>(x =>
            {
                _redisContainer.GetConnectionString().ParseConnectionString(out var host, out var port);
                x.Host = host;
                x.Port = port;
                x.Password = null!;
            });

            services.RemoveAll<IOptions<KafkaSettings>>();
            services.Configure<KafkaSettings>(x =>
            {
                x.Host = "test-host";
                x.BaseEventTopic = "test-topic";
                x.BaseEventConsumerGroup = "test-consumer-group";
                x.DeadLetterTopic = "test-dead-letter-topic";
            });

            var mockFaultedMessageProducer = new Mock<ITopicProducer<FaultedMessage>>();
            mockFaultedMessageProducer.Setup(x => x.Produce(It.IsAny<FaultedMessage>(), It.IsAny<CancellationToken>()));
            var mockBaseEventProducer = new Mock<ITopicProducer<BaseEvent>>();
            mockBaseEventProducer.Setup(x => x.Produce(It.IsAny<BaseEvent>(), It.IsAny<CancellationToken>()));

            services.RemoveAll<IConsumer<BaseEvent>>();
            services.AddScoped<IConsumer<BaseEvent>, UserEventConsumer>();
            services.RemoveAll<ITopicProducer<FaultedMessage>>();
            services.AddScoped<ITopicProducer<FaultedMessage>>(_ => mockFaultedMessageProducer.Object);
            // ResilientConsumeFilter's RedeliveryJob republishes BaseEvent via ITopicProducer<BaseEvent>
            // on retry, which the real Kafka rider only registers as a consumer endpoint, not a producer.
            services.RemoveAll<ITopicProducer<BaseEvent>>();
            services.AddScoped<ITopicProducer<BaseEvent>>(_ => mockBaseEventProducer.Object);

            services.RemoveAll<IBackgroundJobClient>();
            services.AddScoped<IBackgroundJobClient>(provider => new TestBackgroundJobClient(provider));
        });
    }
}
