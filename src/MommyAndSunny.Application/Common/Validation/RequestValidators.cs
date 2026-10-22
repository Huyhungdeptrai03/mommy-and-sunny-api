using FluentValidation;
using MommyAndSunny.Application.Admin.Articles.DTOs;
using MommyAndSunny.Application.Admin.Orders.DTOs;
using MommyAndSunny.Application.Admin.PersonalImportRequests.DTOs;
using MommyAndSunny.Application.Admin.Products.DTOs;
using MommyAndSunny.Application.Admin.PurchaseBatches.DTOs;
using MommyAndSunny.Application.Admin.Reviews.DTOs;
using MommyAndSunny.Application.Admin.Stats.DTOs;
using MommyAndSunny.Application.Admin.Users.DTOs;
using MommyAndSunny.Application.Articles.DTOs;
using MommyAndSunny.Application.Auth.DTOs;
using MommyAndSunny.Application.Cart.DTOs;
using MommyAndSunny.Application.Common.Payments.DTOs;
using MommyAndSunny.Application.Coupons.DTOs;
using MommyAndSunny.Application.Orders.DTOs;
using MommyAndSunny.Application.Products.DTOs;
using MommyAndSunny.Domain.Enums;
using System.Linq.Expressions;

namespace MommyAndSunny.Application.Common.Validation;

internal static class ValidationRules
{
    public static void AddPaging<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, int>> page,
        Expression<Func<T, int>> pageSize)
    {
        validator.RuleFor(page).InclusiveBetween(1, 1_000_000)
            .WithMessage("Trang phải lớn hơn hoặc bằng 1");
        validator.RuleFor(pageSize).InclusiveBetween(1, 100)
            .WithMessage("Số phần tử mỗi trang phải từ 1 đến 100");
    }

    public static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.ProductVariantId).NotEmpty().WithMessage("Biến thể sản phẩm không hợp lệ");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999).WithMessage("Số lượng phải từ 1 đến 999");
    }
}

public sealed class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator() =>
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999).WithMessage("Số lượng phải từ 1 đến 999");
}

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty().WithMessage("Vui lòng chọn địa chỉ giao hàng");
        RuleFor(x => x.PaymentMethod).Must(x => x == PaymentMethod.COD || x == PaymentMethod.SePay).WithMessage("Phương thức thanh toán không hợp lệ");
        RuleFor(x => x.CouponCode).MaximumLength(50);
        RuleFor(x => x.CustomerNote).MaximumLength(1_000);
    }
}

public sealed class ApplyCouponRequestValidator : AbstractValidator<ApplyCouponRequest>
{
    public ApplyCouponRequestValidator() => RuleFor(x => x.Code)
        .NotEmpty().WithMessage("Vui lòng nhập mã giảm giá")
        .MaximumLength(50).Matches("^[A-Za-z0-9_-]+$").WithMessage("Mã giảm giá không hợp lệ");
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator() => RuleFor(x => x.RefreshToken)
        .NotEmpty().WithMessage("Refresh token không được để trống").MaximumLength(2_000);
}

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator() => RuleFor(x => x.Token)
        .NotEmpty().WithMessage("Token xác thực không được để trống").MaximumLength(2_000);
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token đặt lại mật khẩu không được để trống").MaximumLength(2_000);
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("Mật khẩu mới không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự")
            .MaximumLength(128).WithMessage("Mật khẩu tối đa 128 ký tự");
    }
}

public sealed class SePayWebhookRequestValidator : AbstractValidator<SePayWebhookRequest>
{
    public SePayWebhookRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TransferType).Must(x => x == "in" || x == "out");
        RuleFor(x => x.TransferAmount).GreaterThan(0);
        RuleFor(x => x.Code).MaximumLength(100);
        RuleFor(x => x.Content).MaximumLength(2000);
    }
}

public sealed class CreateCouponRequestValidator : AbstractValidator<CreateCouponRequest>
{
    public CreateCouponRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.DiscountType).Must(value => Enum.TryParse<CouponDiscountType>(value, true, out _))
            .WithMessage("Loại giảm giá không hợp lệ");
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100)
            .When(x => string.Equals(x.DiscountType, nameof(CouponDiscountType.Percentage), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Phần trăm giảm giá không được vượt quá 100");
        RuleFor(x => x.MaxDiscountAmount).GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue);
        RuleFor(x => x.MinOrderValue).GreaterThanOrEqualTo(0).When(x => x.MinOrderValue.HasValue);
        RuleFor(x => x.TotalUsageLimit).GreaterThan(0).When(x => x.TotalUsageLimit.HasValue);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau ngày bắt đầu");
    }
}

