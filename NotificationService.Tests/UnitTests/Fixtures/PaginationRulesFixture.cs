using Microsoft.Extensions.Options;
using NotificationService.Application.Settings;

namespace NotificationService.Tests.UnitTests.Fixtures;

internal static class PaginationRulesFixture
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    public static IOptions<PaginationRules> GetPaginationRules()
    {
        return Options.Create(new PaginationRules
        {
            MaxPageSize = MaxPageSize,
            DefaultPageSize = DefaultPageSize
        });
    }
}
