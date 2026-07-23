using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Application.Orders.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Orders.Services
{
    public interface IOrderService
    {
        Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
        Task<PagedResult<OrderListItemDto>> GetMyOrdersAsync(OrderFilterRequest filter, CancellationToken cancellationToken = default);
        Task<OrderDto> GetOrderByCodeAsync(string orderCode, CancellationToken cancellationToken = default);
        Task CancelOrderAsync(string orderCode, CancellationToken cancellationToken = default);
    }
}
