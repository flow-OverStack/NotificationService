namespace NotificationService.Api.Settings;

public class KeycloakSettings
{
    public string Host { get; set; }
    public string Realm { get; set; }
    public string Audience { get; set; }
    public string MetadataAddress => $"{Host}/realms/{Realm}/.well-known/openid-configuration";
}