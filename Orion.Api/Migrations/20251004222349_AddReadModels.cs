using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Orion.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReadModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderDetailViews",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderItemsJson = table.Column<string>(type: "text", nullable: false),
                    StatusHistoryJson = table.Column<string>(type: "text", nullable: false),
                    Age = table.Column<TimeSpan>(type: "interval", nullable: false),
                    AgeDisplay = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetailViews", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "OrderSummaryViews",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    StatusDisplay = table.Column<string>(type: "text", nullable: false),
                    FormattedAmount = table.Column<string>(type: "text", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsPending = table.Column<bool>(type: "boolean", nullable: false),
                    IsFailed = table.Column<bool>(type: "boolean", nullable: false),
                    SearchText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSummaryViews", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "UserOrderHistoryViews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    FormattedAmount = table.Column<string>(type: "text", nullable: false),
                    IsRecent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrderHistoryViews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailViews_CreatedAt",
                table: "OrderDetailViews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailViews_Status",
                table: "OrderDetailViews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailViews_UserId",
                table: "OrderDetailViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryViews_CreatedAt",
                table: "OrderSummaryViews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryViews_SearchText",
                table: "OrderSummaryViews",
                column: "SearchText");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryViews_Status",
                table: "OrderSummaryViews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSummaryViews_UserId",
                table: "OrderSummaryViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrderHistoryViews_Status",
                table: "UserOrderHistoryViews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrderHistoryViews_UserId_CreatedAt",
                table: "UserOrderHistoryViews",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderDetailViews");

            migrationBuilder.DropTable(
                name: "OrderSummaryViews");

            migrationBuilder.DropTable(
                name: "UserOrderHistoryViews");
        }
    }
}
