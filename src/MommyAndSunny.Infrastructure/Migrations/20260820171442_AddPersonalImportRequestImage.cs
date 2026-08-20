using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MommyAndSunny.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalImportRequestImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "PersonalImportRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "PersonalImportRequests");
        }
    }
}
