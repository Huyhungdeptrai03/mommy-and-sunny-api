using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Common.Interfaces
{

    // Application layer chỉ biết đến interface này, không biết ApplicationDbContext (Infrastructure)
    // là EF Core + PostgreSQL cụ thể — giữ đúng nguyên tắc phụ thuộc 1 chiều đã thống nhất.
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Address> Addresses { get; }
        DbSet<Category> Categories { get; }
        DbSet<Product> Products { get; }
        DbSet<Article> Articles { get; }
        DbSet<ProductImage> ProductImages { get; }
        DbSet<ProductSpecification> ProductSpecifications { get; }
        DbSet<ProductVariant> ProductVariants { get; }
        // Ghi đầy đủ MommyAndSunny.Domain.Entities.Cart (thay vì chỉ "Cart") —
        // vì namespace MommyAndSunny.Application (cha của file này) giờ có chứa
        // namespace con "Cart" (từ module Application.Cart.*), khiến compiler nhầm
        // "Cart" là namespace thay vì Entity nếu chỉ viết tên ngắn
        DbSet<MommyAndSunny.Domain.Entities.Cart> Carts { get; }
        DbSet<CartItem> CartItems { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<OrderStatusHistory> OrderStatusHistories { get; }
        DbSet<Payment> Payments { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<Review> Reviews { get; }
        DbSet<PurchaseBatch> PurchaseBatches { get; }
        DbSet<Coupon> Coupons { get; }
        DbSet<WishlistItem> WishlistItems { get; }
        DbSet<Policy> Policies { get; }
        DbSet<ArticleComment> ArticleComments { get; }
        DbSet<PersonalImportRequest> PersonalImportRequests { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
