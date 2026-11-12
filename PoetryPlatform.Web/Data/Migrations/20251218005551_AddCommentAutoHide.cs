using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoetryPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentAutoHide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlagCount",
                table: "Comments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlagCount",
                table: "Comments");
        }
    }
}
