namespace NotificationService.Application.Enums;

public enum ErrorCodes
{
    // UserEvent (Notification): 1-10
    // Pagination: 11-20,
    //Authorization: 21-30
    UserEventNotFound = 1,
    UserEventAlreadyExists = 2,
    SelfNotificationNotAllowed = 3,

    InvalidPagination = 11,
    
    OperationForbidden = 21
}