using MommyAndSunny.Application.Admin.Orders.DTOs;
using MommyAndSunny.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Orders.Service
{
    public interface IAdminOrderService
    {
        Task<PagedResult<AdminOrderListItemDto>> GetOrdersAsync(AdminOrderFilterRequest filter, CancellationToken cancellationToken = default);
        Task<AdminOrderDetailDto> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task UpdateOrderStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
        Task<AdminOrderDetailDto> SetDepositAsync(Guid id, decimal depositAmount, CancellationToken cancellationToken = default);
        Task<AdminOrderDetailDto> MarkDepositPaidAsync(Guid id, CancellationToken cancellationToken = default);
        Task<AdminOrderDetailDto> MarkFinalPaymentPaidAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
