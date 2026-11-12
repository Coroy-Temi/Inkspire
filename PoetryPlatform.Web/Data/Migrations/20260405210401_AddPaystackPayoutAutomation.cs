using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoetryPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaystackPayoutAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                table: "PayoutItems",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaystackRecipientCode",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaystackRecipientType",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaystackSubaccountCode",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaystackSubaccountId",
                table: "AuthorPayoutAccounts",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSettlementReference",
                table: "AuthorEarnings",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "AuthorEarnings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalReference",
                table: "PayoutItems");

            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "PaystackRecipientCode",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "PaystackRecipientType",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "PaystackSubaccountCode",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "PaystackSubaccountId",
                table: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "ExternalSettlementReference",
                table: "AuthorEarnings");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "AuthorEarnings");
        }
    }
}
