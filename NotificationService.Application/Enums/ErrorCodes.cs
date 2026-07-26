namespace NotificationService.Application.Enums;

public enum ErrorCodes
{
    // UserEvent: 1-10
    // Pagination: 11-20,
    //Authorization: 21-30
    UserEventNotFound = 1,

    InvalidPagination = 11,
    
    OperationForbidden = 21
}