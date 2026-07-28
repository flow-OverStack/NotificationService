using NotificationService.Domain.Entities;

namespace NotificationService.Tests.TestData;

internal static class UserEventMother
{
    public static readonly Guid ExistingEventId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public static IQueryable<UserEvent> GetUserEvents()
    {
        return new UserEvent[]
        {
            new()
            {
                Id = 1,
                EventId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                RecipientId = 1, // Recipient: User 1
                InitiatorId = 2,
                EventType = "EntityUpvoted",
                EntityType = "Question",
                EntityId = 1,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddSeconds(-50)
            },
            new()
            {
                Id = 2,
                EventId = ExistingEventId,
                RecipientId = 1, // Recipient: User 1
                InitiatorId = 3,
                EventType = "EntityAccepted",
                EntityType = "Answer",
                EntityId = 2,
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddSeconds(-40)
            },
            new()
            {
                Id = 3,
                EventId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                RecipientId = 1, // Recipient: User 1 - newest event for this recipient
                InitiatorId = 2,
                EventType = "EntityDownvoted",
                EntityType = "Question",
                EntityId = 3,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddSeconds(-30)
            },
            new()
            {
                Id = 4,
                EventId = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                RecipientId = 2, // Recipient: User 2
                InitiatorId = 1,
                EventType = "EntityDeleted",
                EntityType = "Comment",
                EntityId = 4,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddSeconds(-20)
            },
            new()
            {
                Id = 5,
                EventId = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                RecipientId = 2, // Recipient: User 2
                InitiatorId = 3,
                EventType = "EntityVoteRemoved",
                EntityType = "Answer",
                EntityId = 5,
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddSeconds(-10)
            },
            new()
            {
                Id = 6,
                EventId = Guid.Parse("00000000-0000-0000-0000-000000000006"),
                RecipientId = 3, // Recipient: User 3
                InitiatorId = 1,
                EventType = "EntityAcceptanceRevoked",
                EntityType = "Question",
                EntityId = 6,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();
    }
}
