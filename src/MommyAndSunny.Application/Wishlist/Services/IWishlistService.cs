using MommyAndSunny.Application.Wishlist.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Wishlist.Services
{
    public interface IWishlistService
    {
        Task<List<WishlistItemDto>> GetMyWishlistAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Guid productId, CancellationToken cancellationToken = default);
        Task RemoveAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
