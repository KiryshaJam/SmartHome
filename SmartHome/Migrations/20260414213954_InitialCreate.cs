using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartHome.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "measure_unit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measure_unit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "class_node",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    MeasureUnitId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_node", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_node_class_node_ParentId",
                        column: x => x.ParentId,
                        principalTable: "class_node",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_node_measure_unit_MeasureUnitId",
                        column: x => x.MeasureUnitId,
                        principalTable: "measure_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ClassNodeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_class_node_ClassNodeId",
                        column: x => x.ClassNodeId,
                        principalTable: "class_node",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_node_MeasureUnitId",
                table: "class_node",
                column: "MeasureUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_class_node_Name",
                table: "class_node",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_node_ParentId",
                table: "class_node",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_class_node_ShortName",
                table: "class_node",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_measure_unit_Name",
                table: "measure_unit",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_measure_unit_ShortName",
                table: "measure_unit",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_ClassNodeId",
                table: "product",
                column: "ClassNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_product_Name",
                table: "product",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_ShortName",
                table: "product",
                column: "ShortName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "class_node");

            migrationBuilder.DropTable(
                name: "measure_unit");
        }
    }
}
