namespace NotificationService.Domain.Dtos.Notification;

public record NotificationDto(
    long Id,
    long InitiatorId,
    string EventType,
    string EntityType,
    long EntityId,
    bool IsRead,
    DateTime CreatedAt);