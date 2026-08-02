using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MommyAndSunny.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSaleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CompareAtPrice",
                table: "ProductVariants",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleEndsAt",
                table: "ProductVariants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompareAtPrice",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "SaleEndsAt",
                table: "ProductVariants");
        }
    }
}
