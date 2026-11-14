using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartHome.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeBomSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "CreationTransaction",
                table: "bom_variant",
                type: "xid8",
                nullable: false,
                defaultValueSql: "pg_current_xact_id()");

            migrationBuilder.AddColumn<ulong>(
                name: "CreationTransaction",
                table: "bom_revision",
                type: "xid8",
                nullable: false,
                defaultValueSql: "pg_current_xact_id()");

            migrationBuilder.AddColumn<int>(
                name: "DocumentId",
                table: "bom_revision",
                type: "integer",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_bom_variant_identity",
                table: "bom_variant",
                columns: new[] { "Id", "RevisionId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_bom_revision_document",
                table: "bom_revision",
                columns: new[] { "Id", "DocumentId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_bom_revision_identity",
                table: "bom_revision",
                columns: new[] { "Id", "ProductId", "UnitId" });

            migrationBuilder.CreateTable(
                name: "bom_document",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropertyMask = table.Column<long>(type: "bigint", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: true),
                    UnitName = table.Column<string>(type: "text", nullable: true),
                    CreationTransaction = table.Column<ulong>(type: "xid8", nullable: false, defaultValueSql: "pg_current_xact_id()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_document", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bom_document_class",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    ClassId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_document_class", x => new { x.DocumentId, x.Ordinal });
                    table.CheckConstraint("CK_bom_doc_class_ordinal", "\"Ordinal\">=0");
                    table.ForeignKey(
                        name: "FK_bom_document_class_bom_document_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "bom_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_document_class_class_node_ClassId",
                        column: x => x.ClassId,
                        principalTable: "class_node",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_position",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    PositionId = table.Column<string>(type: "text", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    PropertyMask = table.Column<long>(type: "bigint", nullable: false),
                    ComponentId = table.Column<int>(type: "integer", nullable: true),
                    RevisionId = table.Column<int>(type: "integer", nullable: true),
                    UnitId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    ForQuantity = table.Column<decimal>(type: "numeric", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    Condition = table.Column<string>(type: "text", nullable: true),
                    Formula = table.Column<string>(type: "text", nullable: true),
                    ChildVariantId = table.Column<int>(type: "integer", nullable: true),
                    Excluded = table.Column<bool>(type: "boolean", nullable: false),
                    Inherited = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_position", x => new { x.DocumentId, x.PositionId });
                    table.CheckConstraint("CK_bom_position_content", "\"PositionId\" ~ '^[A-Za-z0-9_-]{1,64}$' AND \"Ordinal\">=0 AND (num_nonnulls(\"RevisionId\",\"ComponentId\",\"UnitId\") IN (0,3)) AND (\"Excluded\" OR (\"RevisionId\" IS NOT NULL AND \"Amount\" IS NOT NULL AND \"ForQuantity\" IS NOT NULL AND \"Amount\">0 AND \"Amount\"<=1000000000 AND \"ForQuantity\">0 AND \"ForQuantity\"<=1000000000 AND \"Amount\"/\"ForQuantity\"<=1000000000))");
                    table.ForeignKey(
                        name: "FK_bom_position_bom_document_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "bom_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_position_child",
                        columns: x => new { x.RevisionId, x.ComponentId, x.UnitId },
                        principalTable: "bom_revision",
                        principalColumns: new[] { "Id", "ProductId", "UnitId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_position_variant",
                        columns: x => new { x.ChildVariantId, x.RevisionId },
                        principalTable: "bom_variant",
                        principalColumns: new[] { "Id", "RevisionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_rule",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Expression = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_rule", x => new { x.DocumentId, x.Ordinal });
                    table.CheckConstraint("CK_bom_rule_ordinal", "\"Ordinal\">=0");
                    table.ForeignKey(
                        name: "FK_bom_rule_bom_document_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "bom_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_variant_node",
                columns: table => new
                {
                    VariantId = table.Column<int>(type: "integer", nullable: false),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    ParentNodeId = table.Column<int>(type: "integer", nullable: true),
                    ParentSourceDocumentId = table.Column<int>(type: "integer", nullable: true),
                    SiblingOrdinal = table.Column<int>(type: "integer", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "integer", nullable: false),
                    PropertyMask = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    RevisionId = table.Column<int>(type: "integer", nullable: false),
                    UnitId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    PositionId = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    ForQuantity = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_variant_node", x => new { x.VariantId, x.NodeId });
                    table.UniqueConstraint("AK_bom_node_source", x => new { x.VariantId, x.NodeId, x.SourceDocumentId });
                    table.CheckConstraint("CK_bom_node_content", "\"NodeId\">0 AND \"SiblingOrdinal\">=0 AND \"Quantity\">0 AND \"Quantity\"<=1000000000 AND (\"Amount\" IS NULL OR \"Amount\">0 AND \"Amount\"<=1000000000) AND (\"ForQuantity\" IS NULL OR \"ForQuantity\">0 AND \"ForQuantity\"<=1000000000) AND ((\"ParentNodeId\" IS NULL AND \"ParentSourceDocumentId\" IS NULL AND \"SiblingOrdinal\"=0) OR (\"ParentNodeId\" IS NOT NULL AND \"ParentNodeId\"<\"NodeId\" AND \"ParentSourceDocumentId\" IS NOT NULL AND \"PositionId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_bom_node_parent",
                        columns: x => new { x.VariantId, x.ParentNodeId, x.ParentSourceDocumentId },
                        principalTable: "bom_variant_node",
                        principalColumns: new[] { "VariantId", "NodeId", "SourceDocumentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_node_position",
                        columns: x => new { x.ParentSourceDocumentId, x.PositionId },
                        principalTable: "bom_position",
                        principalColumns: new[] { "DocumentId", "PositionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_node_revision",
                        columns: x => new { x.RevisionId, x.ProductId, x.UnitId },
                        principalTable: "bom_revision",
                        principalColumns: new[] { "Id", "ProductId", "UnitId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_variant_node_bom_document_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "bom_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_variant_node_bom_variant_VariantId",
                        column: x => x.VariantId,
                        principalTable: "bom_variant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_variant_node_class",
                columns: table => new
                {
                    VariantId = table.Column<int>(type: "integer", nullable: false),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    ClassId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_variant_node_class", x => new { x.VariantId, x.NodeId, x.Ordinal });
                    table.CheckConstraint("CK_bom_node_class_ordinal", "\"Ordinal\">=0");
                    table.ForeignKey(
                        name: "FK_bom_node_class_node",
                        columns: x => new { x.VariantId, x.NodeId },
                        principalTable: "bom_variant_node",
                        principalColumns: new[] { "VariantId", "NodeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_variant_node_class_class_node_ClassId",
                        column: x => x.ClassId,
                        principalTable: "class_node",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_calculation",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    ParameterKey = table.Column<string>(type: "text", nullable: false),
                    Expression = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_calculation", x => new { x.DocumentId, x.ParameterKey });
                });

            migrationBuilder.CreateTable(
                name: "bom_parameter",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    PropertyMask = table.Column<long>(type: "bigint", nullable: false),
                    DefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Min = table.Column<decimal>(type: "numeric", nullable: true),
                    Max = table.Column<decimal>(type: "numeric", nullable: true),
                    HasDefault = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultInteger = table.Column<int>(type: "integer", nullable: true),
                    DefaultNumber = table.Column<decimal>(type: "numeric", nullable: true),
                    DefaultString = table.Column<string>(type: "text", nullable: true),
                    DefaultDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DefaultDateText = table.Column<string>(type: "text", nullable: true),
                    DefaultEnumId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_parameter", x => new { x.DocumentId, x.Key });
                    table.UniqueConstraint("AK_bom_parameter_type", x => new { x.DocumentId, x.Key, x.Type });
                    table.CheckConstraint("CK_bom_parameter_default", "(NOT \"HasDefault\" AND num_nonnulls(\"DefaultInteger\",\"DefaultNumber\",\"DefaultString\",\"DefaultDate\",\"DefaultEnumId\")=0 AND \"DefaultDateText\" IS NULL) OR (\"HasDefault\" AND num_nonnulls(\"DefaultInteger\",\"DefaultNumber\",\"DefaultString\",\"DefaultDate\",\"DefaultEnumId\")=1 AND CASE \"Type\" WHEN 1 THEN \"DefaultInteger\" IS NOT NULL WHEN 2 THEN \"DefaultNumber\" IS NOT NULL WHEN 3 THEN \"DefaultString\" IS NOT NULL WHEN 4 THEN \"DefaultDate\" IS NOT NULL AND \"DefaultDateText\" IS NOT NULL AND \"DefaultDateText\"::timestamptz=\"DefaultDate\" WHEN 5 THEN \"DefaultEnumId\" IS NOT NULL ELSE false END AND (\"Type\"=4 OR \"DefaultDateText\" IS NULL))");
                    table.CheckConstraint("CK_bom_parameter_type", "\"Type\" BETWEEN 1 AND 5 AND \"Ordinal\">=0");
                    table.ForeignKey(
                        name: "FK_bom_parameter_bom_document_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "bom_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_parameter_parameter_definition_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "parameter_definition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_parameter_enum",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    ParameterKey = table.Column<string>(type: "text", nullable: false),
                    EnumValueId = table.Column<int>(type: "integer", nullable: false),
                    AllowedOrdinal = table.Column<int>(type: "integer", nullable: true),
                    HasLabel = table.Column<bool>(type: "boolean", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_parameter_enum", x => new { x.DocumentId, x.ParameterKey, x.EnumValueId });
                    table.CheckConstraint("CK_bom_parameter_enum", "(\"AllowedOrdinal\" IS NULL OR \"AllowedOrdinal\">=0) AND (\"HasLabel\" OR \"Label\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_bom_enum_parameter",
                        columns: x => new { x.DocumentId, x.ParameterKey },
                        principalTable: "bom_parameter",
                        principalColumns: new[] { "DocumentId", "Key" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_parameter_enum_enum_value_EnumValueId",
                        column: x => x.EnumValueId,
                        principalTable: "enum_value",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_position_binding",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    PositionId = table.Column<string>(type: "text", nullable: false),
                    ParameterKey = table.Column<string>(type: "text", nullable: false),
                    ChildRevisionId = table.Column<int>(type: "integer", nullable: false),
                    TargetDocumentId = table.Column<int>(type: "integer", nullable: false),
                    Expression = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_position_binding", x => new { x.DocumentId, x.PositionId, x.ParameterKey });
                    table.ForeignKey(
                        name: "FK_bom_binding_parameter",
                        columns: x => new { x.TargetDocumentId, x.ParameterKey },
                        principalTable: "bom_parameter",
                        principalColumns: new[] { "DocumentId", "Key" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_binding_position",
                        columns: x => new { x.DocumentId, x.PositionId },
                        principalTable: "bom_position",
                        principalColumns: new[] { "DocumentId", "PositionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_binding_revision",
                        columns: x => new { x.ChildRevisionId, x.TargetDocumentId },
                        principalTable: "bom_revision",
                        principalColumns: new[] { "Id", "DocumentId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_variant_node_value",
                columns: table => new
                {
                    VariantId = table.Column<int>(type: "integer", nullable: false),
                    NodeId = table.Column<int>(type: "integer", nullable: false),
                    ParameterKey = table.Column<string>(type: "text", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IntegerValue = table.Column<int>(type: "integer", nullable: true),
                    NumberValue = table.Column<decimal>(type: "numeric", nullable: true),
                    StringValue = table.Column<string>(type: "text", nullable: true),
                    DateValue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateText = table.Column<string>(type: "text", nullable: true),
                    EnumValueId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_variant_node_value", x => new { x.VariantId, x.NodeId, x.ParameterKey });
                    table.CheckConstraint("CK_bom_node_value", "num_nonnulls(\"IntegerValue\",\"NumberValue\",\"StringValue\",\"DateValue\",\"EnumValueId\")=1 AND CASE \"Type\" WHEN 1 THEN \"IntegerValue\" IS NOT NULL WHEN 2 THEN \"NumberValue\" IS NOT NULL WHEN 3 THEN \"StringValue\" IS NOT NULL WHEN 4 THEN \"DateValue\" IS NOT NULL AND \"DateText\" IS NOT NULL AND \"DateText\"::timestamptz=\"DateValue\" WHEN 5 THEN \"EnumValueId\" IS NOT NULL ELSE false END AND (\"Type\"=4 OR \"DateText\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_bom_value_enum",
                        columns: x => new { x.SourceDocumentId, x.ParameterKey, x.EnumValueId },
                        principalTable: "bom_parameter_enum",
                        principalColumns: new[] { "DocumentId", "ParameterKey", "EnumValueId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_value_node",
                        columns: x => new { x.VariantId, x.NodeId, x.SourceDocumentId },
                        principalTable: "bom_variant_node",
                        principalColumns: new[] { "VariantId", "NodeId", "SourceDocumentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_value_parameter",
                        columns: x => new { x.SourceDocumentId, x.ParameterKey, x.Type },
                        principalTable: "bom_parameter",
                        principalColumns: new[] { "DocumentId", "Key", "Type" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bom_revision_DocumentId",
                table: "bom_revision",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bom_document_class_ClassId",
                table: "bom_document_class",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_parameter_DefinitionId",
                table: "bom_parameter",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_parameter_DocumentId_Key_DefaultEnumId",
                table: "bom_parameter",
                columns: new[] { "DocumentId", "Key", "DefaultEnumId" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_parameter_DocumentId_Ordinal",
                table: "bom_parameter",
                columns: new[] { "DocumentId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bom_parameter_enum_DocumentId_ParameterKey_AllowedOrdinal",
                table: "bom_parameter_enum",
                columns: new[] { "DocumentId", "ParameterKey", "AllowedOrdinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bom_parameter_enum_EnumValueId",
                table: "bom_parameter_enum",
                column: "EnumValueId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_position_ChildVariantId_RevisionId",
                table: "bom_position",
                columns: new[] { "ChildVariantId", "RevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_position_DocumentId_Ordinal",
                table: "bom_position",
                columns: new[] { "DocumentId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bom_position_RevisionId_ComponentId_UnitId",
                table: "bom_position",
                columns: new[] { "RevisionId", "ComponentId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_position_binding_ChildRevisionId_TargetDocumentId",
                table: "bom_position_binding",
                columns: new[] { "ChildRevisionId", "TargetDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_position_binding_TargetDocumentId_ParameterKey",
                table: "bom_position_binding",
                columns: new[] { "TargetDocumentId", "ParameterKey" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_node_root",
                table: "bom_variant_node",
                column: "VariantId",
                unique: true,
                filter: "\"ParentNodeId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_ParentSourceDocumentId_PositionId",
                table: "bom_variant_node",
                columns: new[] { "ParentSourceDocumentId", "PositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_RevisionId_ProductId_UnitId",
                table: "bom_variant_node",
                columns: new[] { "RevisionId", "ProductId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_SourceDocumentId",
                table: "bom_variant_node",
                column: "SourceDocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_VariantId_ParentNodeId_ParentSourceDocumen~",
                table: "bom_variant_node",
                columns: new[] { "VariantId", "ParentNodeId", "ParentSourceDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_VariantId_ParentNodeId_SiblingOrdinal",
                table: "bom_variant_node",
                columns: new[] { "VariantId", "ParentNodeId", "SiblingOrdinal" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_class_ClassId",
                table: "bom_variant_node_class",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_value_SourceDocumentId_ParameterKey_EnumVa~",
                table: "bom_variant_node_value",
                columns: new[] { "SourceDocumentId", "ParameterKey", "EnumValueId" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_value_SourceDocumentId_ParameterKey_Type",
                table: "bom_variant_node_value",
                columns: new[] { "SourceDocumentId", "ParameterKey", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_node_value_VariantId_NodeId_SourceDocumentId",
                table: "bom_variant_node_value",
                columns: new[] { "VariantId", "NodeId", "SourceDocumentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_bom_revision_bom_document_DocumentId",
                table: "bom_revision",
                column: "DocumentId",
                principalTable: "bom_document",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_bom_calc_parameter",
                table: "bom_calculation",
                columns: new[] { "DocumentId", "ParameterKey" },
                principalTable: "bom_parameter",
                principalColumns: new[] { "DocumentId", "Key" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_bom_parameter_default_enum",
                table: "bom_parameter",
                columns: new[] { "DocumentId", "Key", "DefaultEnumId" },
                principalTable: "bom_parameter_enum",
                principalColumns: new[] { "DocumentId", "ParameterKey", "EnumValueId" },
                onDelete: ReferentialAction.Restrict);
            // Schema first, lossless backfill second; JSON is dropped only after
            // every aggregate and historical SQL result has been checked.
            migrationBuilder.Sql("""
-- JSON exists only at the API/SQL boundary. Masks preserve known legacy field
-- absence (notably SQL norm fallback); all data and links live in typed rows.
CREATE FUNCTION bom_json_mask(j jsonb, keys text[]) RETURNS bigint LANGUAGE sql IMMUTABLE AS $$
 SELECT coalesce(sum(1::bigint << (k.ordinal-1)::int),0)::bigint FROM unnest(keys) WITH ORDINALITY AS k(property_key, ordinal) WHERE j ? k.property_key
$$;
CREATE FUNCTION bom_json_pick(j jsonb, mask bigint, keys text[]) RETURNS jsonb LANGUAGE sql IMMUTABLE AS $$
 SELECT coalesce(jsonb_object_agg(k.property_key,j->k.property_key),'{}') FROM unnest(keys) WITH ORDINALITY AS k(property_key, ordinal) WHERE (mask & (1::bigint << (k.ordinal-1)::int))<>0
$$;
CREATE FUNCTION bom_read_document(did int) RETURNS jsonb LANGUAGE sql STABLE AS $$
 SELECT bom_json_pick(jsonb_build_object('productName',d."ProductName",'unitName',d."UnitName",
 'classes',coalesce((SELECT jsonb_agg(c."ClassId" ORDER BY c."Ordinal") FROM bom_document_class c WHERE c."DocumentId"=did),'[]'),
 'positions',coalesce((SELECT jsonb_agg(bom_json_pick(jsonb_build_object(
   'id',p."PositionId",'componentId',coalesce(p."ComponentId",0),'revisionId',coalesce(p."RevisionId",0),'unitId',coalesce(p."UnitId",0),
   'amount',p."Amount",'forQuantity',p."ForQuantity",'sortOrder',p."SortOrder",'condition',p."Condition",'formula',p."Formula",'childVariantId',p."ChildVariantId",'excluded',p."Excluded",'inherited',p."Inherited",
   'bindings',coalesce((SELECT jsonb_object_agg(b."ParameterKey",b."Expression") FROM bom_position_binding b WHERE b."DocumentId"=did AND b."PositionId"=p."PositionId"),'{}')),
   p."PropertyMask",ARRAY['id','componentId','revisionId','amount','forQuantity','unitId','sortOrder','condition','formula','childVariantId','bindings','excluded','inherited']) ORDER BY p."Ordinal") FROM bom_position p WHERE p."DocumentId"=did),'[]'),
 'parameters',coalesce((SELECT jsonb_agg(bom_json_pick(jsonb_build_object(
   'key',p."Key",'definitionId',p."DefinitionId",'name',p."Name",'type',p."Type",'required',p."Required",'min',p."Min",'max',p."Max",
   'default',CASE WHEN NOT p."HasDefault" THEN 'null'::jsonb WHEN p."Type"=1 THEN to_jsonb(p."DefaultInteger") WHEN p."Type"=2 THEN to_jsonb(p."DefaultNumber") WHEN p."Type"=3 THEN to_jsonb(p."DefaultString") WHEN p."Type"=4 THEN to_jsonb(p."DefaultDateText") WHEN p."Type"=5 THEN to_jsonb(p."DefaultEnumId") END,
   'enumIds',coalesce((SELECT jsonb_agg(e."EnumValueId" ORDER BY e."AllowedOrdinal") FROM bom_parameter_enum e WHERE e."DocumentId"=did AND e."ParameterKey"=p."Key" AND e."AllowedOrdinal" IS NOT NULL),'[]'),
   'enumLabels',coalesce((SELECT jsonb_object_agg(e."EnumValueId"::text,e."Label") FROM bom_parameter_enum e WHERE e."DocumentId"=did AND e."ParameterKey"=p."Key" AND e."HasLabel"),'{}')),
   p."PropertyMask",ARRAY['key','definitionId','name','type','required','min','max','enumIds','default','enumLabels']) ORDER BY p."Ordinal") FROM bom_parameter p WHERE p."DocumentId"=did),'[]'),
 'rules',coalesce((SELECT jsonb_agg(jsonb_build_object('expression',r."Expression",'message',r."Message") ORDER BY r."Ordinal") FROM bom_rule r WHERE r."DocumentId"=did),'[]'),
 'calculations',coalesce((SELECT jsonb_object_agg(c."ParameterKey",c."Expression") FROM bom_calculation c WHERE c."DocumentId"=did),'{}')),
 d."PropertyMask",ARRAY['positions','parameters','rules','calculations','productName','unitName','classes']) FROM bom_document d WHERE d."Id"=did
$$;
CREATE FUNCTION bom_read_node(vid int,nid int) RETURNS jsonb LANGUAGE plpgsql STABLE AS $$
DECLARE n bom_variant_node; answer jsonb;
BEGIN
 SELECT * INTO STRICT n FROM bom_variant_node WHERE "VariantId"=vid AND "NodeId"=nid;
 SELECT bom_json_pick(jsonb_build_object('productId',n."ProductId",'revisionId',n."RevisionId",'name',n."Name",'unitId',n."UnitId",'unit',n."Unit",
 'positionId',n."PositionId",'quantity',n."Quantity",'amount',n."Amount",'forQuantity',n."ForQuantity",
 'classes',coalesce((SELECT jsonb_agg(c."ClassId" ORDER BY c."Ordinal") FROM bom_variant_node_class c WHERE c."VariantId"=vid AND c."NodeId"=nid),'[]'),
 'values',coalesce((SELECT jsonb_object_agg(v."ParameterKey",CASE v."Type" WHEN 1 THEN to_jsonb(v."IntegerValue") WHEN 2 THEN to_jsonb(v."NumberValue") WHEN 3 THEN to_jsonb(v."StringValue") WHEN 4 THEN to_jsonb(v."DateText") WHEN 5 THEN to_jsonb(v."EnumValueId") END) FROM bom_variant_node_value v WHERE v."VariantId"=vid AND v."NodeId"=nid),'{}'),
 'source',bom_read_document(n."SourceDocumentId"),
 'children',coalesce((SELECT jsonb_agg(bom_read_node(vid,c."NodeId") ORDER BY c."SiblingOrdinal") FROM bom_variant_node c WHERE c."VariantId"=vid AND c."ParentNodeId"=nid),'[]')),
 n."PropertyMask",ARRAY['productId','revisionId','name','unitId','unit','classes','positionId','quantity','amount','forQuantity','values','source','children']) INTO answer;
 RETURN answer;
END $$;
CREATE FUNCTION bom_read_variant(vid int) RETURNS jsonb LANGUAGE sql STABLE AS $$
 SELECT bom_read_node(vid,n."NodeId") FROM bom_variant_node n WHERE n."VariantId"=vid AND n."ParentNodeId" IS NULL
$$;
CREATE FUNCTION bom_import_bindings(did int,document jsonb) RETURNS void LANGUAGE plpgsql AS $$
DECLARE pos jsonb; binding record; child_revision int; target_document int;
BEGIN
 FOR pos IN SELECT value FROM jsonb_array_elements(coalesce(document->'positions','[]')) LOOP
  FOR binding IN SELECT * FROM jsonb_each_text(coalesce(pos->'bindings','{}')) LOOP
   child_revision:=(pos->>'revisionId')::int;
   SELECT "DocumentId" INTO STRICT target_document FROM bom_revision WHERE "Id"=child_revision;
   INSERT INTO bom_position_binding("DocumentId","PositionId","ParameterKey","ChildRevisionId","TargetDocumentId","Expression") VALUES(did,pos->>'id',binding.key,child_revision,target_document,binding.value);
  END LOOP;
 END LOOP;
END $$;
CREATE FUNCTION bom_import_document(document jsonb, with_bindings bool DEFAULT true) RETURNS int LANGUAGE plpgsql AS $$
DECLARE did int; item record; p jsonb; typ int; has_default bool; choice record; param_key text;
BEGIN
 IF jsonb_typeof(document) IS DISTINCT FROM 'object' THEN RAISE EXCEPTION 'bom_document: expected object, got %',document; END IF;
 INSERT INTO bom_document("PropertyMask","ProductName","UnitName") VALUES(bom_json_mask(document,ARRAY['positions','parameters','rules','calculations','productName','unitName','classes']),document->>'productName',document->>'unitName') RETURNING "Id" INTO did;
 INSERT INTO bom_document_class("DocumentId","Ordinal","ClassId") SELECT did,(ordinality-1)::int,value::text::int FROM jsonb_array_elements(coalesce(document->'classes','[]')) WITH ORDINALITY AS element(value, ordinality);
 FOR item IN SELECT value,ordinality FROM jsonb_array_elements(coalesce(document->'parameters','[]')) WITH ORDINALITY AS element(value, ordinality) LOOP
  p:=item.value; typ:=(p->>'type')::int; param_key:=p->>'key'; has_default:=p->'default' IS NOT NULL AND p->'default'<>'null'::jsonb;
  IF NOT EXISTS(SELECT 1 FROM parameter_definition WHERE "Id"=(p->>'definitionId')::int) THEN RAISE EXCEPTION 'bom_parameter document %, parameter %: missing parameter_definition id %',did,param_key,p->>'definitionId'; END IF;
  INSERT INTO bom_parameter("DocumentId","Key","Ordinal","PropertyMask","DefinitionId","Name","Type","Required","Min","Max","HasDefault","DefaultInteger","DefaultNumber","DefaultString","DefaultDate","DefaultDateText","DefaultEnumId") VALUES(
   did,param_key,item.ordinality-1,bom_json_mask(p,ARRAY['key','definitionId','name','type','required','min','max','enumIds','default','enumLabels']),
   (p->>'definitionId')::int,p->>'name',typ,coalesce((p->>'required')::bool,false),(p->>'min')::numeric,(p->>'max')::numeric,has_default,
   CASE WHEN has_default AND typ=1 THEN (p->>'default')::numeric::int END,CASE WHEN has_default AND typ=2 THEN (p->>'default')::numeric END,CASE WHEN has_default AND typ=3 THEN p->>'default' END,
   CASE WHEN has_default AND typ=4 THEN (p->>'default')::timestamptz END,CASE WHEN has_default AND typ=4 THEN p->>'default' END,CASE WHEN has_default AND typ=5 THEN (p->>'default')::numeric::int END);
  FOR choice IN SELECT coalesce(a.id,b.id) AS id,a.ord,b.label,b.has_label FROM
   (SELECT value::text::int id,(ordinality-1)::int ord FROM jsonb_array_elements(coalesce(p->'enumIds','[]')) WITH ORDINALITY AS element(value, ordinality)) a FULL JOIN
   (SELECT key::int id,value #>> '{}' label,true has_label FROM jsonb_each(coalesce(p->'enumLabels','{}'))) b USING(id) LOOP
   IF NOT EXISTS(SELECT 1 FROM enum_value WHERE "Id"=choice.id) THEN RAISE EXCEPTION 'bom_parameter_enum document %, parameter %: missing enum_value id %',did,param_key,choice.id; END IF;
   INSERT INTO bom_parameter_enum("DocumentId","ParameterKey","EnumValueId","AllowedOrdinal","HasLabel","Label") VALUES(did,param_key,choice.id,choice.ord,coalesce(choice.has_label,false),choice.label);
  END LOOP;
 END LOOP;
 FOR item IN SELECT value,ordinality FROM jsonb_array_elements(coalesce(document->'positions','[]')) WITH ORDINALITY AS element(value, ordinality) LOOP
  p:=item.value;
  INSERT INTO bom_position("DocumentId","PositionId","Ordinal","PropertyMask","ComponentId","RevisionId","UnitId","Amount","ForQuantity","SortOrder","Condition","Formula","ChildVariantId","Excluded","Inherited") VALUES(did,p->>'id',item.ordinality-1,bom_json_mask(p,ARRAY['id','componentId','revisionId','amount','forQuantity','unitId','sortOrder','condition','formula','childVariantId','bindings','excluded','inherited']),nullif((p->>'componentId')::int,0),nullif((p->>'revisionId')::int,0),nullif((p->>'unitId')::int,0),(p->>'amount')::numeric,(p->>'forQuantity')::numeric,(p->>'sortOrder')::int,p->>'condition',p->>'formula',(p->>'childVariantId')::int,coalesce((p->>'excluded')::bool,false),coalesce((p->>'inherited')::bool,false));
 END LOOP;
 INSERT INTO bom_rule("DocumentId","Ordinal","Expression","Message") SELECT did,ordinality-1,value->>'expression',value->>'message' FROM jsonb_array_elements(coalesce(document->'rules','[]')) WITH ORDINALITY AS element(value, ordinality);
 INSERT INTO bom_calculation("DocumentId","ParameterKey","Expression") SELECT did,key,value FROM jsonb_each_text(coalesce(document->'calculations','{}'));
 IF with_bindings THEN
  PERFORM bom_import_bindings(did,document);
  IF bom_read_document(did) IS DISTINCT FROM document THEN RAISE EXCEPTION 'bom_document id %: lossless roundtrip failed (unknown fields, invalid types or unsupported nulls); migration/write aborted',did; END IF;
 END IF;
 RETURN did;
EXCEPTION WHEN foreign_key_violation THEN RAISE EXCEPTION 'bom_document id %: dangling historical reference (%). No catalog data was substituted.',did,SQLERRM;
END $$;
CREATE FUNCTION bom_import_node(vid int,parent_node int,sibling_ordinal int,node jsonb) RETURNS int LANGUAGE plpgsql AS $$
DECLARE nid int; source_document int; parent_source int; item record; parameter_type int;
BEGIN
 source_document:=bom_import_document(coalesce(node->'source','{}'));
 SELECT coalesce(max("NodeId"),0)+1 INTO nid FROM bom_variant_node WHERE "VariantId"=vid;
 SELECT "SourceDocumentId" INTO parent_source FROM bom_variant_node WHERE "VariantId"=vid AND "NodeId"=parent_node;
 INSERT INTO bom_variant_node("VariantId","NodeId","ParentNodeId","ParentSourceDocumentId","SiblingOrdinal","SourceDocumentId","PropertyMask","ProductId","RevisionId","UnitId","Name","Unit","PositionId","Quantity","Amount","ForQuantity") VALUES(vid,nid,parent_node,parent_source,sibling_ordinal,source_document,
 bom_json_mask(node,ARRAY['productId','revisionId','name','unitId','unit','classes','positionId','quantity','amount','forQuantity','values','source','children']),
 (node->>'productId')::int,(node->>'revisionId')::int,(node->>'unitId')::int,node->>'name',node->>'unit',node->>'positionId',coalesce((node->>'quantity')::numeric,1),(node->>'amount')::numeric,(node->>'forQuantity')::numeric);
 INSERT INTO bom_variant_node_class("VariantId","NodeId","Ordinal","ClassId") SELECT vid,nid,ordinality-1,value::text::int FROM jsonb_array_elements(coalesce(node->'classes','[]')) WITH ORDINALITY AS element(value, ordinality);
 FOR item IN SELECT * FROM jsonb_each(coalesce(node->'values','{}')) LOOP
  SELECT "Type" INTO STRICT parameter_type FROM bom_parameter WHERE "DocumentId"=source_document AND "Key"=item.key;
  INSERT INTO bom_variant_node_value("VariantId","NodeId","SourceDocumentId","ParameterKey","Type","IntegerValue","NumberValue","StringValue","DateValue","DateText","EnumValueId") VALUES(vid,nid,source_document,item.key,parameter_type,
  CASE WHEN parameter_type=1 THEN (item.value #>> '{}')::numeric::int END,CASE WHEN parameter_type=2 THEN (item.value #>> '{}')::numeric END,CASE WHEN parameter_type=3 THEN item.value #>> '{}' END,
  CASE WHEN parameter_type=4 THEN (item.value #>> '{}')::timestamptz END,CASE WHEN parameter_type=4 THEN item.value #>> '{}' END,CASE WHEN parameter_type=5 THEN (item.value #>> '{}')::numeric::int END);
 END LOOP;
 FOR item IN SELECT value,ordinality FROM jsonb_array_elements(coalesce(node->'children','[]')) WITH ORDINALITY AS element(value, ordinality) LOOP PERFORM bom_import_node(vid,nid,(item.ordinality-1)::int,item.value); END LOOP;
 RETURN nid;
EXCEPTION WHEN foreign_key_violation THEN RAISE EXCEPTION 'bom_variant_node variant %, node %: dangling historical reference (%). No catalog data was substituted.',vid,nid,SQLERRM;
END $$;

-- No old migration is edited. Original JSON remains available until every
-- original aggregate has been imported and compared exactly as JSONB.
ALTER TABLE bom_parameter ALTER CONSTRAINT "FK_bom_parameter_default_enum" DEFERRABLE INITIALLY DEFERRED;
CREATE TEMP TABLE bom_upgrade_find ON COMMIT DROP AS SELECT v."Id" vid,(SELECT coalesce(jsonb_agg(t ORDER BY t.path),'[]') FROM find_bom(v."Id",1) t) rows FROM bom_variant v;
CREATE TEMP TABLE bom_upgrade_order ON COMMIT DROP AS SELECT r."Id" rid,(SELECT coalesce(jsonb_agg(t ORDER BY t.product_id,t.unit_id,t.name),'[]') FROM calc_order_bom(r."Id") t) rows FROM bom_order_revision r;
DROP TRIGGER bom_revision_immutable ON bom_revision;
DO $$
DECLARE legacy record; did int;
BEGIN
 FOR legacy IN SELECT * FROM bom_revision ORDER BY "Id" LOOP
  BEGIN
   did:=bom_import_document(legacy."Document",false);
   UPDATE bom_revision SET "DocumentId"=did WHERE "Id"=legacy."Id";
  EXCEPTION WHEN OTHERS THEN RAISE EXCEPTION 'bom_revision id %: backfill failed: %',legacy."Id",SQLERRM; END;
 END LOOP;
 FOR legacy IN SELECT * FROM bom_revision ORDER BY "Id" LOOP
  BEGIN
   PERFORM bom_import_bindings(legacy."DocumentId",legacy."Document");
   IF bom_read_document(legacy."DocumentId") IS DISTINCT FROM legacy."Document" THEN RAISE EXCEPTION 'lossless Document roundtrip failed (unknown fields/types)'; END IF;
  EXCEPTION WHEN OTHERS THEN RAISE EXCEPTION 'bom_revision id %: %',legacy."Id",SQLERRM; END;
 END LOOP;
 FOR legacy IN SELECT * FROM bom_variant ORDER BY "Id" LOOP
  BEGIN
   PERFORM bom_import_node(legacy."Id",NULL,0,legacy."Snapshot");
   IF bom_read_variant(legacy."Id") IS DISTINCT FROM legacy."Snapshot" THEN RAISE EXCEPTION 'lossless Snapshot roundtrip failed (unknown fields/types)'; END IF;
  EXCEPTION WHEN OTHERS THEN RAISE EXCEPTION 'bom_variant id %: %',legacy."Id",SQLERRM; END;
 END LOOP;
END $$;
ALTER TABLE bom_revision ALTER COLUMN "DocumentId" SET NOT NULL;
CREATE TRIGGER bom_revision_immutable BEFORE UPDATE OR DELETE ON bom_revision FOR EACH ROW EXECUTE FUNCTION bom_immutable();

CREATE OR REPLACE FUNCTION bom_effective_positions(rid integer, nesting integer DEFAULT 0) RETURNS jsonb LANGUAGE plpgsql STABLE AS $$
DECLARE r bom_revision; inherited jsonb := '[]'; own jsonb;
BEGIN
 IF nesting>32 THEN RAISE EXCEPTION 'Слишком длинная цепочка модификаций.'; END IF;
 SELECT * INTO STRICT r FROM bom_revision WHERE "Id"=rid;
 own := bom_read_document(r."DocumentId")->'positions';
 IF r."BaseRevisionId" IS NOT NULL THEN inherited := bom_effective_positions(r."BaseRevisionId",nesting+1); END IF;
 RETURN coalesce((SELECT jsonb_agg(p ORDER BY (p->>'sortOrder')::int,p->>'id') FROM (
   SELECT i.value AS p FROM jsonb_array_elements(inherited) i WHERE NOT EXISTS(
     SELECT 1 FROM jsonb_array_elements(own) o WHERE o.value->>'id'=i.value->>'id')
   UNION ALL SELECT value FROM jsonb_array_elements(own)
 ) s WHERE NOT coalesce((p->>'excluded')::boolean,false)), '[]');
END $$;

CREATE OR REPLACE FUNCTION bom_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'История и зафиксированные варианты неизменяемы. Создайте новую версию.'; END $$;

CREATE OR REPLACE FUNCTION bom_guard_revision() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE last_id integer; b bom_revision; pos jsonb; child bom_revision; cycle_found boolean;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT max("Id") INTO last_id FROM bom_revision WHERE "ProductId"=NEW."ProductId";
 IF NEW."PreviousId" IS DISTINCT FROM last_id OR (last_id IS NOT NULL AND NEW."Id"<=last_id) THEN RAISE EXCEPTION 'Спецификация уже изменена. Обновите страницу.' USING ERRCODE='40001'; END IF;
 IF length(trim(NEW."Reason"))=0 THEN RAISE EXCEPTION 'Укажите причину изменения.'; END IF;
 IF NOT EXISTS(SELECT 1 FROM bom_document WHERE "Id"=NEW."DocumentId" AND "CreationTransaction"=pg_current_xact_id()) THEN RAISE EXCEPTION 'Новый документ версии должен создаваться в той же транзакции.'; END IF;
 NEW."CreationTransaction":=pg_current_xact_id();
 IF last_id IS NOT NULL AND EXISTS(SELECT 1 FROM bom_revision WHERE "ProductId"=NEW."ProductId" AND ("UnitId"<>NEW."UnitId" OR "BaseRevisionId" IS DISTINCT FROM NEW."BaseRevisionId")) THEN
 RAISE EXCEPTION 'Единица изделия и версия базы фиксированы первой спецификацией.'; END IF;
 IF NEW."BaseRevisionId" IS NOT NULL THEN
   SELECT * INTO STRICT b FROM bom_revision WHERE "Id"=NEW."BaseRevisionId";
   IF b."ProductId"=NEW."ProductId" OR b."UnitId"<>NEW."UnitId" THEN RAISE EXCEPTION 'Неверная база модификации или единица.'; END IF;
 END IF;
 IF jsonb_typeof(bom_read_document(NEW."DocumentId")->'parameters') IS DISTINCT FROM 'array' OR jsonb_typeof(bom_read_document(NEW."DocumentId")->'rules') IS DISTINCT FROM 'array' OR jsonb_typeof(bom_read_document(NEW."DocumentId")->'calculations') IS DISTINCT FROM 'object' THEN RAISE EXCEPTION 'Неверная структура параметров и правил.'; END IF;
 IF jsonb_typeof(bom_read_document(NEW."DocumentId")->'positions') IS DISTINCT FROM 'array' THEN RAISE EXCEPTION 'Ожидался список позиций.'; END IF;
 IF (SELECT count(*)<>count(DISTINCT x->>'id') FROM jsonb_array_elements(bom_read_document(NEW."DocumentId")->'positions') x) THEN RAISE EXCEPTION 'Повторяется идентификатор позиции.'; END IF;
 FOR pos IN SELECT value FROM jsonb_array_elements(bom_read_document(NEW."DocumentId")->'positions') LOOP
   IF coalesce(pos->>'id','') !~ '^[A-Za-z0-9_-]{1,64}$' THEN RAISE EXCEPTION 'Неверный идентификатор позиции.'; END IF;
   IF coalesce((pos->>'excluded')::boolean,false) THEN
     IF NEW."BaseRevisionId" IS NULL OR NOT EXISTS(SELECT 1 FROM jsonb_array_elements(bom_effective_positions(NEW."BaseRevisionId")) x WHERE x->>'id'=pos->>'id') THEN RAISE EXCEPTION 'Исключать можно только позицию базы.'; END IF;
     CONTINUE;
   END IF;
   SELECT * INTO child FROM bom_revision WHERE "Id"=(pos->>'revisionId')::integer;
   IF child."Id" IS NULL OR child."ProductId" IS DISTINCT FROM (pos->>'componentId')::int OR child."UnitId" IS DISTINCT FROM (pos->>'unitId')::int THEN RAISE EXCEPTION 'Компонент, версия и единица не согласованы.'; END IF;
   IF NOT coalesce((pos->>'amount')::numeric>0 AND (pos->>'amount')::numeric<=1000000000 AND (pos->>'forQuantity')::numeric>0 AND (pos->>'forQuantity')::numeric<=1000000000 AND (pos->>'amount')::numeric/nullif((pos->>'forQuantity')::numeric,0)<=1000000000,false) THEN RAISE EXCEPTION 'Нормы должны быть положительными и ограниченными.'; END IF;
   IF pos->>'childVariantId' IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bom_variant WHERE "Id"=(pos->>'childVariantId')::int AND "RevisionId"=child."Id") THEN RAISE EXCEPTION 'Вложенный вариант другой версии.'; END IF;
 END LOOP;
 RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION bom_guard_cycles() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE bad boolean;
BEGIN
 -- Check the current graph as well as pinned historical paths. The statement lock
 -- serializes opposing A->B / B->A writes, including ordinary direct SQL inserts.
 WITH RECURSIVE latest AS (SELECT DISTINCT ON ("ProductId") "ProductId", "Id" FROM bom_revision ORDER BY "ProductId","Id" DESC),
 edges AS (SELECT l."ProductId" p, (x->>'componentId')::int c FROM latest l CROSS JOIN LATERAL jsonb_array_elements(bom_effective_positions(l."Id")) x),
 walk AS (SELECT NEW."ProductId" AS node, ARRAY[NEW."ProductId"] AS path, false AS cycle
 UNION ALL SELECT e.c, w.path||e.c, e.c=ANY(w.path) FROM walk w JOIN edges e ON e.p=w.node WHERE NOT w.cycle AND cardinality(w.path)<=33)
 SELECT coalesce(bool_or(cycle OR cardinality(path)>33),false) INTO bad FROM walk;
 IF bad THEN RAISE EXCEPTION 'Цикл или превышение глубины состава.'; END IF;
 WITH RECURSIVE walk AS (
 SELECT NEW."Id" AS rid, ARRAY[NEW."ProductId"] AS path,false AS cycle
 UNION ALL SELECT c."Id",w.path||c."ProductId",c."ProductId"=ANY(w.path)
 FROM walk w CROSS JOIN LATERAL jsonb_array_elements(bom_effective_positions(w.rid)) x
 JOIN bom_revision c ON c."Id"=(x->>'revisionId')::int WHERE NOT w.cycle AND cardinality(w.path)<=33)
 SELECT coalesce(bool_or(cycle OR cardinality(path)>33),false) INTO bad FROM walk;
 IF bad THEN RAISE EXCEPTION 'Цикл в зафиксированной версии состава.'; END IF;
 RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION bom_guard_order() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE last_id int;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT max("Id") INTO last_id FROM bom_order_revision WHERE "OrderId"=NEW."OrderId";
 IF NEW."PreviousId" IS DISTINCT FROM last_id OR (last_id IS NOT NULL AND NEW."Id"<=last_id)
 THEN RAISE EXCEPTION 'Заказ уже изменён.' USING ERRCODE='40001'; END IF;
 IF length(trim(NEW."Reason"))=0 THEN RAISE EXCEPTION 'Укажите причину изменения.'; END IF;
 NEW."CreationTransaction" := pg_current_xact_id();
 RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION find_bom(variant_id int, quantity numeric DEFAULT 1)
RETURNS TABLE(path text, depth int, product_id int, name text, unit_id int, unit text, required_quantity numeric, classes jsonb, leaf boolean, revision_id int, norm numeric, for_quantity numeric, position_id text)
LANGUAGE plpgsql STABLE AS $$
BEGIN
 IF NOT (quantity>0 AND quantity<=1000000000) OR quantity IS NULL THEN RAISE EXCEPTION 'Количество должно быть положительным и ограниченным.'; END IF;
 IF NOT EXISTS(SELECT 1 FROM bom_variant WHERE "Id"=variant_id) THEN RAISE EXCEPTION 'Вариант не найден.'; END IF;
 RETURN QUERY WITH RECURSIVE tree AS (
  SELECT n."NodeId" nid,''::text p,0 d,quantity q FROM bom_variant_node n WHERE n."VariantId"=variant_id AND n."ParentNodeId" IS NULL
  UNION ALL SELECT n."NodeId",t.p||'/'||n."PositionId",t.d+1,t.q*n."Quantity" FROM tree t JOIN bom_variant_node n ON n."VariantId"=variant_id AND n."ParentNodeId"=t.nid
 ) SELECT t.p,t.d,n."ProductId",n."Name",n."UnitId",n."Unit",t.q,
 CASE WHEN (n."PropertyMask"&32)<>0 THEN coalesce((SELECT jsonb_agg(c."ClassId" ORDER BY c."Ordinal") FROM bom_variant_node_class c WHERE c."VariantId"=variant_id AND c."NodeId"=n."NodeId"),'[]') END,
 CASE WHEN (n."PropertyMask"&4096)<>0 THEN NOT EXISTS(SELECT 1 FROM bom_variant_node c WHERE c."VariantId"=variant_id AND c."ParentNodeId"=n."NodeId") END,
 n."RevisionId",coalesce(n."Amount",n."Quantity"),coalesce(n."ForQuantity",1),n."PositionId" FROM tree t JOIN bom_variant_node n ON n."VariantId"=variant_id AND n."NodeId"=t.nid ORDER BY t.p;
END $$;

CREATE OR REPLACE FUNCTION calc_bom(variant_id int, quantity numeric DEFAULT 1, class_id int DEFAULT NULL)
RETURNS TABLE(product_id int, name text, unit_id int, unit text, required_quantity numeric)
LANGUAGE sql STABLE AS $$
 SELECT b.product_id,b.name,b.unit_id,b.unit,sum(b.required_quantity) FROM find_bom(variant_id,quantity) b
 WHERE b.leaf AND (class_id IS NULL OR b.classes @> to_jsonb(ARRAY[class_id]))
 GROUP BY b.product_id,b.name,b.unit_id,b.unit ORDER BY b.name,b.unit_id
$$;

CREATE OR REPLACE FUNCTION calc_order_bom(order_revision_id int, class_id int DEFAULT NULL)
RETURNS TABLE(product_id int, name text, unit_id int, unit text, required_quantity numeric)
LANGUAGE sql STABLE AS $$
 SELECT b.product_id,b.name,b.unit_id,b.unit,sum(b.required_quantity)
 FROM bom_order_line l CROSS JOIN LATERAL calc_bom(l."VariantId",l."Quantity",class_id) b
 WHERE l."OrderRevisionId"=order_revision_id GROUP BY b.product_id,b.name,b.unit_id,b.unit ORDER BY b.name,b.unit_id
$$;

CREATE OR REPLACE FUNCTION register_change(product_id int, unit_id int, expected_revision_id int, base_revision_id int, document jsonb, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE rid int; did int;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 did:=bom_import_document(document);
 INSERT INTO bom_revision("ProductId","UnitId","PreviousId","BaseRevisionId","DocumentId","Reason","CreatedAt") VALUES(product_id,unit_id,expected_revision_id,base_revision_id,did,reason,now()) RETURNING "Id" INTO rid;
 RETURN rid;
END $$;

CREATE OR REPLACE FUNCTION bom_create_variant(revision_id int, variant_name text, snapshot jsonb) RETURNS int LANGUAGE plpgsql AS $$
DECLARE vid int;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt") VALUES(revision_id,variant_name,now()) RETURNING "Id" INTO vid;
 PERFORM bom_import_node(vid,NULL,0,snapshot);
 IF bom_read_variant(vid) IS DISTINCT FROM snapshot THEN RAISE EXCEPTION 'bom_variant %: lossless roundtrip failed (unknown fields/types); write aborted',vid; END IF;
 RETURN vid;
END $$;

CREATE OR REPLACE FUNCTION create_modification(base_revision_id int, name text, short_name text, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE b bom_revision; pid int; doc jsonb;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO STRICT b FROM bom_revision WHERE "Id"=base_revision_id;
 INSERT INTO product("Name","ShortName","ClassNodeId") SELECT name,short_name,"ClassNodeId" FROM product WHERE "Id"=b."ProductId" RETURNING "Id" INTO pid;
 doc:=jsonb_set(jsonb_set(bom_read_document(b."DocumentId"),'{positions}','[]'),'{productName}',to_jsonb(name));
 PERFORM register_change(pid,b."UnitId",NULL,b."Id",doc,reason);
 RETURN pid;
END $$;

CREATE OR REPLACE FUNCTION bom_put_position(product_id int, expected_revision_id int, item jsonb, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE r bom_revision; positions jsonb;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO STRICT r FROM bom_revision WHERE "Id"=expected_revision_id AND "ProductId"=product_id;
 SELECT coalesce(jsonb_agg(x),'[]') INTO positions FROM jsonb_array_elements(bom_read_document(r."DocumentId")->'positions') x WHERE x->>'id' IS DISTINCT FROM item->>'id';
 RETURN register_change(product_id,r."UnitId",r."Id",r."BaseRevisionId",jsonb_set(bom_read_document(r."DocumentId"),'{positions}',positions||jsonb_build_array(item)),reason);
END $$;

CREATE OR REPLACE FUNCTION bom_remove_position(product_id int, expected_revision_id int, position_id text, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE r bom_revision; positions jsonb;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO STRICT r FROM bom_revision WHERE "Id"=expected_revision_id AND "ProductId"=product_id;
 IF r."BaseRevisionId" IS NOT NULL AND EXISTS(SELECT 1 FROM jsonb_array_elements(bom_effective_positions(r."BaseRevisionId")) x WHERE x->>'id'=position_id) THEN
 RETURN bom_put_position(product_id,expected_revision_id,jsonb_build_object('id',position_id,'excluded',true),reason);
 END IF;
 IF NOT EXISTS(SELECT 1 FROM jsonb_array_elements(bom_read_document(r."DocumentId")->'positions') x WHERE x->>'id'=position_id) THEN RAISE EXCEPTION 'Позиция не найдена.'; END IF;
 SELECT coalesce(jsonb_agg(x),'[]') INTO positions FROM jsonb_array_elements(bom_read_document(r."DocumentId")->'positions') x WHERE x->>'id'<>position_id;
 RETURN register_change(product_id,r."UnitId",r."Id",r."BaseRevisionId",jsonb_set(bom_read_document(r."DocumentId"),'{positions}',positions),reason);
END $$;

CREATE OR REPLACE FUNCTION bom_guard_order_line() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NOT EXISTS (SELECT 1 FROM bom_order_revision WHERE "Id"=NEW."OrderRevisionId" AND "CreationTransaction"=pg_current_xact_id())
 THEN RAISE EXCEPTION 'Позиции сохранённой версии заказа неизменяемы. Создайте новую версию.'; END IF;
 RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION bom_check_order_lines() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE rid int; n bigint; last_position int;
BEGIN
 IF TG_TABLE_NAME='bom_order_revision' THEN rid:=NEW."Id"; ELSE rid:=NEW."OrderRevisionId"; END IF;
 SELECT count(*),max("SortOrder") INTO n,last_position FROM bom_order_line WHERE "OrderRevisionId"=rid;
 IF n=0 OR last_position<>n-1 THEN RAISE EXCEPTION 'Нужны позиции заказа с последовательным порядком от нуля.'; END IF;
 RETURN NULL;
END $$;

CREATE OR REPLACE FUNCTION bom_guard_revision_sequence() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF EXISTS (SELECT 1 FROM bom_revision WHERE "ProductId"=NEW."ProductId" AND "Id">=NEW."Id")
 THEN RAISE EXCEPTION 'Идентификатор новой версии должен следовать истории.' USING ERRCODE='40001'; END IF;
 RETURN NEW;
END $$;

CREATE FUNCTION bom_guard_document() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN NEW."CreationTransaction":=pg_current_xact_id(); RETURN NEW; END $$;
CREATE FUNCTION bom_guard_document_row() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE did int; pinned_revision int;
BEGIN
 did:=(to_jsonb(NEW)->>'DocumentId')::int;
 IF NOT EXISTS(SELECT 1 FROM bom_document WHERE "Id"=did AND "CreationTransaction"=pg_current_xact_id()) THEN RAISE EXCEPTION '% document %: saved document rows are immutable',TG_TABLE_NAME,did; END IF;
 IF TG_TABLE_NAME='bom_position_binding' THEN
  SELECT "RevisionId" INTO pinned_revision FROM bom_position WHERE "DocumentId"=NEW."DocumentId" AND "PositionId"=NEW."PositionId";
  IF pinned_revision IS DISTINCT FROM NEW."ChildRevisionId" THEN RAISE EXCEPTION 'bom_position_binding document %, position %: child revision mismatch',did,NEW."PositionId"; END IF;
 END IF;
 RETURN NEW;
END $$;
CREATE FUNCTION bom_validate_document_id(did int) RETURNS void LANGUAGE plpgsql AS $$
DECLARE owners int; mask bigint; tbl text; count_rows bigint; last_ordinal int;
BEGIN
 SELECT "PropertyMask" INTO mask FROM bom_document WHERE "Id"=did;
 SELECT (SELECT count(*) FROM bom_revision WHERE "DocumentId"=did)+(SELECT count(*) FROM bom_variant_node WHERE "SourceDocumentId"=did) INTO owners;
 IF owners<>1 THEN RAISE EXCEPTION 'bom_document %: document must have exactly one immutable revision/node owner; got %',did,owners; END IF;
 FOREACH tbl IN ARRAY ARRAY['bom_document_class','bom_position','bom_parameter','bom_rule'] LOOP
  EXECUTE format('SELECT count(*),max("Ordinal") FROM %I WHERE "DocumentId"=$1',tbl) INTO count_rows,last_ordinal USING did;
  IF count_rows>0 AND last_ordinal<>count_rows-1 THEN RAISE EXCEPTION '% document %: incomplete ordering',tbl,did; END IF;
 END LOOP;
 IF EXISTS(SELECT 1 FROM bom_parameter p WHERE p."DocumentId"=did AND p."DefaultEnumId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bom_parameter_enum e WHERE e."DocumentId"=did AND e."ParameterKey"=p."Key" AND e."EnumValueId"=p."DefaultEnumId" AND e."AllowedOrdinal" IS NOT NULL)) THEN RAISE EXCEPTION 'bom_parameter document %: default enum must belong to frozen allowed set',did; END IF;
 IF EXISTS(SELECT 1 FROM bom_parameter_enum e WHERE e."DocumentId"=did AND e."AllowedOrdinal" IS NOT NULL GROUP BY e."ParameterKey" HAVING max(e."AllowedOrdinal")<>count(*)-1) THEN RAISE EXCEPTION 'bom_parameter_enum document %: incomplete allowed ordering',did; END IF;
 IF (mask&1)=0 AND EXISTS(SELECT 1 FROM bom_position WHERE "DocumentId"=did)
 OR (mask&2)=0 AND EXISTS(SELECT 1 FROM bom_parameter WHERE "DocumentId"=did)
 OR (mask&4)=0 AND EXISTS(SELECT 1 FROM bom_rule WHERE "DocumentId"=did)
 OR (mask&8)=0 AND EXISTS(SELECT 1 FROM bom_calculation WHERE "DocumentId"=did)
 OR (mask&64)=0 AND EXISTS(SELECT 1 FROM bom_document_class WHERE "DocumentId"=did) THEN RAISE EXCEPTION 'bom_document %: field-presence mask cannot hide relational rows',did; END IF;
 IF EXISTS(SELECT 1 FROM bom_position_binding b JOIN bom_position p ON p."DocumentId"=b."DocumentId" AND p."PositionId"=b."PositionId" WHERE b."DocumentId"=did AND (b."ChildRevisionId" IS DISTINCT FROM p."RevisionId" OR p."ChildVariantId" IS NOT NULL OR (p."PropertyMask"&1024)=0)) THEN RAISE EXCEPTION 'bom_position_binding document %: invalid child pin, mixed variant/bindings or hidden rows',did; END IF;
 IF EXISTS(SELECT 1 FROM bom_parameter p WHERE p."DocumentId"=did AND (p."HasDefault" AND (p."PropertyMask"&256)=0 OR (p."PropertyMask"&128)=0 AND EXISTS(SELECT 1 FROM bom_parameter_enum e WHERE e."DocumentId"=did AND e."ParameterKey"=p."Key" AND e."AllowedOrdinal" IS NOT NULL) OR (p."PropertyMask"&512)=0 AND EXISTS(SELECT 1 FROM bom_parameter_enum e WHERE e."DocumentId"=did AND e."ParameterKey"=p."Key" AND e."HasLabel"))) THEN RAISE EXCEPTION 'bom_parameter document %: field mask cannot hide default/enum rows',did; END IF;
 IF EXISTS(SELECT 1 FROM bom_parameter p WHERE p."DocumentId"=did AND (p."Min"::text IN ('NaN','Infinity','-Infinity') OR p."Max"::text IN ('NaN','Infinity','-Infinity') OR p."DefaultNumber"::text IN ('NaN','Infinity','-Infinity'))) OR EXISTS(SELECT 1 FROM bom_position p WHERE p."DocumentId"=did AND (p."Amount"::text IN ('NaN','Infinity','-Infinity') OR p."ForQuantity"::text IN ('NaN','Infinity','-Infinity'))) THEN RAISE EXCEPTION 'bom_document %: numeric snapshots must be finite',did; END IF;
 RETURN;
END $$;
CREATE FUNCTION bom_check_document() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE did int;
BEGIN
 IF TG_TABLE_NAME='bom_document' THEN did:=NEW."Id"; ELSE did:=NEW."DocumentId"; END IF;
 PERFORM bom_validate_document_id(did); RETURN NULL;
END $$;
CREATE FUNCTION bom_guard_variant_header() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 NEW."CreationTransaction":=pg_current_xact_id();
 IF length(trim(NEW."Name"))=0 THEN RAISE EXCEPTION 'Укажите имя варианта.'; END IF;
 RETURN NEW;
END $$;
CREATE FUNCTION bom_guard_variant_row() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE vid int; parent_position bom_position; mask bigint;
BEGIN
 vid:=NEW."VariantId";
 IF NOT EXISTS(SELECT 1 FROM bom_variant WHERE "Id"=vid AND "CreationTransaction"=pg_current_xact_id()) THEN RAISE EXCEPTION '% variant %: saved variant rows are immutable',TG_TABLE_NAME,vid; END IF;
 IF TG_TABLE_NAME='bom_variant_node' THEN
  IF NOT EXISTS(SELECT 1 FROM bom_document WHERE "Id"=NEW."SourceDocumentId" AND "CreationTransaction"=pg_current_xact_id()) THEN RAISE EXCEPTION 'bom_variant_node variant %: new frozen Source required',vid; END IF;
  IF NEW."ParentNodeId" IS NOT NULL THEN
   SELECT "PropertyMask" INTO mask FROM bom_variant_node WHERE "VariantId"=vid AND "NodeId"=NEW."ParentNodeId";
   IF (mask&4096)=0 THEN RAISE EXCEPTION 'bom_variant %: parent field mask hides children',vid; END IF;
   SELECT * INTO STRICT parent_position FROM bom_position WHERE "DocumentId"=NEW."ParentSourceDocumentId" AND "PositionId"=NEW."PositionId";
   IF parent_position."Excluded" OR parent_position."RevisionId" IS DISTINCT FROM NEW."RevisionId" OR parent_position."ComponentId" IS DISTINCT FROM NEW."ProductId" OR parent_position."UnitId" IS DISTINCT FROM NEW."UnitId" THEN RAISE EXCEPTION 'bom_variant_node variant %, node %: frozen parent position pin mismatch/excluded',vid,NEW."NodeId"; END IF;
  END IF;
 ELSIF TG_TABLE_NAME='bom_variant_node_class' THEN
  SELECT "PropertyMask" INTO mask FROM bom_variant_node WHERE "VariantId"=vid AND "NodeId"=NEW."NodeId";
  IF (mask&32)=0 THEN RAISE EXCEPTION 'bom_variant %: node mask hides classes',vid; END IF;
 ELSIF TG_TABLE_NAME='bom_variant_node_value' THEN
  IF NEW."NumberValue"::text IN ('NaN','Infinity','-Infinity') THEN RAISE EXCEPTION 'bom_variant %: numeric value must be finite',vid; END IF;
  SELECT "PropertyMask" INTO mask FROM bom_variant_node WHERE "VariantId"=vid AND "NodeId"=NEW."NodeId";
  IF (mask&1024)=0 THEN RAISE EXCEPTION 'bom_variant %: node mask hides values',vid; END IF;
 IF NEW."EnumValueId" IS NOT NULL THEN
  IF NOT EXISTS(SELECT 1 FROM bom_parameter_enum WHERE "DocumentId"=NEW."SourceDocumentId" AND "ParameterKey"=NEW."ParameterKey" AND "EnumValueId"=NEW."EnumValueId" AND "AllowedOrdinal" IS NOT NULL) THEN RAISE EXCEPTION 'bom_variant_node_value variant %, node %: enum outside frozen allowed set',vid,NEW."NodeId"; END IF;
 END IF;
 END IF;
 RETURN NEW;
END $$;
CREATE FUNCTION bom_validate_variant_id(vid int) RETURNS void LANGUAGE plpgsql AS $$
DECLARE node_count bigint; reached bigint; max_depth int; root_revision int; pinned_revision int;
BEGIN
 SELECT count(*) INTO node_count FROM bom_variant_node WHERE "VariantId"=vid;
 WITH RECURSIVE walk AS (SELECT "NodeId",0 depth FROM bom_variant_node WHERE "VariantId"=vid AND "ParentNodeId" IS NULL UNION ALL SELECT n."NodeId",w.depth+1 FROM walk w JOIN bom_variant_node n ON n."VariantId"=vid AND n."ParentNodeId"=w."NodeId") SELECT count(*),max(depth) INTO reached,max_depth FROM walk;
 IF node_count=0 OR reached<>node_count OR max_depth>32 OR node_count>10000 THEN RAISE EXCEPTION 'bom_variant %: missing root, disconnected/cyclic or oversized tree',vid; END IF;
 SELECT "RevisionId" INTO root_revision FROM bom_variant_node WHERE "VariantId"=vid AND "ParentNodeId" IS NULL;
 SELECT "RevisionId" INTO pinned_revision FROM bom_variant WHERE "Id"=vid;
 IF root_revision IS DISTINCT FROM pinned_revision THEN RAISE EXCEPTION 'bom_variant %: root revision mismatch',vid; END IF;
 IF EXISTS(SELECT 1 FROM bom_variant_node n JOIN bom_position p ON p."DocumentId"=n."ParentSourceDocumentId" AND p."PositionId"=n."PositionId" WHERE n."VariantId"=vid AND (p."Excluded" OR p."RevisionId" IS DISTINCT FROM n."RevisionId" OR p."ComponentId" IS DISTINCT FROM n."ProductId" OR p."UnitId" IS DISTINCT FROM n."UnitId")) THEN RAISE EXCEPTION 'bom_variant %: frozen parent position pin mismatch/excluded',vid; END IF;
 IF EXISTS(SELECT 1 FROM bom_variant_node n WHERE n."VariantId"=vid GROUP BY n."ParentNodeId" HAVING max(n."SiblingOrdinal")<>count(*)-1) THEN RAISE EXCEPTION 'bom_variant %: incomplete sibling ordering',vid; END IF;
 IF EXISTS(SELECT 1 FROM bom_variant_node_value v WHERE v."VariantId"=vid AND v."EnumValueId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bom_parameter_enum e WHERE e."DocumentId"=v."SourceDocumentId" AND e."ParameterKey"=v."ParameterKey" AND e."EnumValueId"=v."EnumValueId" AND e."AllowedOrdinal" IS NOT NULL)) THEN RAISE EXCEPTION 'bom_variant %: enum value outside frozen allowed set',vid; END IF;
 IF EXISTS(SELECT 1 FROM bom_variant_node n JOIN bom_document d ON d."Id"=n."SourceDocumentId" WHERE n."VariantId"=vid AND (n."PropertyMask"&2048)=0 AND d."PropertyMask"<>0) THEN RAISE EXCEPTION 'bom_variant %: field mask hides frozen Source',vid; END IF;
 IF EXISTS(SELECT 1 FROM bom_variant_node n WHERE n."VariantId"=vid AND ((n."PropertyMask"&4096)=0 AND EXISTS(SELECT 1 FROM bom_variant_node c WHERE c."VariantId"=vid AND c."ParentNodeId"=n."NodeId") OR (n."PropertyMask"&32)=0 AND EXISTS(SELECT 1 FROM bom_variant_node_class c WHERE c."VariantId"=vid AND c."NodeId"=n."NodeId") OR (n."PropertyMask"&1024)=0 AND EXISTS(SELECT 1 FROM bom_variant_node_value v WHERE v."VariantId"=vid AND v."NodeId"=n."NodeId"))) THEN RAISE EXCEPTION 'bom_variant %: mask hides children/classes/values',vid; END IF;
 IF EXISTS(SELECT 1 FROM bom_variant_node_value v WHERE v."VariantId"=vid AND v."NumberValue"::text IN ('NaN','Infinity','-Infinity')) THEN RAISE EXCEPTION 'bom_variant %: numeric values must be finite',vid; END IF;
END $$;
CREATE FUNCTION bom_check_variant() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE vid int; node_count bigint; reached bigint; max_depth int; count_rows bigint; last_ordinal int; root_revision int;
BEGIN
 IF TG_TABLE_NAME='bom_variant' THEN vid:=NEW."Id"; ELSE vid:=NEW."VariantId"; END IF;
 IF TG_TABLE_NAME='bom_variant' THEN
  PERFORM bom_validate_variant_id(vid);
  SELECT count(*) INTO node_count FROM bom_variant_node WHERE "VariantId"=vid;
  WITH RECURSIVE walk AS (SELECT "NodeId",0 depth FROM bom_variant_node WHERE "VariantId"=vid AND "ParentNodeId" IS NULL UNION ALL SELECT n."NodeId",w.depth+1 FROM walk w JOIN bom_variant_node n ON n."VariantId"=vid AND n."ParentNodeId"=w."NodeId") SELECT count(*),max(depth) INTO reached,max_depth FROM walk;
  IF node_count=0 OR reached<>node_count OR max_depth>32 OR node_count>10000 THEN RAISE EXCEPTION 'bom_variant %: missing root, disconnected/cyclic or oversized frozen tree',vid; END IF;
  SELECT "RevisionId" INTO root_revision FROM bom_variant_node WHERE "VariantId"=vid AND "ParentNodeId" IS NULL;
  IF root_revision IS DISTINCT FROM NEW."RevisionId" THEN RAISE EXCEPTION 'bom_variant %: root revision mismatch',vid; END IF;
 ELSIF TG_TABLE_NAME='bom_variant_node' THEN
  SELECT count(*),max("SiblingOrdinal") INTO count_rows,last_ordinal FROM bom_variant_node WHERE "VariantId"=vid AND "ParentNodeId" IS NOT DISTINCT FROM NEW."ParentNodeId";
  IF last_ordinal<>count_rows-1 THEN RAISE EXCEPTION 'bom_variant %: incomplete sibling ordering',vid; END IF;
  -- Protect late appends even if SET CONSTRAINTS was made IMMEDIATE earlier.
  WITH RECURSIVE ancestors AS (SELECT "NodeId","ParentNodeId",0 depth FROM bom_variant_node WHERE "VariantId"=vid AND "NodeId"=NEW."NodeId" UNION ALL SELECT n."NodeId",n."ParentNodeId",a.depth+1 FROM ancestors a JOIN bom_variant_node n ON n."VariantId"=vid AND n."NodeId"=a."ParentNodeId") SELECT max(depth) INTO max_depth FROM ancestors;
  IF max_depth>32 THEN RAISE EXCEPTION 'bom_variant %: depth exceeds32',vid; END IF;
  SELECT count(*) INTO node_count FROM (SELECT 1 FROM bom_variant_node WHERE "VariantId"=vid LIMIT 10001) limited;
  IF node_count>10000 OR NOT EXISTS(SELECT 1 FROM bom_variant_node n JOIN bom_variant v ON v."Id"=vid WHERE n."VariantId"=vid AND n."ParentNodeId" IS NULL AND n."RevisionId"=v."RevisionId") THEN RAISE EXCEPTION 'bom_variant %: oversized tree or missing/mismatched root',vid; END IF;
 ELSIF TG_TABLE_NAME='bom_variant_node_class' THEN
  SELECT count(*),max("Ordinal") INTO count_rows,last_ordinal FROM bom_variant_node_class WHERE "VariantId"=vid AND "NodeId"=NEW."NodeId";
  IF last_ordinal<>count_rows-1 THEN RAISE EXCEPTION 'bom_variant %: incomplete class ordering',vid; END IF;
 END IF;
 RETURN NULL;
END $$;

CREATE TRIGGER bom_document_create BEFORE INSERT ON bom_document FOR EACH ROW EXECUTE FUNCTION bom_guard_document();
CREATE CONSTRAINT TRIGGER bom_document_publish AFTER INSERT ON bom_document DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_document();
CREATE TRIGGER bom_variant_create BEFORE INSERT ON bom_variant FOR EACH ROW EXECUTE FUNCTION bom_guard_variant_header();
CREATE CONSTRAINT TRIGGER bom_variant_publish AFTER INSERT ON bom_variant DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_variant();
CREATE TRIGGER bom_doc_row_insert BEFORE INSERT ON bom_document_class FOR EACH ROW EXECUTE FUNCTION bom_guard_document_row();
CREATE CONSTRAINT TRIGGER bom_doc_row_publish AFTER INSERT ON bom_document_class DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_document();
CREATE TRIGGER bom_doc_row_insert BEFORE INSERT ON bom_position FOR EACH ROW EXECUTE FUNCTION bom_guard_document_row();
CREATE CONSTRAINT TRIGGER bom_doc_row_publish AFTER INSERT ON bom_position DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_document();
CREATE TRIGGER bom_doc_row_insert BEFORE INSERT ON bom_position_binding FOR EACH ROW EXECUTE FUNCTION bom_guard_document_row();
CREATE CONSTRAINT TRIGGER bom_doc_row_publish AFTER INSERT ON bom_position_binding DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_document();
CREATE TRIGGER bom_doc_row_insert BEFORE INSERT ON bom_parameter FOR EACH ROW EXECUTE FUNCTION bom_guard_document_row();
CREATE CONSTRAINT TRIGGER bom_doc_row_publish AFTER INSERT ON bom_parameter DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_document();
CREATE TRIGGER bom_doc_row_insert BEFORE INSERT ON bom_parameter_enum FOR EACH ROW EXECUTE FUNCTION bom_guard_document_row();
CREATE CONSTRAINT TRIGGER bom_doc_row_publish AFTER INSERT ON bom_parameter_enum DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_document();
CREATE TRIGGER bom_doc_row_insert BEFORE INSERT ON bom_rule FOR EACH ROW EXECUTE FUNCTION bom_guard_document_row();
CREATE CONSTRAINT TRIGGER bom_doc_row_publish AFTER INSERT ON bom_rule DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_document();
CREATE TRIGGER bom_doc_row_insert BEFORE INSERT ON bom_calculation FOR EACH ROW EXECUTE FUNCTION bom_guard_document_row();
CREATE CONSTRAINT TRIGGER bom_doc_row_publish AFTER INSERT ON bom_calculation DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_document();
CREATE TRIGGER bom_variant_row_insert BEFORE INSERT ON bom_variant_node FOR EACH ROW EXECUTE FUNCTION bom_guard_variant_row();
CREATE CONSTRAINT TRIGGER bom_variant_row_publish AFTER INSERT ON bom_variant_node DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_variant();
CREATE TRIGGER bom_variant_row_insert BEFORE INSERT ON bom_variant_node_class FOR EACH ROW EXECUTE FUNCTION bom_guard_variant_row();
CREATE CONSTRAINT TRIGGER bom_variant_row_publish AFTER INSERT ON bom_variant_node_class DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_variant();
CREATE TRIGGER bom_variant_row_insert BEFORE INSERT ON bom_variant_node_value FOR EACH ROW EXECUTE FUNCTION bom_guard_variant_row();
CREATE CONSTRAINT TRIGGER bom_variant_row_publish AFTER INSERT ON bom_variant_node_value DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_variant();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_document FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_document FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_document_class FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_document_class FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_position FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_position FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_position_binding FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_position_binding FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_parameter FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_parameter FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_parameter_enum FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_parameter_enum FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_rule FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_rule FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_calculation FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_calculation FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_variant_node FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_variant_node FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_variant_node_class FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_variant_node_class FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_immutable BEFORE UPDATE OR DELETE ON bom_variant_node_value FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_variant_node_value FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_revision FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_snapshot_no_truncate BEFORE TRUNCATE ON bom_variant FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();

-- Validate already-backfilled rows explicitly: creating triggers cannot schedule
-- constraint events retroactively for those historical rows.
DO $$ DECLARE item record; BEGIN
 FOR item IN SELECT "Id" FROM bom_document LOOP PERFORM bom_validate_document_id(item."Id"); END LOOP;
 FOR item IN SELECT "Id" FROM bom_variant LOOP PERFORM bom_validate_variant_id(item."Id"); END LOOP;
 IF EXISTS(SELECT 1 FROM bom_upgrade_find e WHERE e.rows IS DISTINCT FROM (SELECT coalesce(jsonb_agg(t ORDER BY t.path),'[]') FROM find_bom(e.vid,1) t)) THEN RAISE EXCEPTION 'Historical find_bom result changed; migration aborted'; END IF;
 IF EXISTS(SELECT 1 FROM bom_upgrade_order e WHERE e.rows IS DISTINCT FROM (SELECT coalesce(jsonb_agg(t ORDER BY t.product_id,t.unit_id,t.name),'[]') FROM calc_order_bom(e.rid) t)) THEN RAISE EXCEPTION 'Historical calc_order_bom result changed; migration aborted'; END IF;
END $$;
""");
            migrationBuilder.DropColumn(
                name: "Snapshot",
                table: "bom_variant");

            migrationBuilder.DropColumn(
                name: "Document",
                table: "bom_revision");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("История BOM не удаляется автоматически. Восстановите резервную копию в отдельную БД.");
        }
    }
}
