using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Application.Enums;
using NotificationService.Application.Resources;
using NotificationService.Application.Settings;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interface.Repository;
using NotificationService.Domain.Interface.Service;
using NotificationService.Domain.Results;

namespace NotificationService.Application.Services;

public class NotificationService(
    IBaseRepository<UserEvent> userEventRepository,
    IMapper mapper,
    IValidator<PaginationParams> validator,
    IOptions<PaginationRules> paginationOptions) : INotificationService, INotificationEventHandler
{
    private readonly PaginationRules _paginationRules = paginationOptions.Value;

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
        var skip = paginationParams.Skip ?? 0;
        var take = paginationParams.Take ?? _paginationRules.DefaultPageSize;

        paginationParams = new PaginationParams(take, skip);

        var validation = await validator.ValidateAsync(paginationParams, cancellationToken);
        if (!validation.IsValid)
        {
            var message = $"{ErrorMessage.InvalidPagination}: " +
                          string.Join(' ', validation.Errors.Select(e => e.ErrorMessage));
            return CollectionResult<NotificationDto>.Failure(message, (int)ErrorCodes.InvalidPagination);
        }

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