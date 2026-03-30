using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoetryPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPoemLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PoemLikes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_PoemLikes_UserId",
                table: "PoemLikes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PoemLikes_AspNetUsers_UserId",
                table: "PoemLikes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PoemLikes_AspNetUsers_UserId",
                table: "PoemLikes");

            migrationBuilder.DropIndex(
                name: "IX_PoemLikes_UserId",
                table: "PoemLikes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PoemLikes");
        }
    }
}
