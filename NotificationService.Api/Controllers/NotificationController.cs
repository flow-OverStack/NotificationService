using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Controllers.Base;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Extensions;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Domain.Results;

namespace NotificationService.Api.Controllers;

/// <summary>
///     Controller for handling notification-related operations.
/// </summary>
[Authorize]
public class NotificationController(INotificationService notificationService) : BaseController
{
    /// <summary>
    ///     Marks a notification as read
    /// </summary>
    /// <response code="200">Notification was marked as read successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not the recipient of the notification</response>
    /// <response code="404">Notification not found</response>
    [HttpPatch("{id:long}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResult<NotificationDto>>> MarkAsReadAsync(long id,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await notificationService.MarkAsReadAsync(id, userId, cancellationToken);

        return HandleBaseResult(result);
    }

    /// <summary>
    ///     Gets a paginated list of notifications for the current user
    /// </summary>
    /// <response code="200">Notifications were retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CollectionResult<NotificationDto>>> GetAllByRecipientIdAsync(bool unreadOnly,
        [FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result =
            await notificationService.GetAllByRecipientIdAsync(userId, unreadOnly, paginationParams, cancellationToken);

        return HandleCollectionResult(result);
    }
}