namespace Billing.Options;

public class GlobalSettingsOptions
{
    public required string WebhookKey { get; set; }
    public required Dictionary<string, EnvironmentConfig> Environments { get; set; }
}

public class EnvironmentConfig
{
    public required string BaseAddress { get; set; }
    public required string WebhookKey { get; set; }
}
