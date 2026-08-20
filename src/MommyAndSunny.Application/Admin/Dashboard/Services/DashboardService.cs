using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Admin.Dashboard.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Dashboard.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IApplicationDbContext _context;

        public DashboardService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var weekAgoUtc = todayUtc.AddDays(-6); // 7 ngày gần nhất, tính cả hôm nay

            // Chỉ tính doanh thu từ đơn ĐÃ GIAO THÀNH CÔNG — tránh doanh thu ảo
            // từ đơn chưa hoàn tất, đúng nghiệp vụ đã thống nhất từ đầu dự án
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0;

            var ordersToday = await _context.Orders
                .CountAsync(o => o.CreatedAt >= todayUtc, cancellationToken);

            var ordersThisWeek = await _context.Orders
                .CountAsync(o => o.CreatedAt >= weekAgoUtc, cancellationToken);

            var pendingOrdersCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);

            return new DashboardSummaryDto
            {
                TotalRevenue = totalRevenue,
                OrdersToday = ordersToday,
                OrdersThisWeek = ordersThisWeek,
                PendingOrdersCount = pendingOrdersCount
            };
        }

        private const int MaxRevenueChartRangeDays = 90;

        public async Task<List<RevenueChartPointDto>> GetRevenueChartAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
        {
            var todayUtc = DateTime.UtcNow.Date;
            // fromDate/toDate đến từ query string nên Kind luôn là Unspecified — Npgsql từ chối
            // so sánh với cột timestamptz nếu không ép rõ về UTC (khác biệt so với DashboardService
            // cũ dùng DateTime.UtcNow.Date, vốn đã sẵn Kind=Utc)
            var rangeTo = DateTime.SpecifyKind((toDate ?? todayUtc).Date, DateTimeKind.Utc);
            var rangeFrom = DateTime.SpecifyKind((fromDate ?? rangeTo.AddDays(-6)).Date, DateTimeKind.Utc);

            if (rangeFrom > rangeTo)
            {
                (rangeFrom, rangeTo) = (rangeTo, rangeFrom);
            }

            // Chặn khoảng quá rộng — khớp giới hạn "tối đa 90 ngày" đã ghi trên FE,
            // tránh query quét toàn bộ lịch sử đơn hàng nếu client gửi khoảng ngày bất thường
            if ((rangeTo - rangeFrom).Days > MaxRevenueChartRangeDays)
            {
                rangeFrom = rangeTo.AddDays(-MaxRevenueChartRangeDays);
            }

            var rawData = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.CreatedAt >= rangeFrom && o.CreatedAt < rangeTo.AddDays(1))
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync(cancellationToken);

            // Điền đủ tất cả các ngày trong khoảng (kể cả ngày doanh thu = 0) —
            // để FE vẽ biểu đồ liên tục, không bị đứt quãng ở ngày không có đơn
            var result = new List<RevenueChartPointDto>();
            for (var date = rangeFrom; date <= rangeTo; date = date.AddDays(1))
            {
                var match = rawData.FirstOrDefault(r => r.Date == date);
                result.Add(new RevenueChartPointDto
                {
                    Date = date,
                    Revenue = match?.Revenue ?? 0
                });
            }

            return result;
        }
    }

}
