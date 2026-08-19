using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.PublicStats.DTOs;
using MommyAndSunny.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.PublicStats.Services
{
    public class PublicStatsService : IPublicStatsService
    {
        private readonly IApplicationDbContext _context;

        public PublicStatsService(IApplicationDbContext context)
        {
            _context = context;
        }

        // CỐ Ý chỉ trả số lượng (count), KHÔNG BAO GIỜ trả doanh thu/giá trị tiền tệ —
        // đây là endpoint công khai (không yêu cầu đăng nhập), khác hẳn
        // AdminDashboardController vốn có [Authorize(Roles = "Admin")]
        public async Task<PublicStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            return new PublicStatsDto
            {
                TotalProducts = await _context.Products
                    .CountAsync(p => p.Status == ProductStatus.Published, cancellationToken),
                TotalCategories = await _context.Categories.CountAsync(cancellationToken),
                TotalDeliveredOrders = await _context.Orders
                    .CountAsync(o => o.Status == OrderStatus.Delivered, cancellationToken),
                TotalCustomers = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Customer, cancellationToken)
            };
        }
    }
}
