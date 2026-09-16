using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NotificationService.Api;
using NotificationService.Api.Middlewares;
using NotificationService.Api.Settings;
using NotificationService.Application.DependencyInjection;
using NotificationService.Application.Settings;
using NotificationService.Cache.DependencyInjection;
using NotificationService.Cache.Settings;
using NotificationService.DAL.DependencyInjection;
using NotificationService.Messaging.DependencyInjection;
using NotificationService.Messaging.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KeycloakSettings>(builder.Configuration.GetSection(nameof(KeycloakSettings)));
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(nameof(KafkaSettings)));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection(nameof(RedisSettings)));
builder.Services.Configure<PaginationRules>(builder.Configuration.GetSection(nameof(PaginationRules)));

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();
builder.Services.AddLocalization(options => options.ResourcesPath = nameof(NotificationService.Application.Resources));

builder.Services.AddAuthenticationAndAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();
builder.Services.AddMassTransitServices();
builder.Services.AddHangfire(builder.Configuration);


builder.Host.AddLogging(builder.Configuration);

builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Services.AddCache();
builder.Services.AddApplication();
builder.Services.AddRealtime(builder.Configuration);

builder.AddOpenTelemetry();
builder.Services.AddHealthChecks(builder.Configuration);
builder.Services.AddCors(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseStatusCodePages();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRequestLogging();

app.UseRouting();
app.UseCors("DefaultCorsPolicy");
app.MapControllers();
app.MapRealtime();
app.UseLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfire();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.MapHealthChecks("health", new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
app.UseForwardedHeaders(builder.Configuration);

app.UseMiddleware<ClaimsValidationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
    await app.Services.MigrateDatabaseAsync();
}

app.UseSwagger();

app.LogListeningUrls();

await app.RunAsync();