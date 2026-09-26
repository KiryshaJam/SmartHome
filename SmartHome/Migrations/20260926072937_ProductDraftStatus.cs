using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Migrations
{
    /// <inheritdoc />
    public partial class ProductDraftStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "product",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "product",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsDraft",
                value: true);

            migrationBuilder.UpdateData(
                table: "product",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsDraft",
                value: true);

            migrationBuilder.UpdateData(
                table: "product",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsDraft",
                value: true);

            migrationBuilder.UpdateData(
                table: "product",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsDraft",
                value: true);

            migrationBuilder.UpdateData(
                table: "product",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsDraft",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "product");
        }
    }
}