public sealed class UpdateCouponRequestValidator : AbstractValidator<UpdateCouponRequest>
{
    public UpdateCouponRequestValidator()
    {
        RuleFor(x => x.DiscountType).Must(value => Enum.TryParse<CouponDiscountType>(value, true, out _))
            .WithMessage("Loại giảm giá không hợp lệ");
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100)
            .When(x => string.Equals(x.DiscountType, nameof(CouponDiscountType.Percentage), StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.MaxDiscountAmount).GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue);
        RuleFor(x => x.MinOrderValue).GreaterThanOrEqualTo(0).When(x => x.MinOrderValue.HasValue);
        RuleFor(x => x.TotalUsageLimit).GreaterThan(0).When(x => x.TotalUsageLimit.HasValue);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau ngày bắt đầu");
    }
}

public sealed class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator() => RuleFor(x => x.Role)
        .Must(value => Enum.TryParse<UserRole>(value, true, out _)).WithMessage("Vai trò không hợp lệ");
}

public sealed class UpdateArticleStatusRequestValidator : AbstractValidator<UpdateArticleStatusRequest>
{
    public UpdateArticleStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class UpdateProductStatusRequestValidator : AbstractValidator<UpdateProductStatusRequest>
{
    public UpdateProductStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class UpdatePurchaseBatchStatusRequestValidator : AbstractValidator<UpdatePurchaseBatchStatusRequest>
{
    public UpdatePurchaseBatchStatusRequestValidator() => RuleFor(x => x.Status)
        .Must(value => Enum.TryParse<PurchaseBatchStatus>(value, true, out _)).WithMessage("Trạng thái đợt hàng không hợp lệ");
}

public sealed class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.ToStatus).IsInEnum().WithMessage("Trạng thái đơn hàng không hợp lệ");
        RuleFor(x => x.Note).MaximumLength(1_000);
    }
}

public sealed class SetOrderDepositRequestValidator : AbstractValidator<SetOrderDepositRequest>
{
    public SetOrderDepositRequestValidator() => RuleFor(x => x.DepositAmount)
        .GreaterThan(0).WithMessage("Tiền cọc phải lớn hơn 0");
}

public sealed class ProductFilterRequestValidator : AbstractValidator<ProductFilterRequest>
{
    public ProductFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.AgeMonths).InclusiveBetween(0, 360).When(x => x.AgeMonths.HasValue);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(x => x.MinPrice ?? 0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.CategorySlug).MaximumLength(200);
        RuleFor(x => x.SortBy).IsInEnum();
    }
}

public sealed class OrderFilterRequestValidator : AbstractValidator<OrderFilterRequest>
{
    public OrderFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Status!.Value).IsInEnum().When(x => x.Status.HasValue);
    }
}

public sealed class ArticleFilterRequestValidator : AbstractValidator<ArticleFilterRequest>
{
    public ArticleFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Category!.Value).IsInEnum().When(x => x.Category.HasValue);
    }
}

public sealed class AdminProductFilterRequestValidator : AbstractValidator<AdminProductFilterRequest>
{
    public AdminProductFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Status!.Value).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class AdminOrderFilterRequestValidator : AbstractValidator<AdminOrderFilterRequest>
{
    public AdminOrderFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Status!.Value).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue).WithMessage("Ngày kết thúc phải sau ngày bắt đầu");
    }
}

public sealed class AdminArticleFilterRequestValidator : AbstractValidator<AdminArticleFilterRequest>
{
    public AdminArticleFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Status!.Value).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Category!.Value).IsInEnum().When(x => x.Category.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class AdminArticleCommentFilterRequestValidator : AbstractValidator<AdminArticleCommentFilterRequest>
{
    public AdminArticleCommentFilterRequestValidator() => ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
}

public sealed class AdminPersonalImportRequestFilterRequestValidator : AbstractValidator<AdminPersonalImportRequestFilterRequest>
{
    public AdminPersonalImportRequestFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Status!.Value).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class AdminUserFilterRequestValidator : AbstractValidator<AdminUserFilterRequest>
{
    public AdminUserFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class CustomerStatsFilterRequestValidator : AbstractValidator<CustomerStatsFilterRequest>
{
    public CustomerStatsFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class AdminReviewFilterRequestValidator : AbstractValidator<AdminReviewFilterRequest>
{
    public AdminReviewFilterRequestValidator()
    {
        ValidationRules.AddPaging(this, x => x.Page, x => x.PageSize);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
    }
}
