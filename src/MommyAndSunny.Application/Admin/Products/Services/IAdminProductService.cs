using MommyAndSunny.Application.Admin.Products.DTOs;
using MommyAndSunny.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.Services
{
    public interface IAdminProductService
    {
        Task<PagedResult<AdminProductListItemDto>> GetProductsAsync(AdminProductFilterRequest filter, CancellationToken cancellationToken = default);
        Task<AdminProductDetailDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<AdminProductDetailDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
        Task<AdminProductDetailDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
        Task UpdateProductStatusAsync(Guid id, UpdateProductStatusRequest request, CancellationToken cancellationToken = default);

        Task<ProductImageAdminDto> AddImageAsync(Guid productId, CreateProductImageRequest request, CancellationToken cancellationToken = default);
        Task DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);

        Task<ProductSpecificationAdminDto> AddSpecificationAsync(Guid productId, CreateProductSpecificationRequest request, CancellationToken cancellationToken = default);
        Task DeleteSpecificationAsync(Guid productId, Guid specificationId, CancellationToken cancellationToken = default);

        Task<ProductVariantAdminDto> AddVariantAsync(Guid productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default);
        Task<ProductVariantAdminDto> UpdateVariantAsync(Guid variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken = default);
        Task DeleteVariantAsync(Guid variantId, CancellationToken cancellationToken = default);
    }
}
