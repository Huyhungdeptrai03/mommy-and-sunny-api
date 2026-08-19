using MommyAndSunny.Application.PurchaseBatches.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.PurchaseBatches.Services
{
    public interface IPublicPurchaseBatchService
    {
        Task<List<PublicPurchaseBatchDto>> GetOpenBatchesAsync(CancellationToken cancellationToken = default);
    }
}
