using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MommyAndSunny.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueMomoTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_TransactionId",
                table: "Payments",
                columns: new[] { "Provider", "TransactionId" },
                unique: true,
                filter: "\"TransactionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_TransactionId",
                table: "Payments");
        }
    }
}
