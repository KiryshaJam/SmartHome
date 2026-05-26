using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartHome.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumSubsystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "enum_class",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ValueType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MeasureUnitId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enum_class", x => x.Id);
                    table.ForeignKey(
                        name: "FK_enum_class_measure_unit_MeasureUnitId",
                        column: x => x.MeasureUnitId,
                        principalTable: "measure_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "enum_value",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnumClassId = table.Column<int>(type: "integer", nullable: false),
                    StringValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    NumberValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    IconValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enum_value", x => x.Id);
                    table.ForeignKey(
                        name: "FK_enum_value_enum_class_EnumClassId",
                        column: x => x.EnumClassId,
                        principalTable: "enum_class",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "enum_class",
                columns: new[] { "Id", "MeasureUnitId", "Name", "ShortName", "SortOrder", "ValueType" },
                values: new object[,]
                {
                    { 1, null, "Тип подключения", "connection-kind", 1, 1 },
                    { 2, null, "Протокол связи", "communication-protocol", 2, 1 },
                    { 4, null, "Иконка устройства", "device-icon", 4, 3 }
                });

            migrationBuilder.InsertData(
                table: "measure_unit",
                columns: new[] { "Id", "Name", "ShortName" },
                values: new object[] { 5, "Ватт", "Вт" });

            migrationBuilder.InsertData(
                table: "enum_class",
                columns: new[] { "Id", "MeasureUnitId", "Name", "ShortName", "SortOrder", "ValueType" },
                values: new object[] { 3, 5, "Мощность лампы", "lamp-power", 3, 2 });

            migrationBuilder.InsertData(
                table: "enum_value",
                columns: new[] { "Id", "DisplayName", "EnumClassId", "IconValue", "NumberValue", "SortOrder", "StringValue" },
                values: new object[,]
                {
                    { 1, "Проводное подключение", 1, null, null, 1, "Проводное" },
                    { 2, "Беспроводное подключение", 1, null, null, 2, "Беспроводное" },
                    { 3, "Гибридное подключение", 1, null, null, 3, "Гибридное" },
                    { 4, "Wi-Fi", 2, null, null, 1, "Wi-Fi" },
                    { 5, "Zigbee", 2, null, null, 2, "Zigbee" },
                    { 6, "Bluetooth", 2, null, null, 3, "Bluetooth" },
                    { 10, "Лампочка", 4, "lightbulb", null, 1, null },
                    { 11, "Термостат", 4, "thermostat", null, 2, null },
                    { 12, "Камера", 4, "camera", null, 3, null },
                    { 7, "5 Вт", 3, null, 5m, 1, null },
                    { 8, "9 Вт", 3, null, 9m, 2, null },
                    { 9, "12 Вт", 3, null, 12m, 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_enum_class_MeasureUnitId",
                table: "enum_class",
                column: "MeasureUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_enum_class_Name",
                table: "enum_class",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enum_class_ShortName",
                table: "enum_class",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enum_class_SortOrder",
                table: "enum_class",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_enum_value_EnumClassId",
                table: "enum_value",
                column: "EnumClassId");

            migrationBuilder.CreateIndex(
                name: "IX_enum_value_EnumClassId_SortOrder",
                table: "enum_value",
                columns: new[] { "EnumClassId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enum_value");

            migrationBuilder.DropTable(
                name: "enum_class");

            migrationBuilder.DeleteData(
                table: "measure_unit",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
