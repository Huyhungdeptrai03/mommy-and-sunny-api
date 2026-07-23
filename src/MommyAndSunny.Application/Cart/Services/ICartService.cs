using MommyAndSunny.Application.Cart.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Cart.Services
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(CancellationToken cancellationToken = default);
        Task<CartDto> AddItemAsync(AddCartItemRequest request, CancellationToken cancellationToken = default);
        Task<CartDto> UpdateItemAsync(Guid itemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
        Task RemoveItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    }
}
