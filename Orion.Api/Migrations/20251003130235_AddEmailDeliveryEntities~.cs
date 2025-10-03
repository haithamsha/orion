using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Orion.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailDeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeliveryTimeMs = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailDeliveryAttempts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmailDeliveryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    EmailType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ToName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TemplateId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    TotalDeliveryTimeMs = table.Column<double>(type: "double precision", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDeliveryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailDeliveryLogs_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryAttempts_AttemptedAt",
                table: "EmailDeliveryAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryAttempts_OrderId",
                table: "EmailDeliveryAttempts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryAttempts_Success",
                table: "EmailDeliveryAttempts",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogs_CreatedAt",
                table: "EmailDeliveryLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogs_OrderId",
                table: "EmailDeliveryLogs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogs_Success",
                table: "EmailDeliveryLogs",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogs_ToEmail",
                table: "EmailDeliveryLogs",
                column: "ToEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailDeliveryAttempts");

            migrationBuilder.DropTable(
                name: "EmailDeliveryLogs");
        }
    }
}
