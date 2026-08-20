using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Admin.PersonalImportRequests.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Admin.PersonalImportRequests.Services
{
    public class AdminPersonalImportRequestService : IAdminPersonalImportRequestService
    {
        private readonly IApplicationDbContext _context;

        public AdminPersonalImportRequestService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AdminPersonalImportRequestDto>> GetRequestsAsync(
            AdminPersonalImportRequestFilterRequest filter, CancellationToken cancellationToken = default)
        {
            var query = _context.PersonalImportRequests.AsQueryable();

            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.ToLower();
                query = query.Where(r =>
                    r.ProductName.ToLower().Contains(term) ||
                    r.CustomerName.ToLower().Contains(term) ||
                    r.CustomerPhone.Contains(term));
            }

            query = query.OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminPersonalImportRequestDto>
            {
                Items = entities.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<AdminPersonalImportRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.PersonalImportRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new NotFoundException("yêu cầu đặt hàng xách tay", id.ToString());

            return MapToDto(entity);
        }

        public async Task<AdminPersonalImportRequestDto> UpdateStatusAsync(
            Guid id, UpdatePersonalImportRequestStatusRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _context.PersonalImportRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new NotFoundException("yêu cầu đặt hàng xách tay", id.ToString());

            entity.Status = request.Status;
            if (request.AdminNote is not null)
            {
                entity.AdminNote = request.AdminNote;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(entity);
        }

        private static AdminPersonalImportRequestDto MapToDto(PersonalImportRequest r)
        {
            return new AdminPersonalImportRequestDto
            {
                Id = r.Id,
                ProductName = r.ProductName,
                ProductLink = r.ProductLink,
                Quantity = r.Quantity,
                Notes = r.Notes,
                ImageUrl = r.ImageUrl,
                CustomerName = r.CustomerName,
                CustomerPhone = r.CustomerPhone,
                CustomerEmail = r.CustomerEmail,
                UserId = r.UserId,
                Status = r.Status.ToString(),
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt
            };
        }
    }
}
