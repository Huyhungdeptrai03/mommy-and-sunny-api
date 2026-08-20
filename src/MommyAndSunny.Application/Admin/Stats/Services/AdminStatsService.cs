using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Admin.Stats.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Application.Admin.Stats.Services
{
    public class AdminStatsService : IAdminStatsService
    {
        private const int MaxMonths = 36;

        private readonly IApplicationDbContext _context;

        public AdminStatsService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MonthlyStatsDto>> GetMonthlyStatsAsync(int months, CancellationToken cancellationToken = default)
        {
            if (months <= 0) months = 12;
            if (months > MaxMonths) months = MaxMonths;

            var todayUtc = DateTime.UtcNow.Date;
            // Mốc đầu tháng, lùi lại (months - 1) tháng — để tháng hiện tại luôn nằm trong khoảng
            var fromDate = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(months - 1));

            var rawData = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new MonthlyStatsDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count(),
                    // Chỉ tính doanh thu từ đơn ĐÃ GIAO — khớp định nghĩa "doanh thu" đã dùng
                    // xuyên suốt ở Dashboard, tránh doanh thu ảo từ đơn chưa hoàn tất
                    Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            // Điền đủ mọi tháng trong khoảng (kể cả tháng không có đơn) để FE vẽ biểu đồ liên tục
            var result = new List<MonthlyStatsDto>();
            for (var cursor = fromDate; cursor <= todayUtc; cursor = cursor.AddMonths(1))
            {
                var match = rawData.FirstOrDefault(r => r.Year == cursor.Year && r.Month == cursor.Month);
                result.Add(match ?? new MonthlyStatsDto { Year = cursor.Year, Month = cursor.Month, OrderCount = 0, Revenue = 0 });
            }

            return result;
        }

        public async Task<PagedResult<CustomerStatsItemDto>> GetCustomerStatsAsync(
            CustomerStatsFilterRequest filter, CancellationToken cancellationToken = default)
        {
            var grouped =
                from o in _context.Orders
                group o by o.UserId into g
                select new
                {
                    UserId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
                };

            var query =
                from g in grouped
                join u in _context.Users on g.UserId equals u.Id
                select new CustomerStatsItemDto
                {
                    UserId = g.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    OrderCount = g.OrderCount,
                    TotalSpent = g.TotalSpent
                };

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(term) || c.Email.ToLower().Contains(term));
            }

            query = query.OrderByDescending(c => c.TotalSpent);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<CustomerStatsItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
    }
}
