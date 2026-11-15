using Microsoft.EntityFrameworkCore;
using SmartHome.Models;
using SmartHome.Models.Bom;
using SmartHome.Models.Routes;
namespace SmartHome.Data;

public partial class AppDbContext
{
    private static void ConfigureRoutes(ModelBuilder m)
    {
        m.Entity<Product>().Property(x => x.PurchasePrice).HasColumnType("numeric");
        m.Entity<Department>(e =>
        {
            e.ToTable("department"); e.HasKey(x => x.Id);
            e.HasOne<ClassNode>().WithMany().HasForeignKey(x => x.TypeId).HasConstraintName("department_TypeId_fkey").OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<WorkCenter>(e =>
        {
            e.ToTable("work_center"); e.HasKey(x => x.Id);
            e.HasOne<ClassNode>().WithMany().HasForeignKey(x => x.TypeId).HasConstraintName("work_center_TypeId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).HasConstraintName("work_center_DepartmentId_fkey").OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<TechStep>(e =>
        {
            e.ToTable("tech_step"); e.HasKey(x => x.Id);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("tech_step_ProductId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MeasureUnit>().WithMany().HasForeignKey(x => x.UnitId).HasConstraintName("tech_step_UnitId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ClassNode>().WithMany().HasForeignKey(x => x.OperationId).HasConstraintName("tech_step_OperationId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<WorkCenter>().WithMany().HasForeignKey(x => x.WorkCenterId).HasConstraintName("tech_step_WorkCenterId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ClassNode>().WithMany().HasForeignKey(x => x.ProfessionId).HasConstraintName("tech_step_ProfessionId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EnumValue>().WithMany().HasForeignKey(x => x.QualificationId).HasConstraintName("tech_step_QualificationId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SetupMinutes).HasColumnType("numeric");
            e.Property(x => x.UnitMinutes).HasColumnType("numeric");
            e.Property(x => x.HourlyRate).HasColumnType("numeric");
        });
        m.Entity<TechInput>(e =>
        {
            e.ToTable("tech_input"); e.HasKey(x => x.Id);
            e.HasOne<TechStep>().WithMany().HasForeignKey(x => x.StepId).HasConstraintName("tech_input_StepId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TechStep>().WithMany().HasForeignKey(x => x.SourceId).HasConstraintName("tech_input_SourceId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ParameterDefinition>().WithMany().HasForeignKey(x => x.ParameterId).HasConstraintName("tech_input_ParameterId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Amount).HasColumnType("numeric");
            e.Property(x => x.ForQuantity).HasColumnType("numeric");
            e.HasIndex(x => new { x.StepId, x.SourceId }).IsUnique().HasDatabaseName("tech_input_StepId_SourceId_key");
        });
        m.Entity<TechRouteSetting>(e =>
        {
            e.ToTable("tech_route_setting"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.FinalStepId).IsUnique().HasDatabaseName("UX_tech_route_owner").HasFilter("\"Kind\"<>'application'");
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("tech_route_setting_ProductId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.RevisionId).HasConstraintName("tech_route_setting_RevisionId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TechStep>().WithMany().HasForeignKey(x => x.FinalStepId).HasConstraintName("tech_route_setting_FinalStepId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ParameterDefinition>().WithMany().HasForeignKey(x => x.ParameterId).HasConstraintName("tech_route_setting_ParameterId_fkey").OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ParameterValue).HasColumnType("numeric");
        });

        m.Entity<Department>().HasKey(x => x.Id).HasName("department_pkey");
        m.Entity<WorkCenter>().HasKey(x => x.Id).HasName("work_center_pkey");
        m.Entity<TechInput>().HasKey(x => x.Id).HasName("tech_input_pkey");
        m.Entity<TechStep>().HasKey(x => x.Id).HasName("tech_step_pkey");
        m.Entity<TechRouteSetting>().HasKey(x => x.Id).HasName("tech_route_setting_pkey");

        m.Entity<TechInput>().ToTable("tech_input", t => t.HasCheckConstraint("tech_input_check", "(\"StepId\" <> \"SourceId\")"));
        m.Entity<Product>().ToTable("product", t => t.HasCheckConstraint("CK_product_price", "((\"PurchasePrice\" >= (0)::numeric) AND (\"PurchasePrice\" < (1000000000)::numeric))"));
        m.Entity<TechStep>().ToTable("tech_step", t => t.HasCheckConstraint("tech_step_Kind_check", "(\"Kind\" = ANY (ARRAY['purchase'::text, 'production'::text]))"));
        m.Entity<TechStep>().ToTable("tech_step", t => t.HasCheckConstraint("tech_step_ResultName_check", "(length(TRIM(BOTH FROM \"ResultName\")) > 0)"));
        m.Entity<TechStep>().ToTable("tech_step", t => t.HasCheckConstraint("tech_step_SetupMinutes_check", "((\"SetupMinutes\" >= (0)::numeric) AND (\"SetupMinutes\" < (1000000000)::numeric))"));
        m.Entity<TechStep>().ToTable("tech_step", t => t.HasCheckConstraint("tech_step_UnitMinutes_check", "((\"UnitMinutes\" >= (0)::numeric) AND (\"UnitMinutes\" < (1000000000)::numeric))"));
        m.Entity<TechStep>().ToTable("tech_step", t => t.HasCheckConstraint("tech_step_HourlyRate_check", "((\"HourlyRate\" >= (0)::numeric) AND (\"HourlyRate\" < (1000000000)::numeric))"));
        m.Entity<TechStep>().ToTable("tech_step", t => t.HasCheckConstraint("tech_step_check", "((\"Kind\" = 'purchase'::text) OR ((\"Number\" > 0) AND (\"OperationId\" IS NOT NULL) AND (\"WorkCenterId\" IS NOT NULL) AND (\"ProfessionId\" IS NOT NULL) AND (\"QualificationId\" IS NOT NULL)))"));
        m.Entity<TechInput>().ToTable("tech_input", t => t.HasCheckConstraint("tech_input_Amount_check", "((\"Amount\" > (0)::numeric) AND (\"Amount\" < (1000000000)::numeric))"));
        m.Entity<TechInput>().ToTable("tech_input", t => t.HasCheckConstraint("tech_input_ForQuantity_check", "((\"ForQuantity\" > (0)::numeric) AND (\"ForQuantity\" < (1000000000)::numeric))"));
        m.Entity<TechRouteSetting>().ToTable("tech_route_setting", t => t.HasCheckConstraint("tech_route_setting_Name_check", "(length(TRIM(BOTH FROM \"Name\")) > 0)"));
        m.Entity<TechRouteSetting>().ToTable("tech_route_setting", t => t.HasCheckConstraint("tech_route_setting_Kind_check", "(\"Kind\" = ANY (ARRAY['standalone'::text, 'template'::text, 'application'::text]))"));
        m.Entity<TechRouteSetting>().ToTable("tech_route_setting", t => t.HasCheckConstraint("tech_route_setting_check", "(((\"ParameterId\" IS NULL) AND (\"ParameterValue\" IS NULL)) OR ((\"ParameterId\" IS NOT NULL) AND (\"ParameterValue\" IS NOT NULL) AND (\"ParameterValue\" > (0)::numeric) AND (\"ParameterValue\" < (1000000000)::numeric)))"));
    }
}