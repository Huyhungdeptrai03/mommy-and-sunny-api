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
// SmtpEmailSender, SePayPaymentGateway ở các bước trước
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
                _logger.LogError(ex, "Lỗi khi dọn dẹp đơn hàng SePay bị treo");
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

        var ordersToCancel = new List<(Order Order, string Reason)>();

        // ===== Nhánh 1: đơn SePay chưa thanh toán quá lâu (đã có từ trước) =====
        var cutoff = DateTime.UtcNow - _staleThreshold;
        var staleSePayOrders = await context.Orders
            .Include(o => o.Items)
            .Where(o =>
                o.PaymentMethod == PaymentMethod.SePay &&
                o.PaymentStatus == OrderPaymentStatus.Unpaid &&
                o.Status == OrderStatus.Pending &&
                o.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var order in staleSePayOrders)
        {
            ordersToCancel.Add((order, "Tự động hủy do quá thời gian chưa thanh toán SePay"));
        }

        // ===== Nhánh 2 (MỚI): đơn COD có sản phẩm đặt trước, chưa thu cọc, và đợt hàng
        // gốc đã ĐÓNG (CloseDate đã qua) — khách bỏ ngang không đặt cọc, giữ tồn kho vô thời
        // hạn nếu không dọn. Không áp dụng cho đơn COD hàng có sẵn (Pending vẫn hợp lệ chờ
        // Admin xác nhận thủ công như bình thường) =====
        var codPreOrders = await context.Orders
            .Include(o => o.Items)
            .Where(o =>
                o.PaymentMethod == PaymentMethod.COD &&
                o.Status == OrderStatus.Pending &&
                o.DepositPaymentStatus == OrderPaymentStatus.Unpaid &&
                o.Items.Any(i => i.PurchaseBatchId != null))
            .ToListAsync(cancellationToken);

        if (codPreOrders.Count > 0)
        {
            var batchIds = codPreOrders
                .SelectMany(o => o.Items)
                .Where(i => i.PurchaseBatchId != null)
                .Select(i => i.PurchaseBatchId!.Value)
                .Distinct()
                .ToList();

            var batchCloseDates = await context.PurchaseBatches
                .Where(b => batchIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.CloseDate, cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var order in codPreOrders)
            {
                var isBatchClosed = order.Items.Any(i =>
                    i.PurchaseBatchId != null &&
                    batchCloseDates.TryGetValue(i.PurchaseBatchId.Value, out var closeDate) &&
                    closeDate < now);

                if (isBatchClosed)
                {
                    ordersToCancel.Add((order,
                        "Tự động hủy do chưa đặt cọc trước khi đợt hàng đóng"));
                }
            }
        }

        if (ordersToCancel.Count == 0)
        {
            _logger.LogInformation(
                "[OrderCleanup] Đã kiểm tra lúc {Time}, không có đơn nào cần hủy",
                DateTime.UtcNow);
            return;
        }

        // Tra cứu tường minh qua ProductVariantId thay vì dùng navigation property
        // (không chắc chắn OrderItem có navigation "Variant" hay không) — an toàn hơn,
        // cùng cách tiếp cận đã dùng ở ReviewService trước đó
        var variantIds = ordersToCancel
            .SelectMany(x => x.Order.Items)
            .Select(i => i.ProductVariantId)
            .Distinct()
            .ToList();

        var variants = await context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        foreach (var (order, reason) in ordersToCancel)
        {
            // Giải phóng tồn kho đã giữ chỗ — quan trọng nhất của cả job này
            foreach (var item in order.Items)
            {
                if (variants.TryGetValue(item.ProductVariantId, out var variant))
                {
                    variant.ReservedQuantity = Math.Max(0, variant.ReservedQuantity - item.Quantity);
                }
            }

            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.Pending,
                ToStatus = OrderStatus.Cancelled,
                ChangedByUserId = null,
                Note = reason,
                ChangedAt = DateTime.UtcNow
            });

            order.Status = OrderStatus.Cancelled;

            _logger.LogInformation("Tự động hủy đơn {OrderCode}: {Reason}", order.OrderCode, reason);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
