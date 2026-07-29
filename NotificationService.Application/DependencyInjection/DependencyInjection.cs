using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Mappings;
using NotificationService.Application.Services.Cache;
using NotificationService.Application.Validators;
using NotificationService.Domain.Dtos.Pagination;
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
        services.AddScoped<INotificationEventHandler, Services.NotificationService>();
        services.AddScoped<IValidator<PaginationParams>, PaginationParamsValidator>();

        services.Decorate<INotificationService, CacheNotificationService>();
        services.Decorate<INotificationEventHandler, CacheNotificationEventHandler>();
    }
}