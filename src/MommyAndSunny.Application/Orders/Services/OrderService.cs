using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Application.Orders.DTOs;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public OrderService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, cancellationToken);
        if (address is null)
        {
            throw new NotFoundException("địa chỉ giao hàng", request.AddressId);
        }

        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var cartItems = cart is null
            ? new List<CartItem>()
            : await _context.CartItems
                .Where(i => i.CartId == cart.Id)
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                .ToListAsync(cancellationToken);

        if (cartItems.Count == 0)
        {
            throw new DomainException("Giỏ hàng trống, không thể đặt hàng");
        }

        // Double-check tồn kho lần cuối — tránh trường hợp hết hàng giữa lúc
        // khách thêm giỏ và lúc thực sự bấm đặt hàng
        foreach (var item in cartItems)
        {
            if (item.Quantity > item.ProductVariant.AvailableQuantity)
            {
                throw new InsufficientStockException(
                    item.ProductVariant.Product.Name, item.ProductVariant.AvailableQuantity, item.Quantity);
            }
        }

        var subTotal = cartItems.Sum(i => i.ProductVariant.Price * i.Quantity);
        const decimal shippingFee = 0;   // MVP: miễn phí ship — tính phí thật để Tier 2
        const decimal discountAmount = 0; // Chưa có coupon ở MVP

        var order = new Order
        {
            OrderCode = GenerateOrderCode(),
            UserId = userId,
            // Snapshot thông tin giao hàng — không chỉ lưu FK đến Address (đã giải thích ở tài liệu database)
            RecipientName = address.RecipientName,
            RecipientPhone = address.PhoneNumber,
            ShippingAddress = $"{address.DetailAddress}, {address.Ward}, {address.District}, {address.Province}",
            Status = OrderStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = OrderPaymentStatus.Unpaid,
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            DiscountAmount = discountAmount,
            TotalAmount = subTotal + shippingFee - discountAmount,
            CustomerNote = request.CustomerNote
        };

        foreach (var item in cartItems)
        {
            order.Items.Add(new OrderItem
            {
                ProductVariantId = item.ProductVariantId,
                ProductNameSnapshot = item.ProductVariant.Product.Name,
                SkuSnapshot = item.ProductVariant.Sku,
                VariantDescriptionSnapshot = BuildVariantDescription(item.ProductVariant),
                UnitPrice = item.ProductVariant.Price,
                Quantity = item.Quantity,
                Subtotal = item.ProductVariant.Price * item.Quantity
            });

            // Chỉ TĂNG ReservedQuantity — CHƯA trừ StockQuantity thật.
            // Trừ kho chính thức chỉ xảy ra khi admin chuyển đơn sang Confirmed (sẽ làm ở module Admin Orders)
            item.ProductVariant.ReservedQuantity += item.Quantity;
        }

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);

        // Thêm trực tiếp vào DbSet (không qua order.StatusHistory.Add) — cùng lý do đã xác định
        // ở CancelOrderAsync, áp dụng nhất quán để tránh rủi ro tương tự
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = null,
            ToStatus = OrderStatus.Pending,
            ChangedByUserId = null,
            Note = "Đơn hàng được tạo",
            ChangedAt = DateTime.UtcNow
        });

        try
        {
            // Toàn bộ thay đổi (Order, OrderItems, ReservedQuantity, xóa CartItems)
            // nằm trong 1 lệnh SaveChangesAsync -> EF Core tự bọc trong 1 transaction,
            // đảm bảo atomic: hoặc tất cả thành công, hoặc rollback toàn bộ nếu lỗi
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion trên ProductVariant phát hiện có request khác vừa sửa tồn kho
            // đồng thời — an toàn hơn là báo lỗi để khách thử lại, tránh bán vượt tồn kho
            throw new ConflictException("Có thay đổi tồn kho trong lúc bạn đặt hàng, vui lòng thử lại");
        }

        return new CreateOrderResponse
        {
            OrderCode = order.OrderCode,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString()
        };
    }

    public async Task<PagedResult<OrderListItemDto>> GetMyOrdersAsync(OrderFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

        var query = _context.Orders.Where(o => o.UserId == userId);

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => new OrderListItemDto
            {
                OrderCode = o.OrderCode,
                Status = o.Status.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<OrderDto> GetOrderByCodeAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            // Điều kiện UserId = userId đảm bảo khách A không xem được đơn của khách B
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("đơn hàng", orderCode);
        }

        return MapToOrderDto(order);
    }

    public async Task CancelOrderAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("đơn hàng", orderCode);
        }

        // State machine: chỉ cho hủy khi Pending hoặc Confirmed — đúng theo nghiệp vụ đã thống nhất
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
        {
            throw new DomainException("Không thể hủy đơn hàng đang được xử lý");
        }

        foreach (var item in order.Items)
        {
            if (order.Status == OrderStatus.Pending)
            {
                // Mới chỉ giữ chỗ (Reserved), chưa trừ kho thật -> chỉ cần giải phóng chỗ giữ
                item.ProductVariant.ReservedQuantity -= item.Quantity;
            }
            else
            {
                // Confirmed: đã trừ kho thật trước đó -> phải hoàn lại StockQuantity
                item.ProductVariant.StockQuantity += item.Quantity;
            }
        }

        // Thêm trực tiếp vào DbSet thay vì qua order.StatusHistory.Add(...) —
        // cách cũ (qua navigation collection của 1 Order được load KHÔNG kèm Include StatusHistory)
        // khiến EF Core đôi khi suy luận sai trạng thái entity thành Modified thay vì Added,
        // sinh ra UPDATE thay vì INSERT -> gây DbUpdateConcurrencyException giả (đã xác nhận qua log)
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = OrderStatus.Cancelled,
            ChangedByUserId = null,
            Note = "Khách hàng tự hủy đơn",
            ChangedAt = DateTime.UtcNow
        });

        order.Status = OrderStatus.Cancelled;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Đơn hàng vừa được cập nhật, vui lòng tải lại trang và thử lại");
        }
    }

    private static string GenerateOrderCode()
    {
        // MVP: timestamp + số ngẫu nhiên — đủ tránh trùng ở quy mô nhỏ.
        // Nếu lượng đơn tăng cao, nên chuyển sang sequence riêng trong database để đảm bảo chặt hơn.
        return $"MS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
    }

    private static string? BuildVariantDescription(ProductVariant variant)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(variant.Size)) parts.Add($"Size {variant.Size}");
        if (!string.IsNullOrEmpty(variant.Color)) parts.Add(variant.Color);
        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    private static OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            OrderCode = order.OrderCode,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            ShippingAddress = order.ShippingAddress,
            SubTotal = order.SubTotal,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            CustomerNote = order.CustomerNote,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductName = i.ProductNameSnapshot,
                VariantDescription = i.VariantDescriptionSnapshot,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                Subtotal = i.Subtotal
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
}