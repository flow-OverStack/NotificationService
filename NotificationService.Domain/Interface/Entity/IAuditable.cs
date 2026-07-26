namespace NotificationService.Domain.Interface.Entity;

public interface IAuditable
{
    public DateTime CreatedAt { get; set; }
}