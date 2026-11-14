using Microsoft.EntityFrameworkCore;
using SmartHome.Models;
using SmartHome.Models.Bom;

namespace SmartHome.Data;

public partial class AppDbContext
{
    private static void ConfigureBom(ModelBuilder m)
    {
        m.Entity<BomRevision>(e =>
        {
            e.ToTable("bom_revision", t => t.HasCheckConstraint("CK_bom_revision_kind", "(NOT \"IsConfigured\" AND \"SourceRevisionId\" IS NULL AND \"Name\" IS NULL) OR (\"IsConfigured\" AND \"SourceRevisionId\" IS NOT NULL AND \"PreviousId\" IS NULL AND \"BaseRevisionId\" IS NULL)"));
            e.HasKey(x => x.Id);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MeasureUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.PreviousId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.BaseRevisionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.SourceRevisionId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ProductId, x.PreviousId }).IsUnique().AreNullsDistinct(false).HasFilter("NOT \"IsConfigured\"");
        });
        m.Entity<BomPosition>(e =>
        {
            e.ToTable("bom_position", t => t.HasCheckConstraint("CK_bom_position", "\"PositionKey\" ~ '^[A-Za-z0-9_-]{1,64}$' AND \"SortOrder\">=0 AND (\"Excluded\" OR (\"ComponentRevisionId\" IS NOT NULL AND \"Amount\">0 AND \"Amount\"<=1000000000 AND \"ForQuantity\">0 AND \"ForQuantity\"<=1000000000 AND \"Amount\"/\"ForQuantity\"<=1000000000))"));
            e.HasKey(x => new { x.RevisionId, x.PositionKey });
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.ComponentRevisionId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<BomParameter>(e =>
        {
            e.ToTable("bom_parameter", t =>
            {
                t.HasCheckConstraint("CK_bom_parameter_bounds", "\"SortOrder\">=0 AND \"Type\" BETWEEN 1 AND 5 AND (\"Min\" IS NULL OR \"Max\" IS NULL OR \"Min\"<=\"Max\")");
                t.HasCheckConstraint("CK_bom_parameter_value", "num_nonnulls(\"IntegerValue\",\"NumberValue\",\"StringValue\",\"DateTimeTicks\",\"EnumValueId\")<=1 AND (\"IntegerValue\" IS NULL OR \"Type\"=1) AND (\"NumberValue\" IS NULL OR \"Type\"=2) AND (\"StringValue\" IS NULL OR \"Type\"=3) AND (\"DateTimeTicks\" IS NULL OR \"Type\"=4) AND (\"EnumValueId\" IS NULL OR \"Type\"=5)");
            });
            e.HasKey(x => new { x.RevisionId, x.DefinitionId });
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ParameterDefinition>().WithMany().HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EnumValue>().WithMany().HasForeignKey(x => x.EnumValueId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<BomParameterOption>(e =>
        {
            e.ToTable("bom_parameter_option"); e.HasKey(x => new { x.RevisionId, x.DefinitionId, x.EnumValueId });
            e.HasOne<BomParameter>().WithMany().HasForeignKey(x => new { x.RevisionId, x.DefinitionId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EnumValue>().WithMany().HasForeignKey(x => x.EnumValueId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<BomPositionBinding>(e =>
        {
            e.ToTable("bom_position_binding"); e.HasKey(x => new { x.RevisionId, x.PositionKey, x.DefinitionId });
            e.HasOne<BomPosition>().WithMany().HasForeignKey(x => new { x.RevisionId, x.PositionKey }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ParameterDefinition>().WithMany().HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<BomRule>(e =>
        {
            e.ToTable("bom_rule", t => t.HasCheckConstraint("CK_bom_rule", "\"SortOrder\">=0 AND length(trim(\"Expression\"))>0 AND length(trim(\"Message\"))>0"));
            e.HasKey(x => new { x.RevisionId, x.SortOrder });
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<BomOrder>(e =>
        {
            e.ToTable("bom_order", t => t.HasCheckConstraint("CK_bom_order_token", "\"UpdateToken\">0")); e.HasKey(x => x.Id); e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.UpdateToken).IsConcurrencyToken();
        });
        m.Entity<BomOrderLine>(e =>
        {
            e.ToTable("bom_order_line", t => t.HasCheckConstraint("CK_bom_order_line", "\"SortOrder\">=0 AND \"Quantity\">0 AND \"Quantity\"<=1000000000"));
            e.HasKey(x => new { x.OrderId, x.SortOrder });
            e.HasOne<BomOrder>().WithMany(x => x.Lines).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.ConfiguredRevisionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
