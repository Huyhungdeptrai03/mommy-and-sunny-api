using Microsoft.Extensions.Options;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Options;
using MommyAndSunny.Application.Common.Payments.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MommyAndSunny.Infrastructure.ExternalServices
{
    // CỐ Ý đặt tên KHÔNG kết thúc bằng "Service" — tránh Scrutor (auto DI scan) tự động
    // bắt nhầm class này rồi đăng ký trùng với dòng đăng ký thủ công ở Program.cs
    // (cùng lý do đã áp dụng với LogEmailSender/SmtpEmailSender trước đó)
    public class MomoPaymentGateway : IMomoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly MomoOptions _options;

        public MomoPaymentGateway(HttpClient httpClient, IOptions<MomoOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<MomoCreatePaymentResult> CreatePaymentRequestAsync(
            string orderCode, decimal amount, string orderInfo, CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid().ToString();
            var amountString = ((long)amount).ToString();
            const string extraData = "";
            const string requestType = "captureWallet";

            // Chuỗi ký PHẢI đúng thứ tự alphabet theo tên field — sai thứ tự sẽ khiến
            // MoMo từ chối request dù nội dung đúng (đây là lỗi rất dễ mắc phải)
            var rawSignature =
                $"accessKey={_options.AccessKey}" +
                $"&amount={amountString}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={_options.IpnUrl}" +
                $"&orderId={orderCode}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={_options.PartnerCode}" +
                $"&redirectUrl={_options.RedirectUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={requestType}";

            var signature = ComputeHmacSha256(rawSignature, _options.SecretKey);

            var requestBody = new
            {
                partnerCode = _options.PartnerCode,
                partnerName = "Mommy and Sunny",
                storeId = "MommyAndSunnyStore",
                requestId,
                amount = amountString,
                orderId = orderCode,
                orderInfo,
                redirectUrl = _options.RedirectUrl,
                ipnUrl = _options.IpnUrl,
                lang = "vi",
                extraData,
                requestType,
                signature
            };

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync(_options.Endpoint, requestBody, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return new MomoCreatePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Không thể kết nối tới MoMo: {ex.Message}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MomoApiResponse>(
                responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null || result.ResultCode != 0)
            {
                return new MomoCreatePaymentResult
                {
                    Success = false,
                    ErrorMessage = result?.Message ?? "Không thể khởi tạo thanh toán MoMo"
                };
            }

            return new MomoCreatePaymentResult
            {
                Success = true,
                PayUrl = result.PayUrl
            };
        }

        public bool VerifyIpnSignature(MomoIpnRequest request)
        {
            // Thứ tự field của chữ ký IPN KHÁC với chữ ký lúc tạo giao dịch —
            // đây là điểm dễ gây lỗi nhất khi tích hợp MoMo, phải theo đúng tài liệu chính thức
            var rawSignature =
                $"accessKey={_options.AccessKey}" +
                $"&amount={request.Amount}" +
                $"&extraData={request.ExtraData}" +
                $"&message={request.Message}" +
                $"&orderId={request.OrderId}" +
                $"&orderInfo={request.OrderInfo}" +
                $"&orderType={request.OrderType}" +
                $"&partnerCode={request.PartnerCode}" +
                $"&payType={request.PayType}" +
                $"&requestId={request.RequestId}" +
                $"&responseTime={request.ResponseTime}" +
                $"&resultCode={request.ResultCode}" +
                $"&transId={request.TransId}";

            var expectedSignature = ComputeHmacSha256(rawSignature, _options.SecretKey);
            return string.Equals(expectedSignature, request.Signature, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private class MomoApiResponse
        {
            public int ResultCode { get; set; }
            public string? Message { get; set; }
            public string? PayUrl { get; set; }
        }
    }
}
