using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Admin.Policies.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Admin.Policies.Services
{
    public class AdminPolicyService : IAdminPolicyService
    {
        private readonly IApplicationDbContext _context;

        public AdminPolicyService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AdminPolicyDto>> GetPoliciesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Policies
                .OrderBy(p => p.Slug)
                .Select(p => new AdminPolicyDto
                {
                    Id = p.Id,
                    Slug = p.Slug,
                    Title = p.Title,
                    Content = p.Content,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<AdminPolicyDto> GetPolicyByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (policy is null)
            {
                throw new NotFoundException("chính sách", id);
            }

            return MapToDto(policy);
        }

        public async Task<AdminPolicyDto> UpdatePolicyAsync(Guid id, UpdatePolicyRequest request, CancellationToken cancellationToken = default)
        {
            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (policy is null)
            {
                throw new NotFoundException("chính sách", id);
            }

            // Cố ý KHÔNG cho sửa Slug — 4 trang chính sách cố định, đổi slug sẽ vỡ link
            // đã gắn sẵn trong Footer và mọi nơi khác trỏ tới
            policy.Title = request.Title;
            policy.Content = request.Content;

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(policy);
        }

        private static AdminPolicyDto MapToDto(Domain.Entities.Policy policy)
        {
            return new AdminPolicyDto
            {
                Id = policy.Id,
                Slug = policy.Slug,
                Title = policy.Title,
                Content = policy.Content,
                UpdatedAt = policy.UpdatedAt
            };
        }
    }
}
