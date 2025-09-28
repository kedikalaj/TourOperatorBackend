using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TourOperator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DBSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "PasswordHash", "Role", "TourOperatorId" },
                values: new object[,]
                {
                    { new Guid("869b02d4-9e30-40c7-9f25-24ead5bf27a7"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "kedi.tourop@example.com", "$2a$11$4rR/8DCe8AvhwX9KeA1fQ.nDR34bdzVf1wGQ/rPPoFH.gjP5vLFBm", "TourOperator", null },
                    { new Guid("98def145-30b0-424b-92e2-68ded6787dfa"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "kedi.admin@example.com", "$2a$11$Epo02T.BFCuAbuRsYeIafuYB8IoBhNWEIl2ZTCOqVSGAQ4SD1Y31.", "Admin", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("869b02d4-9e30-40c7-9f25-24ead5bf27a7"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("98def145-30b0-424b-92e2-68ded6787dfa"));
        }
    }
}
