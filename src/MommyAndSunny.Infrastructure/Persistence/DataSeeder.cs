using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MommyAndSunny.Infrastructure.Persistence
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Nếu đã có dữ liệu (đã seed trước đó), bỏ qua — tránh tạo trùng mỗi lần chạy app
            if (await context.Categories.AnyAsync())
            {
                return;
            }

            // ===== 1. Categories (có danh mục cha/con) =====
            var catQuanAo = new Category { Name = "Quần áo", Slug = "quan-ao" };
            var catQuanAoBeTrai = new Category { Name = "Quần áo bé trai", Slug = "quan-ao-be-trai", ParentCategory = catQuanAo };
            var catQuanAoBeGai = new Category { Name = "Quần áo bé gái", Slug = "quan-ao-be-gai", ParentCategory = catQuanAo };

            var catDoDung = new Category { Name = "Đồ dùng mẹ & bé", Slug = "do-dung-me-be" };
            var catBinhSua = new Category { Name = "Bình sữa", Slug = "binh-sua", ParentCategory = catDoDung };
            var catTaBim = new Category { Name = "Tã - Bỉm", Slug = "ta-bim", ParentCategory = catDoDung };

            var catDoChoi = new Category { Name = "Đồ chơi", Slug = "do-choi" };

            await context.Categories.AddRangeAsync(
                catQuanAo, catQuanAoBeTrai, catQuanAoBeGai,
                catDoDung, catBinhSua, catTaBim,
                catDoChoi);

            // ===== 2. Products (kèm Images + Variants qua object graph — EF tự gán FK) =====

            var product1 = new Product
            {
                Category = catQuanAoBeTrai,
                Name = "Bộ quần áo cotton form rộng",
                Slug = "bo-quan-ao-cotton-form-rong",
                Description = "Chất liệu cotton 100% thoáng mát, form rộng rãi cho bé thoải mái vận động.",
                Brand = "Mommy Basic",
                AgeRangeMinMonths = 0,
                AgeRangeMaxMonths = 6,
                Status = ProductStatus.Published,
                Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://placehold.co/600x600?text=Bo+Quan+Ao+Cotton", IsPrimary = true, DisplayOrder = 0 }
            },
                Variants = new List<ProductVariant>
            {
                new() { Sku = "MS-BQA-S-TRANG", Size = "S", Color = "Trắng", Price = 150000, StockQuantity = 20 },
                new() { Sku = "MS-BQA-M-TRANG", Size = "M", Color = "Trắng", Price = 160000, StockQuantity = 15 },
                new() { Sku = "MS-BQA-S-XANH", Size = "S", Color = "Xanh", Price = 150000, StockQuantity = 10 },
            }
            };

            var product2 = new Product
            {
                Category = catQuanAoBeGai,
                Name = "Váy liền thân bé gái",
                Slug = "vay-lien-than-be-gai",
                Description = "Váy liền thân họa tiết hoa nhí, chất liệu cotton mềm mại.",
                Brand = "Sunny Kids",
                AgeRangeMinMonths = 6,
                AgeRangeMaxMonths = 12,
                Status = ProductStatus.Published,
                Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://placehold.co/600x600?text=Vay+Lien+Than", IsPrimary = true, DisplayOrder = 0 }
            },
                Variants = new List<ProductVariant>
            {
                new() { Sku = "MS-VLT-M-HONG", Size = "M", Color = "Hồng", Price = 180000, StockQuantity = 12 },
            }
            };

            var product3 = new Product
            {
                Category = catBinhSua,
                Name = "Bình sữa cổ rộng chống sặc 150ml",
                Slug = "binh-sua-co-rong-chong-sac-150ml",
                Description = "Núm ty silicon mềm, thiết kế chống sặc, chống đầy hơi cho bé.",
                Brand = "BabyCare",
                AgeRangeMinMonths = 0,
                AgeRangeMaxMonths = 6,
                Status = ProductStatus.Published,
                Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://placehold.co/600x600?text=Binh+Sua+150ml", IsPrimary = true, DisplayOrder = 0 }
            },
                Variants = new List<ProductVariant>
            {
                // Sản phẩm không có size/màu — Size và Color để null, minh họa trường hợp biến thể đơn giản
                new() { Sku = "MS-BS150-01", Price = 120000, StockQuantity = 30 },
            }
            };

            var product4 = new Product
            {
                Category = catTaBim,
                Name = "Tã dán sơ sinh size S",
                Slug = "ta-dan-so-sinh-size-s",
                Description = "Bề mặt bông mềm mịn, thấm hút nhanh, phù hợp bé sơ sinh.",
                Brand = "SoftDry",
                AgeRangeMinMonths = 0,
                AgeRangeMaxMonths = 3,
                Status = ProductStatus.Published,
                Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://placehold.co/600x600?text=Ta+Dan+Size+S", IsPrimary = true, DisplayOrder = 0 }
            },
                Variants = new List<ProductVariant>
            {
                new() { Sku = "MS-TADAN-S-30", Size = "S", Price = 95000, StockQuantity = 50 },
            }
            };

            // Cố ý để Draft — dùng để test luồng "sản phẩm chưa xuất bản không hiển thị ở storefront"
            var product5 = new Product
            {
                Category = catDoChoi,
                Name = "Xúc xắc treo nôi phát nhạc",
                Slug = "xuc-xac-treo-noi-phat-nhac",
                Description = "Treo nôi phát nhạc êm dịu, kích thích thị giác và thính giác cho bé.",
                Brand = "PlayFun",
                AgeRangeMinMonths = 0,
                AgeRangeMaxMonths = 12,
                Status = ProductStatus.Draft,
                Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://placehold.co/600x600?text=Xuc+Xac+Treo+Noi", IsPrimary = true, DisplayOrder = 0 }
            },
                Variants = new List<ProductVariant>
            {
                new() { Sku = "MS-XXTN-01", Price = 85000, StockQuantity = 8 },
            }
            };

            await context.Products.AddRangeAsync(product1, product2, product3, product4, product5);

            await context.SaveChangesAsync();
        }
    }
}
