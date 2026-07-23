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

        public async Task<List<RevenueChartPointDto>> GetRevenueChartAsync(int days, CancellationToken cancellationToken = default)
        {
            var fromDate = DateTime.UtcNow.Date.AddDays(-(days - 1));

            var rawData = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.CreatedAt >= fromDate)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync(cancellationToken);

            // Điền đủ tất cả các ngày trong khoảng (kể cả ngày doanh thu = 0) —
            // để FE vẽ biểu đồ liên tục, không bị đứt quãng ở ngày không có đơn
            var result = new List<RevenueChartPointDto>();
            for (var date = fromDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
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
