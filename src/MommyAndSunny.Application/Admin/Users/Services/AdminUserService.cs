using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Admin.Users.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Users.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IApplicationDbContext _context;

        public AdminUserService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
            AdminUserFilterRequest filter, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(search) || u.FullName.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = users.Select(u => u.Id).ToList();
            var orderCounts = await _context.Orders
                .Where(o => userIds.Contains(o.UserId))
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count, cancellationToken);

            var items = users.Select(u => new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                EmailVerified = u.EmailVerified,
                OrderCount = orderCounts.GetValueOrDefault(u.Id, 0),
                CreatedAt = u.CreatedAt
            }).ToList();

            return new PagedResult<AdminUserDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<AdminUserDto> UpdateRoleAsync(
            Guid id, Guid currentAdminId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            // Chặn Admin tự đổi quyền của chính mình — tránh tự khóa mình khỏi khu quản trị
            // (đặc biệt nguy hiểm nếu đây là Admin duy nhất của hệ thống)
            if (id == currentAdminId)
            {
                throw new DomainException("Không thể tự thay đổi quyền của chính mình");
            }

            if (!Enum.TryParse<UserRole>(request.Role, out var newRole))
            {
                throw new DomainException("Vai trò không hợp lệ");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                ?? throw new NotFoundException("người dùng", id.ToString());

            user.Role = newRole;
            await _context.SaveChangesAsync(cancellationToken);

            return new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                EmailVerified = user.EmailVerified,
                OrderCount = await _context.Orders.CountAsync(o => o.UserId == id, cancellationToken),
                CreatedAt = user.CreatedAt
            };
        }
    }

}
