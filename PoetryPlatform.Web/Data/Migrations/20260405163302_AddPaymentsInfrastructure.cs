using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoetryPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PoemId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyerId = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<string>(type: "TEXT", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PlatformFeeAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    AuthorNetAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExternalCheckoutSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalPaymentIntentId = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalChargeId = table.Column<string>(type: "TEXT", nullable: true),
                    CheckoutUrl = table.Column<string>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookOrders_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookOrders_AspNetUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookOrders_Poems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "Poems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    EventId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SignatureHeader = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ExternalEventId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ObjectType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    RawPayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_BookOrders_BookOrderId",
                        column: x => x.BookOrderId,
                        principalTable: "BookOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookOrders_AuthorId",
                table: "BookOrders",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_BookOrders_BuyerId",
                table: "BookOrders",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookOrders_OrderNumber",
                table: "BookOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookOrders_PoemId_BuyerId_Status",
                table: "BookOrders",
                columns: new[] { "PoemId", "BuyerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_BookOrderId_ExternalReference",
                table: "PaymentRecords",
                columns: new[] { "BookOrderId", "ExternalReference" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookLogs_Provider_EventId",
                table: "PaymentWebhookLogs",
                columns: new[] { "Provider", "EventId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "PaymentWebhookLogs");

            migrationBuilder.DropTable(
                name: "BookOrders");
        }
    }
}
