using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MommyAndSunny.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatProductTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductTagsJson",
                table: "ChatMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductTagsJson",
                table: "ChatMessages");
        }
    }
}
