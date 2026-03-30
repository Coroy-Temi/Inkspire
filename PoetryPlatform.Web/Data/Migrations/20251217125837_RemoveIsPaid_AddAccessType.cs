using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoetryPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsPaid_AddAccessType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPaid",
                table: "Poems",
                newName: "IsPublished");

            migrationBuilder.AddColumn<int>(
                name: "AccessType",
                table: "Poems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPurchaseUrl",
                table: "Poems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfPath",
                table: "Poems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessType",
                table: "Poems");

            migrationBuilder.DropColumn(
                name: "ExternalPurchaseUrl",
                table: "Poems");

            migrationBuilder.DropColumn(
                name: "PdfPath",
                table: "Poems");

            migrationBuilder.RenameColumn(
                name: "IsPublished",
                table: "Poems",
                newName: "IsPaid");
        }
    }
}
