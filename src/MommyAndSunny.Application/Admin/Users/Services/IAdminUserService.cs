using MommyAndSunny.Application.Admin.Users.DTOs;
using MommyAndSunny.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Users.Services
{
    public interface IAdminUserService
    {
        Task<PagedResult<AdminUserDto>> GetUsersAsync(
            AdminUserFilterRequest filter, CancellationToken cancellationToken = default);

        Task<AdminUserDto> UpdateRoleAsync(
            Guid id, Guid currentAdminId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
    }
}
