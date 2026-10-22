namespace MommyAndSunny.Application.Common.Payments.DTOs;

public sealed class SePayWebhookRequest
{
    public long Id { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Content { get; set; } = string.Empty;
    public string TransferType { get; set; } = string.Empty;
    public decimal TransferAmount { get; set; }
    public string? ReferenceCode { get; set; }
    public string? SubAccount { get; set; }
}
