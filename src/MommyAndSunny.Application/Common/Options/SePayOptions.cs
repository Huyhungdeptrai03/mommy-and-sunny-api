namespace MommyAndSunny.Application.Common.Options;

public sealed class SePayOptions
{
    // Payments must be explicitly activated, even when credentials are present.
    public bool Enabled { get; set; } = false;
    public bool HasRequiredCredentials => !string.IsNullOrWhiteSpace(Bank) &&
        !string.IsNullOrWhiteSpace(AccountNumber) && !string.IsNullOrWhiteSpace(WebhookApiKey);
    public bool UseMock { get; set; }
    public string Bank { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string WebhookApiKey { get; set; } = string.Empty;
}
