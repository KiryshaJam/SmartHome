using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartHome.Migrations
{
    /// <inheritdoc />
    public partial class AddProductParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parameter_definition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ValueType = table.Column<int>(type: "integer", nullable: false),
                    MeasureUnitId = table.Column<int>(type: "integer", nullable: true),
                    EnumClassId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parameter_definition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_parameter_definition_enum_class_EnumClassId",
                        column: x => x.EnumClassId,
                        principalTable: "enum_class",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parameter_definition_measure_unit_MeasureUnitId",
                        column: x => x.MeasureUnitId,
                        principalTable: "measure_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "parameter_group",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parameter_group", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "class_parameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClassNodeId = table.Column<int>(type: "integer", nullable: false),
                    ParameterDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    ParameterGroupId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsInherited = table.Column<bool>(type: "boolean", nullable: false),
                    MinNumberValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MaxNumberValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_parameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_parameter_class_node_ClassNodeId",
                        column: x => x.ClassNodeId,
                        principalTable: "class_node",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_class_parameter_parameter_definition_ParameterDefinitionId",
                        column: x => x.ParameterDefinitionId,
                        principalTable: "parameter_definition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_parameter_parameter_group_ParameterGroupId",
                        column: x => x.ParameterGroupId,
                        principalTable: "parameter_group",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "product_parameter_value",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ClassParameterId = table.Column<int>(type: "integer", nullable: false),
                    IntegerValue = table.Column<int>(type: "integer", nullable: true),
                    NumberValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    StringValue = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnumValueId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_parameter_value", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_parameter_value_class_parameter_ClassParameterId",
                        column: x => x.ClassParameterId,
                        principalTable: "class_parameter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_parameter_value_enum_value_EnumValueId",
                        column: x => x.EnumValueId,
                        principalTable: "enum_value",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_parameter_value_product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "parameter_definition",
                columns: new[] { "Id", "EnumClassId", "MeasureUnitId", "Name", "ShortName", "ValueType" },
                values: new object[,]
                {
                    { 1, 1, null, "Тип подключения", "connection-kind-param", 5 },
                    { 2, 2, null, "Протокол связи", "communication-protocol-param", 5 },
                    { 3, null, 5, "Мощность", "power-watt", 2 },
                    { 4, 3, null, "Мощность лампы из списка", "lamp-power-enum-param", 5 },
                    { 5, 4, null, "Иконка устройства", "device-icon-param", 5 },
                    { 6, null, 4, "Количество каналов", "channel-count", 1 },
                    { 7, null, null, "Рабочая температура", "working-temperature", 2 },
                    { 8, null, null, "Описание устройства", "device-description", 3 },
                    { 9, null, null, "Дата ввода в эксплуатацию", "commissioning-date", 4 }
                });

            migrationBuilder.InsertData(
                table: "parameter_group",
                columns: new[] { "Id", "Name", "ShortName", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Подключение", "connection", 1 },
                    { 2, "Электропитание", "power", 2 },
                    { 3, "Технические характеристики", "technical", 3 },
                    { 4, "Климатические параметры", "climate", 4 },
                    { 5, "Внешний вид", "appearance", 5 }
                });

            migrationBuilder.InsertData(
                table: "class_parameter",
                columns: new[] { "Id", "ClassNodeId", "IsInherited", "IsRequired", "MaxNumberValue", "MinNumberValue", "ParameterDefinitionId", "ParameterGroupId", "SortOrder" },
                values: new object[,]
                {
                    { 1, 7, false, true, null, null, 1, 1, 1 },
                    { 2, 7, false, true, null, null, 2, 1, 2 },
                    { 3, 7, false, true, 20m, 3m, 3, 2, 3 },
                    { 4, 8, false, true, null, null, 2, 1, 1 },
                    { 5, 8, false, true, null, null, 4, 2, 2 },
                    { 6, 10, false, true, null, null, 1, 1, 1 },
                    { 7, 10, false, true, 6m, 1m, 6, 3, 2 },
                    { 8, 16, false, true, null, null, 2, 1, 1 },
                    { 9, 16, false, true, 35m, 5m, 7, 4, 2 },
                    { 10, 23, false, true, null, null, 2, 1, 1 },
                    { 11, 23, false, false, null, null, 8, 3, 2 },
                    { 12, 23, false, false, null, null, 9, 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "product_parameter_value",
                columns: new[] { "Id", "ClassParameterId", "DateTimeValue", "EnumValueId", "IntegerValue", "NumberValue", "ProductId", "StringValue" },
                values: new object[,]
                {
                    { 1, 1, null, 2, null, null, 1, null },
                    { 2, 2, null, 4, null, null, 1, null },
                    { 3, 3, null, null, null, 9m, 1, null },
                    { 4, 4, null, 5, null, null, 2, null },
                    { 5, 5, null, 8, null, null, 2, null },
                    { 6, 6, null, 2, null, null, 3, null },
                    { 7, 7, null, null, 2, null, 3, null },
                    { 8, 8, null, 4, null, null, 4, null },
                    { 9, 9, null, null, null, 25m, 4, null },
                    { 10, 10, null, 5, null, null, 5, null },
                    { 11, 11, null, null, null, null, 5, "Датчик температуры и влажности для системы умного дома" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_parameter_ClassNodeId_ParameterDefinitionId",
                table: "class_parameter",
                columns: new[] { "ClassNodeId", "ParameterDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_parameter_ClassNodeId_SortOrder",
                table: "class_parameter",
                columns: new[] { "ClassNodeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_class_parameter_ParameterDefinitionId",
                table: "class_parameter",
                column: "ParameterDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_class_parameter_ParameterGroupId",
                table: "class_parameter",
                column: "ParameterGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_parameter_definition_EnumClassId",
                table: "parameter_definition",
                column: "EnumClassId");

            migrationBuilder.CreateIndex(
                name: "IX_parameter_definition_MeasureUnitId",
                table: "parameter_definition",
                column: "MeasureUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_parameter_definition_Name",
                table: "parameter_definition",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parameter_definition_ShortName",
                table: "parameter_definition",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parameter_definition_ValueType",
                table: "parameter_definition",
                column: "ValueType");

            migrationBuilder.CreateIndex(
                name: "IX_parameter_group_Name",
                table: "parameter_group",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parameter_group_ShortName",
                table: "parameter_group",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parameter_group_SortOrder",
                table: "parameter_group",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_product_parameter_value_ClassParameterId",
                table: "product_parameter_value",
                column: "ClassParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_product_parameter_value_EnumValueId",
                table: "product_parameter_value",
                column: "EnumValueId");

            migrationBuilder.CreateIndex(
                name: "IX_product_parameter_value_ProductId_ClassParameterId",
                table: "product_parameter_value",
                columns: new[] { "ProductId", "ClassParameterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_parameter_value");

            migrationBuilder.DropTable(
                name: "class_parameter");

            migrationBuilder.DropTable(
                name: "parameter_definition");

            migrationBuilder.DropTable(
                name: "parameter_group");
        }
    }
}
