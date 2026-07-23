using MommyAndSunny.Application.Account.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Account.Services
{
    public interface IAddressService
    {
        Task<List<AddressDto>> GetMyAddressesAsync(CancellationToken cancellationToken = default);
        Task<AddressDto> CreateAddressAsync(CreateAddressRequest request, CancellationToken cancellationToken = default);
    }
}
