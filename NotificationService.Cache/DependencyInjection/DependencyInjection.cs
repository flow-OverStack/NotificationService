using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationService.Cache.Providers;
using NotificationService.Cache.Repositories;
using NotificationService.Cache.Settings;
using NotificationService.Domain.Interfaces.Provider;
using NotificationService.Domain.Interfaces.Repository.Cache;
using StackExchange.Redis;

namespace NotificationService.Cache.DependencyInjection;

public static class DependencyInjection
{
    public static void AddCache(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisSettings = provider.GetRequiredService<IOptions<RedisSettings>>().Value;
            var configuration = new ConfigurationOptions
            {
                EndPoints = { { redisSettings.Host, redisSettings.Port } },
                Password = redisSettings.Password
            };

            return ConnectionMultiplexer.Connect(configuration);
        });

        services.AddScoped<IDatabase>(provider =>
        {
            var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
            return multiplexer.GetDatabase();
        });

        services.AddScoped<ICacheProvider, RedisCacheProvider>();
        services.AddScoped<INotificationCacheRepository, NotificationCacheRepository>();
    }
}
