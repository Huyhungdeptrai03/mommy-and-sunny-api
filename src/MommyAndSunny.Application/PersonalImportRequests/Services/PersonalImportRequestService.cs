using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.PersonalImportRequests.DTOs;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.PersonalImportRequests.Services
{
    public class PersonalImportRequestService : IPersonalImportRequestService
    {
        private readonly IApplicationDbContext _context;

        public PersonalImportRequestService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PersonalImportRequestDto> CreateAsync(
            Guid? userId, CreatePersonalImportRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.ProductName))
            {
                throw new DomainException("Vui lòng nhập tên sản phẩm muốn đặt");
            }
            if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                throw new DomainException("Vui lòng nhập tên và số điện thoại liên hệ");
            }
            if (request.Quantity < 1)
            {
                throw new DomainException("Số lượng phải lớn hơn 0");
            }

            var entity = new PersonalImportRequest
            {
                ProductName = request.ProductName.Trim(),
                ProductLink = request.ProductLink?.Trim(),
                Quantity = request.Quantity,
                Notes = request.Notes?.Trim(),
                ImageUrl = request.ImageUrl?.Trim(),
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone.Trim(),
                CustomerEmail = request.CustomerEmail?.Trim(),
                UserId = userId
            };

            _context.PersonalImportRequests.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return new PersonalImportRequestDto
            {
                Id = entity.Id,
                ProductName = entity.ProductName,
                Status = entity.Status.ToString(),
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
