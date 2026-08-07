using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Enums;
using NotificationService.Application.Resources;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interface.Repository;
using NotificationService.Domain.Interface.Service;
using NotificationService.Domain.Results;
using Serilog;

namespace NotificationService.Application.Services;

public class NotificationService(
    IBaseRepository<UserEvent> userEventRepository,
    INotificationPusher notificationPusher,
    IMapper mapper,
    ILogger logger) : INotificationService, INotificationEventHandler
{
    public async Task<BaseResult<NotificationDto>> CreateAsync(UserEventDto eventDto,
        CancellationToken cancellationToken = default)
    {
        var userEvent = await userEventRepository.GetAll()
            .FirstOrDefaultAsync(x => x.EventId == eventDto.EventId, cancellationToken);

        if (userEvent != null)
            return BaseResult<NotificationDto>.Failure(ErrorMessage.UserEventAlreadyExists,
                (int)ErrorCodes.UserEventAlreadyExists);

        if (eventDto.RecipientId == eventDto.InitiatorId)
            return BaseResult<NotificationDto>.Failure(ErrorMessage.SelfNotificationNotAllowed,
                (int)ErrorCodes.SelfNotificationNotAllowed);

        userEvent = mapper.Map<UserEvent>(eventDto);

        await userEventRepository.CreateAsync(userEvent, cancellationToken);
        await userEventRepository.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<NotificationDto>(userEvent);

        try
        {
            await notificationPusher.PushAsync(userEvent.RecipientId, dto, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // the notification may not reach the user (e.g. user is not connected) - not an exception
            logger.Debug(e, "Failed to push realtime notification to recipient {RecipientId}",
                userEvent.RecipientId);
        }

        return BaseResult<NotificationDto>.Success(dto);
    }

    public async Task<BaseResult<NotificationDto>> MarkAsReadAsync(long id, long userId,
        CancellationToken cancellationToken = default)
    {
        var userEvent = await userEventRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (userEvent == null)
            return BaseResult<NotificationDto>.Failure(ErrorMessage.UserEventNotFound,
                (int)ErrorCodes.UserEventNotFound);

        if (userEvent.RecipientId != userId)
            return BaseResult<NotificationDto>.Failure(ErrorMessage.OperationForbidden,
                (int)ErrorCodes.OperationForbidden);


        userEvent.IsRead = true;

        userEventRepository.Update(userEvent);
        await userEventRepository.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<NotificationDto>(userEvent);

        return BaseResult<NotificationDto>.Success(dto);
    }

    public async Task<CollectionResult<NotificationDto>> GetAllByRecipientIdAsync(long recipientId, bool unreadOnly,
        PaginationParams paginationParams,
        CancellationToken cancellationToken = default)
    {
        var skip = paginationParams.Skip!.Value;
        var take = paginationParams.Take!.Value;

        var events = await userEventRepository.GetAll()
            .Where(x => x.RecipientId == recipientId)
            .Where(x => !unreadOnly || !x.IsRead)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);

        var notifications = events.Select(mapper.Map<NotificationDto>);

        return CollectionResult<NotificationDto>.Success(notifications);
    }
}