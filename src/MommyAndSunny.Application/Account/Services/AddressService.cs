using MommyAndSunny.Application.Account.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Account.Services
{
    public class AddressService : IAddressService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AddressService(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<AddressDto>> GetMyAddressesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            return await _context.Addresses
                .Where(a => a.UserId == userId)
                .Select(a => new AddressDto
                {
                    Id = a.Id,
                    RecipientName = a.RecipientName,
                    PhoneNumber = a.PhoneNumber,
                    Province = a.Province,
                    District = a.District,
                    Ward = a.Ward,
                    DetailAddress = a.DetailAddress,
                    IsDefault = a.IsDefault
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<AddressDto> CreateAddressAsync(CreateAddressRequest request, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ cũ trước — chỉ 1 địa chỉ mặc định tại 1 thời điểm
            if (request.IsDefault)
            {
                var currentDefaults = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync(cancellationToken);

                foreach (var addr in currentDefaults)
                {
                    addr.IsDefault = false;
                }
            }

            var address = new Address
            {
                UserId = userId,
                RecipientName = request.RecipientName,
                PhoneNumber = request.PhoneNumber,
                Province = request.Province,
                District = request.District,
                Ward = request.Ward,
                DetailAddress = request.DetailAddress,
                IsDefault = request.IsDefault
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync(cancellationToken);

            return new AddressDto
            {
                Id = address.Id,
                RecipientName = address.RecipientName,
                PhoneNumber = address.PhoneNumber,
                Province = address.Province,
                District = address.District,
                Ward = address.Ward,
                DetailAddress = address.DetailAddress,
                IsDefault = address.IsDefault
            };
        }
    }
}
