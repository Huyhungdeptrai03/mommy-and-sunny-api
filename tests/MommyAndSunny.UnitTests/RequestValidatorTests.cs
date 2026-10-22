using MommyAndSunny.Application.Admin.PurchaseBatches.DTOs;
using MommyAndSunny.Application.Admin.PurchaseBatches.Validators;
using MommyAndSunny.Application.Cart.DTOs;
using MommyAndSunny.Application.Common.Validation;
using MommyAndSunny.Application.Products.DTOs;

namespace MommyAndSunny.UnitTests;

public class RequestValidatorTests
{
    [Fact]
    public async Task CartQuantity_RejectsZero()
    {
        var result = await new AddCartItemRequestValidator().ValidateAsync(new AddCartItemRequest
        {
            ProductVariantId = Guid.NewGuid(),
            Quantity = 0
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddCartItemRequest.Quantity));
    }

    [Fact]
    public async Task ProductFilter_RejectsInvalidPriceRangeAndPageSize()
    {
        var result = await new ProductFilterRequestValidator().ValidateAsync(new ProductFilterRequest
        {
            MinPrice = 500,
            MaxPrice = 100,
            PageSize = 101
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ProductFilterRequest.MaxPrice));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ProductFilterRequest.PageSize));
    }

    [Fact]
    public async Task PurchaseBatch_RejectsInvalidDateOrder()
    {
        var openDate = DateTime.UtcNow;
        var result = await new CreatePurchaseBatchRequestValidator().ValidateAsync(new CreatePurchaseBatchRequest
        {
            Name = "Test batch",
            OpenDate = openDate,
            CloseDate = openDate.AddDays(-1),
            EstimatedArrivalDate = openDate.AddDays(-2)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePurchaseBatchRequest.CloseDate));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePurchaseBatchRequest.EstimatedArrivalDate));
    }
}
