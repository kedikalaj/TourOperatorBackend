using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourOperator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigraiton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PricingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourOperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeasonCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EconomySeats = table.Column<int>(type: "int", nullable: false),
                    BusinessSeats = table.Column<int>(type: "int", nullable: false),
                    EconomyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BusinessPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TourOperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRecords_TourOperatorId",
                table: "PricingRecords",
                column: "TourOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingRecords");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
