using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Dtos.UserEvent;

public record UserEventDto(
    Guid EventId,
    long RecipientId,
    long InitiatorId,
    BaseEventType EventType,
    EntityType EntityType,
    long EntityId);