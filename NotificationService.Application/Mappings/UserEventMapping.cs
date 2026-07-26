using AutoMapper;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Mappings;

public class UserEventMapping : Profile
{
    public UserEventMapping()
    {
        CreateMap<UserEvent, NotificationDto>().ReverseMap();
        CreateMap<UserEvent, UserEventDto>().ReverseMap();
    }
}