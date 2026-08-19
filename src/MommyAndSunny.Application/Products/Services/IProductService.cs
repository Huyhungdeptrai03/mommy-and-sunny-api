using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Application.Products.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.Services
{
    public interface IProductService
    {
        Task<PagedResult<ProductListItemDto>> GetProductsAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default);
        Task<ProductDto> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
        Task<List<FlashSaleItemDto>> GetFlashSaleAsync(CancellationToken cancellationToken = default);
        Task<List<string>> GetBrandsAsync(CancellationToken cancellationToken = default);
    }

}
