using MommyAndSunny.Application.Categories.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Categories.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetCategoryTreeAsync(CancellationToken cancellationToken = default);
    }
}
