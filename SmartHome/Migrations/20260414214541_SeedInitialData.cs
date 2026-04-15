using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartHome.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "class_node",
                columns: new[] { "Id", "IsTerminal", "MeasureUnitId", "Name", "ParentId", "ShortName", "SortOrder" },
                values: new object[] { 1, false, null, "Умный дом", null, "smart-home", 1 });

            migrationBuilder.InsertData(
                table: "measure_unit",
                columns: new[] { "Id", "Name", "ShortName" },
                values: new object[,]
                {
                    { 1, "Штука", "шт." },
                    { 2, "Комплект", "компл." },
                    { 3, "Зона", "зона" },
                    { 4, "Канал", "канал" }
                });

            migrationBuilder.InsertData(
                table: "class_node",
                columns: new[] { "Id", "IsTerminal", "MeasureUnitId", "Name", "ParentId", "ShortName", "SortOrder" },
                values: new object[,]
                {
                    { 2, false, null, "Освещение", 1, "lighting", 1 },
                    { 3, false, null, "Климат-контроль", 1, "climate-control", 2 },
                    { 4, false, null, "Источники света", 2, "light-sources", 1 },
                    { 5, false, null, "Управление освещением", 2, "lighting-control", 2 },
                    { 6, true, 1, "Датчики освещенности", 2, "light-sensors", 3 },
                    { 12, false, null, "Отопление", 3, "heating", 1 },
                    { 13, false, null, "Охлаждение", 3, "cooling", 2 },
                    { 14, false, null, "Вентиляция", 3, "ventilation", 3 },
                    { 15, false, null, "Датчики климата", 3, "climate-sensors", 4 },
                    { 7, true, 1, "LED лампы", 4, "led-lamps", 1 },
                    { 8, true, 1, "RGB лампы", 4, "rgb-lamps", 2 },
                    { 9, true, 1, "Умные ленты", 4, "smart-strips", 3 },
                    { 10, true, 1, "Выключатели", 5, "switches", 1 },
                    { 11, true, 1, "Панели управления", 5, "control-panels", 2 },
                    { 16, true, 1, "Термостаты", 12, "thermostats", 1 },
                    { 17, true, 1, "Радиаторные клапаны", 12, "radiator-valves", 2 },
                    { 18, true, 2, "Тёплый пол", 12, "floor-heating", 3 },
                    { 19, true, 1, "Кондиционеры", 13, "air-conditioners", 1 },
                    { 20, true, 1, "Вентиляторы", 13, "fans", 2 },
                    { 21, true, 1, "Рекуператоры", 14, "recuperators", 1 },
                    { 22, true, 1, "Вытяжные системы", 14, "exhaust-systems", 2 },
                    { 23, true, 1, "Датчики температуры", 15, "temperature-sensors", 1 },
                    { 24, true, 1, "Датчики влажности", 15, "humidity-sensors", 2 },
                    { 25, true, 1, "CO2 датчики", 15, "co2-sensors", 3 }
                });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "Id", "ClassNodeId", "Name", "ShortName" },
                values: new object[,]
                {
                    { 1, 7, "Xiaomi Smart LED Bulb", "xiaomi-led-bulb" },
                    { 2, 8, "Philips Hue White and Color", "philips-hue-color" },
                    { 3, 10, "Aqara Wall Switch H1", "aqara-switch-h1" },
                    { 4, 16, "Google Nest Thermostat", "nest-thermostat" },
                    { 5, 23, "Xiaomi Temperature and Humidity Sensor", "xiaomi-temp-humidity" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "measure_unit",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "measure_unit",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "product",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "measure_unit",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "measure_unit",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "class_node",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
