using MommyAndSunny.Application.Admin.PurchaseBatches.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.PurchaseBatches.Services
{
    public interface IAdminPurchaseBatchService
    {
        Task<List<AdminPurchaseBatchDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<AdminPurchaseBatchDto> CreateAsync(
            CreatePurchaseBatchRequest request, CancellationToken cancellationToken = default);

        Task<AdminPurchaseBatchDto> UpdateAsync(
            Guid id, UpdatePurchaseBatchRequest request, CancellationToken cancellationToken = default);

        Task<AdminPurchaseBatchDto> UpdateStatusAsync(
            Guid id, UpdatePurchaseBatchStatusRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
