using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Api.Hubs.Clients;

namespace NotificationService.Api.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>;
