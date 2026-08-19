using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MommyAndSunny.Application.Admin.Orders.DTOs;
using MommyAndSunny.Application.Admin.Orders.Service;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Application.Orders.DTOs;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Admin.Orders.Services;


public class AdminOrderService : IAdminOrderService
{
    // State machine — CONTEXT-AWARE: đơn thường đi thẳng Confirmed -> Shipping, đơn có
    // sản phẩm thuộc đợt hàng xách tay (PurchaseBatchId != null trên ít nhất 1 OrderItem)
    // phải đi qua 3 bước trung gian Purchasing -> InternationalShipping -> ArrivedAtWarehouse
    private static bool HasBatchItems(Order order) => order.Items.Any(i => i.PurchaseBatchId != null);

    private static OrderStatus[] GetValidNextStatuses(Order order)
    {
        var isBatchOrder = HasBatchItems(order);

        return order.Status switch
        {
            OrderStatus.Pending => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => isBatchOrder
                ? new[] { OrderStatus.Purchasing, OrderStatus.Cancelled }
                : new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
            OrderStatus.Purchasing => new[] { OrderStatus.InternationalShipping },
            OrderStatus.InternationalShipping => new[] { OrderStatus.ArrivedAtWarehouse },
            OrderStatus.ArrivedAtWarehouse => new[] { OrderStatus.Shipping },
            OrderStatus.Shipping => new[] { OrderStatus.Delivered },
            OrderStatus.Delivered => Array.Empty<OrderStatus>(),
            OrderStatus.Cancelled => Array.Empty<OrderStatus>(),
            _ => Array.Empty<OrderStatus>()
        };
    }

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;

    public AdminOrderService(
        IApplicationDbContext context, ICurrentUserService currentUserService, IConfiguration configuration)
    {
        _context = context;
        _currentUserService = currentUserService;
        _configuration = configuration;
    }

    // Đơn có sản phẩm đặt trước VÀ tổng giá trị từ ngưỡng này trở lên -> bắt buộc phải
    // đặt cọc và xác nhận đã thu trước khi cho chuyển sang "Đang mua hàng". Đọc từ config
    // "PurchaseBatch:DepositRequiredThreshold" (VND), mặc định 2.000.000đ nếu chưa cấu hình
    private bool RequiresDeposit(Order order)
    {
        if (!HasBatchItems(order)) return false;

        var threshold = decimal.Parse(
            _configuration["PurchaseBatch:DepositRequiredThreshold"] ?? "2000000");
        return order.TotalAmount >= threshold;
    }

    public async Task<PagedResult<AdminOrderListItemDto>> GetOrdersAsync(AdminOrderFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.Include(o => o.User).AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(o =>
                o.OrderCode.Contains(filter.Search) ||
                o.User.Email.Contains(filter.Search) ||
                o.RecipientName.Contains(filter.Search));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => new AdminOrderListItemDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                CustomerName = o.RecipientName,
                CustomerEmail = o.User.Email,
                Status = o.Status.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminOrderListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<AdminOrderDetailDto> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("đơn hàng", id);
        }

