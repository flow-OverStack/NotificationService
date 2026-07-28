using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.DAL;
using NotificationService.Domain.Entities;
using NotificationService.Tests.TestData;

namespace NotificationService.Tests.FunctionalTests.Configurations;

internal static class PrepDb
{
    public static void PrepPopulation(this IServiceScope serviceScope)
    {
        var userEvents = UserEventMother.GetUserEvents()
            .Select(x => new UserEvent
            {
                Id = 0,
                EventId = x.EventId,
                RecipientId = x.RecipientId,
                InitiatorId = x.InitiatorId,
                EventType = x.EventType,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        var originalCreatedAt = userEvents.ToDictionary(x => x, x => x.CreatedAt);

        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();

        dbContext.Set<UserEvent>().AddRange(userEvents);

        dbContext.SaveChanges();

        // DateInterceptor stamps CreatedAt = UtcNow on insert, overwriting the mother's staggered
        // values. Re-apply them as an update (entities are now tracked as Modified, not Added, so
        // the interceptor leaves CreatedAt alone) so ordering assertions stay deterministic.
        foreach (var userEvent in userEvents) userEvent.CreatedAt = originalCreatedAt[userEvent];
        dbContext.SaveChanges();
    }
}
