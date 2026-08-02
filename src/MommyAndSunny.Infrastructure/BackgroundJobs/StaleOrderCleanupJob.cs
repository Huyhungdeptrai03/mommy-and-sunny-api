using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Infrastructure.BackgroundJobs;

// CỐ Ý đặt tên KHÔNG kết thúc bằng "Service" — tránh Scrutor tự động bắt nhầm class
// này (không có interface tương ứng), cùng lý do đã áp dụng với LogEmailSender,
// SmtpEmailSender, MomoPaymentGateway ở các bước trước
public class StaleOrderCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleOrderCleanupJob> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _staleThreshold;

    public StaleOrderCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<StaleOrderCleanupJob> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Đọc từ config để có thể chỉnh ngắn hơn khi test local (VD: 1 phút/2 phút)
        // mà không cần sửa code — mặc định 5 phút kiểm tra 1 lần, hủy đơn treo quá 30 phút.
        // Dùng int.Parse thủ công thay vì GetValue<T>() để tránh phải cài thêm package
        // Microsoft.Extensions.Configuration.Binder (đã đủ đau đầu với version conflict trước đó)
        var checkIntervalMinutes = int.Parse(configuration["OrderCleanup:CheckIntervalMinutes"] ?? "5");
        var staleThresholdMinutes = int.Parse(configuration["OrderCleanup:StaleThresholdMinutes"] ?? "30");

        _checkInterval = TimeSpan.FromMinutes(checkIntervalMinutes);
        _staleThreshold = TimeSpan.FromMinutes(staleThresholdMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupStaleOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Lỗi ở background job không được làm crash cả ứng dụng — chỉ log lại,
                // vòng lặp tiếp theo sẽ tự thử lại
                _logger.LogError(ex, "Lỗi khi dọn dẹp đơn hàng MoMo bị treo");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CleanupStaleOrdersAsync(CancellationToken cancellationToken)
    {
        // BackgroundService chạy như Singleton, nhưng DbContext là Scoped —
        // BẮT BUỘC phải tự tạo scope riêng mỗi lần chạy, không thể inject DbContext trực tiếp
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var cutoff = DateTime.UtcNow - _staleThreshold;

        var staleOrders = await context.Orders
            .Include(o => o.Items)
            .Where(o =>
                o.PaymentMethod == PaymentMethod.Momo &&
                o.PaymentStatus == OrderPaymentStatus.Unpaid &&
                o.Status == OrderStatus.Pending &&
                o.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (staleOrders.Count == 0)
        {
            _logger.LogInformation(
                "[OrderCleanup] Đã kiểm tra lúc {Time}, không có đơn nào cần hủy",
                DateTime.UtcNow);
            return;
        }

        // Tra cứu tường minh qua ProductVariantId thay vì dùng navigation property
        // (không chắc chắn OrderItem có navigation "Variant" hay không) — an toàn hơn,
        // cùng cách tiếp cận đã dùng ở ReviewService trước đó
        var variantIds = staleOrders
            .SelectMany(o => o.Items)
            .Select(i => i.ProductVariantId)
            .Distinct()
            .ToList();

        var variants = await context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        foreach (var order in staleOrders)
        {
            // Giải phóng tồn kho đã giữ chỗ — quan trọng nhất của cả job này
            foreach (var item in order.Items)
            {
                if (variants.TryGetValue(item.ProductVariantId, out var variant))
                {
                    variant.ReservedQuantity = Math.Max(0, variant.ReservedQuantity - item.Quantity);
                }
            }

            order.Status = OrderStatus.Cancelled;

            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.Pending,
                ToStatus = OrderStatus.Cancelled,
                ChangedByUserId = null,
                Note = "Tự động hủy do quá thời gian chưa thanh toán MoMo",
                ChangedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Tự động hủy đơn {OrderCode} do quá {Minutes} phút chưa thanh toán MoMo",
                order.OrderCode, _staleThreshold.TotalMinutes);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}