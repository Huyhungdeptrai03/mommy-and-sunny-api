using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Policies.DTOs;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Policies.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly IApplicationDbContext _context;

        public PolicyService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PolicyDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
            if (policy is null)
            {
                throw new NotFoundException("chính sách", slug);
            }

            return new PolicyDto
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
