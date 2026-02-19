using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init_Orders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TableId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrderType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ServiceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId_UserId_CreatedAtUtc",
                schema: "orders",
                table: "Orders",
                columns: new[] { "BranchId", "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId_OrderNo",
                schema: "orders",
                table: "Orders",
                columns: new[] { "BranchId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId_Status_CreatedAtUtc",
                schema: "orders",
                table: "Orders",
                columns: new[] { "BranchId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId_TableId_Status",
                schema: "orders",
                table: "Orders",
                columns: new[] { "BranchId", "TableId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders",
                schema: "orders");
        }
    }
}
