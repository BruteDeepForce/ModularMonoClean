using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Tables.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tables");

            migrationBuilder.CreateTable(
                name: "Tables",
                schema: "tables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Hall = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MinCapacity = table.Column<int>(type: "integer", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: false),
                    PositionX = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PositionY = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MergedIntoTableId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    QrToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tables", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tables_BranchId_Code",
                schema: "tables",
                table: "Tables",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tables_BranchId_Hall",
                schema: "tables",
                table: "Tables",
                columns: new[] { "BranchId", "Hall" });

            migrationBuilder.CreateIndex(
                name: "IX_Tables_BranchId_QrToken",
                schema: "tables",
                table: "Tables",
                columns: new[] { "BranchId", "QrToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tables_BranchId_Status",
                schema: "tables",
                table: "Tables",
                columns: new[] { "BranchId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tables",
                schema: "tables");
        }
    }
}
