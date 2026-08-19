using MommyAndSunny.Application.Admin.PurchaseBatches.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.PurchaseBatches.Services
{
    public class AdminPurchaseBatchService : IAdminPurchaseBatchService
    {
        // Chuyển trạng thái chỉ được đi ĐÚNG theo thứ tự này — không cho nhảy cóc hay lùi lại.
        // Cancelled chỉ khả thi TRƯỚC khi đã thực sự đi mua hàng (Open/Closed) — sau khi đã
        // Purchasing nghĩa là tiền đã chi ra thật, hủy đợt lúc này không còn ý nghĩa
        private static readonly Dictionary<PurchaseBatchStatus, PurchaseBatchStatus[]> ValidTransitions = new()
        {
            [PurchaseBatchStatus.Open] = [PurchaseBatchStatus.Closed, PurchaseBatchStatus.Cancelled],
            [PurchaseBatchStatus.Closed] = [PurchaseBatchStatus.Purchasing, PurchaseBatchStatus.Cancelled],
            [PurchaseBatchStatus.Purchasing] = [PurchaseBatchStatus.Shipping],
            [PurchaseBatchStatus.Shipping] = [PurchaseBatchStatus.Arrived],
            [PurchaseBatchStatus.Arrived] = [PurchaseBatchStatus.Completed],
            [PurchaseBatchStatus.Completed] = [],
            [PurchaseBatchStatus.Cancelled] = []
        };

        private readonly IApplicationDbContext _context;

        public AdminPurchaseBatchService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AdminPurchaseBatchDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var batches = await _context.PurchaseBatches
                .OrderByDescending(b => b.OpenDate)
                .ToListAsync(cancellationToken);

            var productCounts = await _context.Products
                .Where(p => p.PurchaseBatchId != null)
                .GroupBy(p => p.PurchaseBatchId!.Value)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.BatchId, g => g.Count, cancellationToken);

            return batches.Select(b => MapToDto(b, productCounts.GetValueOrDefault(b.Id, 0))).ToList();
        }

        public async Task<AdminPurchaseBatchDto> CreateAsync(
            CreatePurchaseBatchRequest request, CancellationToken cancellationToken = default)
        {
            if (request.CloseDate <= request.OpenDate)
            {
                throw new DomainException("Ngày chốt đơn phải sau ngày mở đợt");
            }

            var batch = new PurchaseBatch
            {
                Name = request.Name,
                CountryOfOrigin = request.CountryOfOrigin,
                OpenDate = request.OpenDate,
                CloseDate = request.CloseDate,
                EstimatedArrivalDate = request.EstimatedArrivalDate,
                Status = PurchaseBatchStatus.Open
            };

            _context.PurchaseBatches.Add(batch);
            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(batch, 0);
        }

        public async Task<AdminPurchaseBatchDto> UpdateAsync(
            Guid id, UpdatePurchaseBatchRequest request, CancellationToken cancellationToken = default)
        {
            var batch = await _context.PurchaseBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                ?? throw new NotFoundException("đợt hàng", id.ToString());

            if (request.CloseDate <= request.OpenDate)
            {
                throw new DomainException("Ngày chốt đơn phải sau ngày mở đợt");
            }

            batch.Name = request.Name;
            batch.CountryOfOrigin = request.CountryOfOrigin;
            batch.OpenDate = request.OpenDate;
            batch.CloseDate = request.CloseDate;
            batch.EstimatedArrivalDate = request.EstimatedArrivalDate;

            await _context.SaveChangesAsync(cancellationToken);

            var productCount = await _context.Products.CountAsync(p => p.PurchaseBatchId == id, cancellationToken);
            return MapToDto(batch, productCount);
        }

        public async Task<AdminPurchaseBatchDto> UpdateStatusAsync(
            Guid id, UpdatePurchaseBatchStatusRequest request, CancellationToken cancellationToken = default)
        {
            var batch = await _context.PurchaseBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                ?? throw new NotFoundException("đợt hàng", id.ToString());

            if (!Enum.TryParse<PurchaseBatchStatus>(request.Status, out var newStatus))
            {
                throw new DomainException("Trạng thái không hợp lệ");
            }

            var allowedNextStatuses = ValidTransitions[batch.Status];
            if (!allowedNextStatuses.Contains(newStatus))
            {
                throw new DomainException(
                    $"Không thể chuyển từ trạng thái \"{batch.Status}\" sang \"{newStatus}\"");
            }

            batch.Status = newStatus;

            // Hủy đợt hàng -> TỰ ĐỘNG hủy các đơn CHƯA đi mua (Pending/Confirmed) có sản phẩm
            // thuộc đợt này, đồng thời giải phóng tồn kho đã giữ chỗ. Đơn đã qua Purchasing
            // trở đi KHÔNG bị đụng vào — tiền đã thực sự chi ra, nằm ngoài phạm vi hủy đợt.
            // (State machine của PurchaseBatch chỉ cho phép Cancelled từ Open/Closed, tức là
            // TRƯỚC khi Purchasing bắt đầu, nên về logic mọi đơn liên quan lúc này chỉ có thể
            // đang ở Pending/Confirmed — trừ khi Admin trước đó lỡ đẩy đơn vượt quá, vẫn được
            // xử lý an toàn nhờ điều kiện lọc theo Status bên dưới)
            if (newStatus == PurchaseBatchStatus.Cancelled)
            {
                var affectedOrders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o =>
                        (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed) &&
                        o.Items.Any(i => i.PurchaseBatchId == id))
                    .ToListAsync(cancellationToken);

                if (affectedOrders.Count > 0)
                {
                    var variantIds = affectedOrders
                        .SelectMany(o => o.Items)
                        .Select(i => i.ProductVariantId)
                        .Distinct()
                        .ToList();

                    var variants = await _context.ProductVariants
                        .Where(v => variantIds.Contains(v.Id))
                        .ToDictionaryAsync(v => v.Id, cancellationToken);

                    foreach (var order in affectedOrders)
                    {
                        foreach (var item in order.Items)
                        {
                            if (variants.TryGetValue(item.ProductVariantId, out var variant))
                            {
                                variant.ReservedQuantity =
                                    Math.Max(0, variant.ReservedQuantity - item.Quantity);
                            }
                        }

                        _context.OrderStatusHistories.Add(new OrderStatusHistory
                        {
                            OrderId = order.Id,
                            FromStatus = order.Status,
                            ToStatus = OrderStatus.Cancelled,
                            ChangedByUserId = null,
                            Note = $"Tự động hủy do đợt hàng \"{batch.Name}\" đã bị hủy",
                            ChangedAt = DateTime.UtcNow
                        });

                        order.Status = OrderStatus.Cancelled;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var productCount = await _context.Products.CountAsync(p => p.PurchaseBatchId == id, cancellationToken);
            return MapToDto(batch, productCount);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var batch = await _context.PurchaseBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                ?? throw new NotFoundException("đợt hàng", id.ToString());

            var hasProducts = await _context.Products.AnyAsync(p => p.PurchaseBatchId == id, cancellationToken);
            if (hasProducts)
            {
                throw new ConflictException(
                    "Không thể xóa đợt hàng đang có sản phẩm gắn vào. Vui lòng gỡ sản phẩm khỏi đợt hàng trước.");
            }

            _context.PurchaseBatches.Remove(batch);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static AdminPurchaseBatchDto MapToDto(PurchaseBatch batch, int productCount) => new()
        {
            Id = batch.Id,
            Name = batch.Name,
            CountryOfOrigin = batch.CountryOfOrigin,
            OpenDate = batch.OpenDate,
            CloseDate = batch.CloseDate,
            EstimatedArrivalDate = batch.EstimatedArrivalDate,
            Status = batch.Status.ToString(),
            ProductCount = productCount
        };
    }
}
