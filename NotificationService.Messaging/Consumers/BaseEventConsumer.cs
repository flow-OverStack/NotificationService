using MassTransit;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interface.Service;
using NotificationService.Domain.Results;
using NotificationService.Messaging.Events;
using Serilog;

namespace NotificationService.Messaging.Consumers;

public class BaseEventConsumer(
    INotificationEventHandler notificationHandler,
    ILogger logger) : IConsumer<BaseEvent>
{
    public async Task Consume(ConsumeContext<BaseEvent> context)
    {
        var dto = new UserEventDto(context.Message.EventId, context.Message.AuthorId, context.Message.InitiatorId,
            Enum.Parse<BaseEventType>(context.Message.EventType), Enum.Parse<EntityType>(context.Message.EntityType), context.Message.EntityId);
        
        var result = await notificationHandler.CreateAsync(dto, context.CancellationToken);

        LogUpdateResult(context.Message, result);
    }

    private void LogUpdateResult(BaseEvent message, BaseResult result)
    {
        if (!result.IsSuccess)
            logger.Warning(
                "Failed to create notification. Error: {ErrorMessage}. InitiatorId: {InitiatorId}. AuthorId: {UserId}. Event: {EventType}. EventId: {EventId}. Entity type: {EntityType}. EntityId: {EntityId}",
                result.ErrorMessage, message.InitiatorId, message.AuthorId, message.EventType, message.EventId,
                message.EntityType, message.EntityId);
        else
            logger.Information(
                "Successfully created notification. InitiatorId: {InitiatorId}. AuthorId: {UserId}. Event: {EventType}. EventId: {EventId}. Entity type: {EntityType}. EntityId: {EntityId}",
                message.InitiatorId, message.AuthorId, message.EventType, message.EventId, message.EntityType,
                message.EntityId);
    }
}