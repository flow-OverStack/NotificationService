using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Mappings;
using NotificationService.Domain.Interface.Service;

namespace NotificationService.Application.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        services.AddAutoMapper(typeof(UserEventMapping));
        services.InitServices();
    }

    private static void InitServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, Services.NotificationService>();
    }
}