        return new AdminOrderDetailDto
        {
            OrderCode = order.OrderCode,
            CustomerName = order.RecipientName,
            CustomerEmail = order.User.Email,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            ShippingAddress = order.ShippingAddress,
            SubTotal = order.SubTotal,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            CouponCode = order.CouponCode,
            TotalAmount = order.TotalAmount,
            DepositAmount = order.DepositAmount,
            DepositPaymentStatus = order.DepositPaymentStatus.ToString(),
            RequiresDeposit = RequiresDeposit(order),
            CustomerNote = order.CustomerNote,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductName = i.ProductNameSnapshot,
                VariantDescription = i.VariantDescriptionSnapshot,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                Subtotal = i.Subtotal,
                PurchaseBatchName = i.PurchaseBatchNameSnapshot
            }).ToList(),
            StatusHistory = order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new OrderStatusHistoryDto
                {
                    FromStatus = h.FromStatus?.ToString(),
                    ToStatus = h.ToStatus.ToString(),
                    Note = h.Note,
                    ChangedAt = h.ChangedAt
                }).ToList()
        };
    }

    public async Task UpdateOrderStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var adminUserId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("đơn hàng", id);
        }

        var allowedNextStatuses = GetValidNextStatuses(order);
        if (!allowedNextStatuses.Contains(request.ToStatus))
        {
            // Đúng thông báo lỗi đã thống nhất trong API contract
            throw new DomainException($"Không thể chuyển từ '{order.Status}' sang '{request.ToStatus}'");
        }

        // Đơn có hàng đặt trước: bắt buộc đã thanh toán đủ (PaymentStatus = Paid) trước khi
        // giao hàng nội địa — tránh giao hàng khi khách còn nợ tiền hàng đã về kho
        if (HasBatchItems(order)
            && order.Status == OrderStatus.ArrivedAtWarehouse
            && request.ToStatus == OrderStatus.Shipping
            && order.PaymentStatus != OrderPaymentStatus.Paid)
        {
            throw new DomainException(
                "Cần xác nhận khách đã thanh toán đủ trước khi giao hàng nội địa. " +
                "Dùng chức năng \"Đánh dấu đã thanh toán\" trước.");
        }

        // Đơn giá trị cao có sản phẩm đặt trước: BẮT BUỘC đã đặt cọc và đã thu cọc
        // trước khi cho phép đi mua hàng thật ở nước ngoài — giảm rủi ro tài chính
        // cho shop khi ứng tiền mua hàng mà chưa chắc khách sẽ nhận hàng
        if (order.Status == OrderStatus.Confirmed
            && request.ToStatus == OrderStatus.Purchasing
            && RequiresDeposit(order)
            && order.DepositPaymentStatus != OrderPaymentStatus.Paid)
        {
            throw new DomainException(
                "Đơn hàng giá trị cao có sản phẩm đặt trước bắt buộc phải đặt cọc và " +
                "xác nhận đã thu cọc trước khi chuyển sang \"Đang mua hàng\".");
        }

        ApplyInventoryEffect(order, request.ToStatus);

        // Hoàn lại lượt dùng mã giảm giá khi Admin hủy đơn — cùng logic đã áp dụng cho
        // luồng khách tự hủy (OrderService.CancelOrderAsync)
        if (request.ToStatus == OrderStatus.Cancelled && order.CouponId.HasValue)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Id == order.CouponId.Value, cancellationToken);
            if (coupon is not null)
            {
                coupon.UsageCount = Math.Max(0, coupon.UsageCount - 1);
            }
        }

        // Thêm trực tiếp vào DbSet (không qua order.StatusHistory.Add) — tránh bug EF Core
        // suy luận sai trạng thái entity (Added vs Modified) đã xác nhận ở module Orders
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = request.ToStatus,
            ChangedByUserId = adminUserId,
            Note = request.Note,
            ChangedAt = DateTime.UtcNow
        });

        order.Status = request.ToStatus;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Có thay đổi tồn kho đồng thời, vui lòng tải lại và thử lại");
        }
    }

    public async Task<AdminOrderDetailDto> SetDepositAsync(
        Guid id, decimal depositAmount, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("đơn hàng", id);
        }

        if (!HasBatchItems(order))
        {
            throw new DomainException("Chỉ đơn có sản phẩm thuộc đợt hàng đặt trước mới cần đặt cọc");
        }

        if (depositAmount <= 0 || depositAmount > order.TotalAmount)
        {
            throw new DomainException("Số tiền cọc phải lớn hơn 0 và không vượt quá tổng giá trị đơn");
        }

        order.DepositAmount = depositAmount;
        // Đặt/sửa lại số tiền cọc luôn coi như CHƯA thu — admin phải tự đánh dấu lại
        // đã thu sau khi thực sự nhận được tiền
        order.DepositPaymentStatus = OrderPaymentStatus.Unpaid;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetOrderByIdAsync(id, cancellationToken);
    }

    public async Task<AdminOrderDetailDto> MarkDepositPaidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException("đơn hàng", id);

        if (order.DepositAmount is null)
        {
            throw new DomainException("Đơn hàng chưa được đặt số tiền cọc");
        }

        order.DepositPaymentStatus = OrderPaymentStatus.Paid;
        await _context.SaveChangesAsync(cancellationToken);
        return await GetOrderByIdAsync(id, cancellationToken);
    }

    public async Task<AdminOrderDetailDto> MarkFinalPaymentPaidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException("đơn hàng", id);

        order.PaymentStatus = OrderPaymentStatus.Paid;
        await _context.SaveChangesAsync(cancellationToken);
        return await GetOrderByIdAsync(id, cancellationToken);
    }

    private static void ApplyInventoryEffect(Order order, OrderStatus toStatus)
    {
        switch (order.Status, toStatus)
        {
            // Xác nhận đơn -> trừ kho THẬT, đồng thời giải phóng phần đã giữ chỗ
            case (OrderStatus.Pending, OrderStatus.Confirmed):
                foreach (var item in order.Items)
                {
                    item.ProductVariant.StockQuantity -= item.Quantity;
                    item.ProductVariant.ReservedQuantity -= item.Quantity;
                }
                break;

            // Hủy khi mới Pending -> chỉ giải phóng chỗ giữ, chưa từng trừ kho thật
            case (OrderStatus.Pending, OrderStatus.Cancelled):
                foreach (var item in order.Items)
                {
                    item.ProductVariant.ReservedQuantity -= item.Quantity;
                }
                break;

            // Hủy khi đã Confirmed -> đã trừ kho thật trước đó, bắt buộc phải hoàn lại
            case (OrderStatus.Confirmed, OrderStatus.Cancelled):
                foreach (var item in order.Items)
                {
                    item.ProductVariant.StockQuantity += item.Quantity;
                }
                break;

            // Giao thành công + thanh toán COD -> tự động đánh dấu đã thanh toán
            case (OrderStatus.Shipping, OrderStatus.Delivered):
                if (order.PaymentMethod == PaymentMethod.COD)
                {
                    order.PaymentStatus = OrderPaymentStatus.Paid;
                }
                break;

                // Confirmed -> Shipping: không ảnh hưởng đến tồn kho, chỉ đổi trạng thái
        }
    }
}