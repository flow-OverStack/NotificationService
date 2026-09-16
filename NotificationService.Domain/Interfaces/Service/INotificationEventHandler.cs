using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Results;

namespace NotificationService.Domain.Interfaces.Service;

public interface INotificationEventHandler
{
    Task<BaseResult<NotificationDto>> CreateAsync(UserEventDto eventDto, CancellationToken cancellationToken = default);
}