using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHome.Data;

#nullable disable
namespace SmartHome.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261113130645_FixBomTreeResultTypes")]
public sealed class FixBomTreeResultTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE OR REPLACE FUNCTION find_bom(variant_id int, quantity numeric DEFAULT 1)
RETURNS TABLE(path text,depth int,product_id int,name text,unit_id int,unit text,required_quantity numeric,classes jsonb,leaf bool,revision_id int,norm numeric,for_quantity numeric,position_id text) LANGUAGE plpgsql STABLE AS $$
BEGIN
 IF quantity IS NULL OR quantity<=0 OR quantity>1000000000 OR NOT EXISTS(SELECT 1 FROM bom_revision WHERE "Id"=variant_id AND "IsConfigured" AND "IsFinalized") THEN RAISE EXCEPTION 'Неверное исполнение или количество.'; END IF;
 RETURN QUERY WITH RECURSIVE tree AS (
  SELECT r."Id" rid,''::text route,0 lvl,quantity q,r."RootAmount" amount,r."RootForQuantity" denominator,''::text position FROM bom_revision r WHERE r."Id"=variant_id
  UNION ALL SELECT pos."ComponentRevisionId",tree.route||'/'||pos."PositionKey",tree.lvl+1,tree.q*pos."ResolvedQuantity",pos."Amount",pos."ForQuantity",pos."PositionKey" FROM tree JOIN bom_position pos ON pos."RevisionId"=tree.rid WHERE tree.lvl<32
 ) SELECT tree.route,tree.lvl,r."ProductId",p."Name"::text,r."UnitId",u."ShortName"::text,tree.q,bom_classes(r."ProductId"),NOT EXISTS(SELECT 1 FROM bom_position pos WHERE pos."RevisionId"=tree.rid),r."SourceRevisionId",tree.amount,tree.denominator,tree.position::text
 FROM tree JOIN bom_revision r ON r."Id"=tree.rid JOIN product p ON p."Id"=r."ProductId" JOIN measure_unit u ON u."Id"=r."UnitId" ORDER BY tree.route;
END $$;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE OR REPLACE FUNCTION find_bom(variant_id int, quantity numeric DEFAULT 1)
RETURNS TABLE(path text,depth int,product_id int,name text,unit_id int,unit text,required_quantity numeric,classes jsonb,leaf bool,revision_id int,norm numeric,for_quantity numeric,position_id text) LANGUAGE plpgsql STABLE AS $$
BEGIN
 IF quantity IS NULL OR quantity<=0 OR quantity>1000000000 OR NOT EXISTS(SELECT 1 FROM bom_revision WHERE "Id"=variant_id AND "IsConfigured" AND "IsFinalized") THEN RAISE EXCEPTION 'Неверное исполнение или количество.'; END IF;
 RETURN QUERY WITH RECURSIVE tree AS (
  SELECT r."Id" rid,''::text route,0 lvl,quantity q,r."RootAmount" amount,r."RootForQuantity" denominator,''::text position FROM bom_revision r WHERE r."Id"=variant_id
  UNION ALL SELECT pos."ComponentRevisionId",tree.route||'/'||pos."PositionKey",tree.lvl+1,tree.q*pos."ResolvedQuantity",pos."Amount",pos."ForQuantity",pos."PositionKey" FROM tree JOIN bom_position pos ON pos."RevisionId"=tree.rid WHERE tree.lvl<32
 ) SELECT tree.route,tree.lvl,r."ProductId",p."Name",r."UnitId",u."ShortName",tree.q,bom_classes(r."ProductId"),NOT EXISTS(SELECT 1 FROM bom_position pos WHERE pos."RevisionId"=tree.rid),r."SourceRevisionId",tree.amount,tree.denominator,tree.position
 FROM tree JOIN bom_revision r ON r."Id"=tree.rid JOIN product p ON p."Id"=r."ProductId" JOIN measure_unit u ON u."Id"=r."UnitId" ORDER BY tree.route;
END $$;
""");
    }
}
