using Microsoft.EntityFrameworkCore;
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
    // State machine — đúng theo sơ đồ vòng đời đơn hàng đã thống nhất từ đầu dự án:
    // Pending -> Confirmed -> Shipping -> Delivered, với nhánh phụ Cancelled từ Pending/Confirmed
    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        [OrderStatus.Confirmed] = new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
        [OrderStatus.Shipping] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AdminOrderService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        var allowedNextStatuses = ValidTransitions[order.Status];
        if (!allowedNextStatuses.Contains(request.ToStatus))
        {
            // Đúng thông báo lỗi đã thống nhất trong API contract
            throw new DomainException($"Không thể chuyển từ '{order.Status}' sang '{request.ToStatus}'");
        }

        ApplyInventoryEffect(order, request.ToStatus);

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