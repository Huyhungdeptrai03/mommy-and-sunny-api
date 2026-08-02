using MommyAndSunny.Application.Admin.Categories.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Categories.Services
{
    public interface IAdminCategoryService
    {
        Task<List<AdminCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<AdminCategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
        Task<AdminCategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
