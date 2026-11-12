using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoetryPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorNameToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "AspNetUsers");
        }
    }
}
