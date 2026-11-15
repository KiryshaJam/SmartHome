using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace SmartHome.Migrations;
public partial class AddManufacturingRoutes : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  using var reader = new StreamReader(typeof(AddManufacturingRoutes).Assembly.GetManifestResourceStream("SmartHome.Sql.routes.sql")!);
  migrationBuilder.Sql(reader.ReadToEnd());
 }
 protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Откат маршрутов удаляет производственные данные. Восстановите согласованную резервную копию.");
}
