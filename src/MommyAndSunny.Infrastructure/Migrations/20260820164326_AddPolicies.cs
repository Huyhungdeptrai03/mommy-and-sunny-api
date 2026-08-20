using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MommyAndSunny.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Policies",
                columns: new[] { "Id", "Content", "CreatedAt", "Slug", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a1a1a1a1-0001-0001-0001-000000000001"), "Nội dung chính sách đổi trả đang được cập nhật.", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "doi-tra", "Chính sách đổi trả", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1a1a1a1-0002-0002-0002-000000000002"), "Nội dung chính sách vận chuyển đang được cập nhật.", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "van-chuyen", "Chính sách vận chuyển", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1a1a1a1-0003-0003-0003-000000000003"), "Nội dung chính sách bảo mật đang được cập nhật.", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "bao-mat", "Chính sách bảo mật", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a1a1a1a1-0004-0004-0004-000000000004"), "Nội dung điều khoản dịch vụ đang được cập nhật.", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "dieu-khoan", "Điều khoản dịch vụ", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_Slug",
                table: "Policies",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Policies");
        }
    }
}
