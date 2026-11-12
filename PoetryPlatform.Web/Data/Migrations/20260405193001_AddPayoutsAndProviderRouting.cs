using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoetryPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutsAndProviderRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PreferredCheckoutProvider",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthorPayoutAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthorId = table.Column<string>(type: "TEXT", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    StripeAccountId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ChargesEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PayoutsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetailsSubmitted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastOnboardingUrl = table.Column<string>(type: "TEXT", nullable: true),
                    RequirementsSummary = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastOnboardingAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorPayoutAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorPayoutAccounts_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayoutBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchReference = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReaderSubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Interval = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonthlyReadingCredits = table.Column<int>(type: "INTEGER", nullable: false),
                    IncludesPremiumLibrary = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderSubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthorEarnings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorId = table.Column<string>(type: "TEXT", nullable: false),
                    PayoutProvider = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorPayoutAccountId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PlatformFeeAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    NetAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorEarnings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorEarnings_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthorEarnings_AuthorPayoutAccounts_AuthorPayoutAccountId",
                        column: x => x.AuthorPayoutAccountId,
                        principalTable: "AuthorPayoutAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuthorEarnings_BookOrders_BookOrderId",
                        column: x => x.BookOrderId,
                        principalTable: "BookOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReaderSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReaderId = table.Column<string>(type: "TEXT", nullable: false),
                    ReaderSubscriptionPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ExternalCustomerId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReaderSubscriptions_AspNetUsers_ReaderId",
                        column: x => x.ReaderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReaderSubscriptions_ReaderSubscriptionPlans_ReaderSubscriptionPlanId",
                        column: x => x.ReaderSubscriptionPlanId,
                        principalTable: "ReaderSubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayoutItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PayoutBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorEarningId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorPayoutAccountId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExternalTransferId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutItems_AuthorEarnings_AuthorEarningId",
                        column: x => x.AuthorEarningId,
                        principalTable: "AuthorEarnings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayoutItems_AuthorPayoutAccounts_AuthorPayoutAccountId",
                        column: x => x.AuthorPayoutAccountId,
                        principalTable: "AuthorPayoutAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayoutItems_PayoutBatches_PayoutBatchId",
                        column: x => x.PayoutBatchId,
                        principalTable: "PayoutBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorEarnings_AuthorId_Status",
                table: "AuthorEarnings",
                columns: new[] { "AuthorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorEarnings_AuthorPayoutAccountId",
                table: "AuthorEarnings",
                column: "AuthorPayoutAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorEarnings_BookOrderId",
                table: "AuthorEarnings",
                column: "BookOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorPayoutAccounts_AuthorId_Provider",
                table: "AuthorPayoutAccounts",
                columns: new[] { "AuthorId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoutBatches_BatchReference",
                table: "PayoutBatches",
                column: "BatchReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoutItems_AuthorEarningId",
                table: "PayoutItems",
                column: "AuthorEarningId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoutItems_AuthorPayoutAccountId",
                table: "PayoutItems",
                column: "AuthorPayoutAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutItems_PayoutBatchId",
                table: "PayoutItems",
                column: "PayoutBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaderSubscriptions_ReaderId_Status",
                table: "ReaderSubscriptions",
                columns: new[] { "ReaderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReaderSubscriptions_ReaderSubscriptionPlanId",
                table: "ReaderSubscriptions",
                column: "ReaderSubscriptionPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoutItems");

            migrationBuilder.DropTable(
                name: "ReaderSubscriptions");

            migrationBuilder.DropTable(
                name: "AuthorEarnings");

            migrationBuilder.DropTable(
                name: "PayoutBatches");

            migrationBuilder.DropTable(
                name: "ReaderSubscriptionPlans");

            migrationBuilder.DropTable(
                name: "AuthorPayoutAccounts");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredCheckoutProvider",
                table: "AspNetUsers");
        }
    }
}
