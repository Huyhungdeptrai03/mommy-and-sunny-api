using MommyAndSunny.Application.Coupons.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Coupons.Services
{
    public interface IAdminCouponService
    {
        Task<List<AdminCouponDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<AdminCouponDto> CreateAsync(CreateCouponRequest request, CancellationToken cancellationToken = default);
        Task<AdminCouponDto> UpdateAsync(Guid id, UpdateCouponRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }

}
