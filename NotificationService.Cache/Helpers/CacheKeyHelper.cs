namespace NotificationService.Cache.Helpers;

public static class CacheKeyHelper
{
    private const string RecipientNotificationsPattern = "user:{0}:notifications:{1}:{2}:{3}";
    private const string RecipientNotificationKeysPattern = "user:{0}:notification_keys";
    private const string NullPlaceholder = "-";

    public static string GetRecipientNotificationsKey(long recipientId, bool unreadOnly, int? skip, int? take)
    {
        return string.Format(RecipientNotificationsPattern, recipientId, unreadOnly,
            skip?.ToString() ?? NullPlaceholder, take?.ToString() ?? NullPlaceholder);
    }

    public static string GetRecipientNotificationKeysKey(long recipientId)
    {
        return string.Format(RecipientNotificationKeysPattern, recipientId);
    }
}
