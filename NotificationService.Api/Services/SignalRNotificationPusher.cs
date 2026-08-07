using Microsoft.AspNetCore.SignalR;
using NotificationService.Api.Hubs;
using NotificationService.Api.Hubs.Clients;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Interfaces.Service;

namespace NotificationService.Api.Services;

public class SignalRNotificationPusher(IHubContext<NotificationHub, INotificationClient> hubContext)
    : INotificationPusher
{
    public Task PushAsync(long recipientId, NotificationDto dto, CancellationToken cancellationToken = default)
        => hubContext.Clients.User(recipientId.ToString()).ReceiveNotification(dto);
}
