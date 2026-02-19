using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_PhoneLoginCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhoneLoginCodes",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneLoginCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneLoginCodes_PhoneNumber_CodeHash",
                schema: "identity",
                table: "PhoneLoginCodes",
                columns: new[] { "PhoneNumber", "CodeHash" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneLoginCodes_UserId_ExpiresAtUtc",
                schema: "identity",
                table: "PhoneLoginCodes",
                columns: new[] { "UserId", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneLoginCodes",
                schema: "identity");
        }
    }
}
