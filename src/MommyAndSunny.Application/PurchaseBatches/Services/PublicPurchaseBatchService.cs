using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.PurchaseBatches.DTOs;
using MommyAndSunny.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.PurchaseBatches.Services
{
    public class PublicPurchaseBatchService : IPublicPurchaseBatchService
    {
        private readonly IApplicationDbContext _context;

        public PublicPurchaseBatchService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PublicPurchaseBatchDto>> GetOpenBatchesAsync(CancellationToken cancellationToken = default)
        {
            var batches = await _context.PurchaseBatches
                .Where(b => b.Status == PurchaseBatchStatus.Open)
                .OrderBy(b => b.CloseDate)
                .ToListAsync(cancellationToken);

            // Chỉ đếm sản phẩm ĐÃ XUẤT BẢN — tránh quảng bá đợt hàng chưa có sản phẩm nào
            // khách thực sự xem/mua được (sản phẩm Draft không hiện với khách)
            var productCounts = await _context.Products
                .Where(p => p.PurchaseBatchId != null && p.Status == ProductStatus.Published)
                .GroupBy(p => p.PurchaseBatchId!.Value)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.BatchId, g => g.Count, cancellationToken);

            return batches
                .Select(b => new PublicPurchaseBatchDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    CountryOfOrigin = b.CountryOfOrigin,
                    CloseDate = b.CloseDate,
                    EstimatedArrivalDate = b.EstimatedArrivalDate,
                    ProductCount = productCounts.GetValueOrDefault(b.Id, 0)
                })
                .Where(b => b.ProductCount > 0)
                .ToList();
        }
    }
}
