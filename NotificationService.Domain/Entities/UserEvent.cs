using NotificationService.Domain.Interfaces.Entity;

namespace NotificationService.Domain.Entities;

public class UserEvent : IEntityId<long>, IAuditable
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public long RecipientId { get; set; }
    public long InitiatorId { get; set; }
    public string EventType { get; set; }
    public string EntityType { get; set; }
    public long EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}