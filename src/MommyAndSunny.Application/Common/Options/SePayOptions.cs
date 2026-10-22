namespace MommyAndSunny.Application.Common.Options;

public sealed class SePayOptions
{
    public bool UseMock { get; set; }
    public string Bank { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string WebhookApiKey { get; set; } = string.Empty;
}
