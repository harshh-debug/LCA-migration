using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace Lca.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantCatalogCapability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "migration");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDisabled",
                schema: "dbo",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlternateItemCode",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(name: "AlternateLocation", schema: "dbo", table: "Products", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "GujaratiName", schema: "dbo", table: "Products", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Location", schema: "dbo", table: "Products", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ManufacturerName", schema: "dbo", table: "Products", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Remark", schema: "dbo", table: "Products", type: "nvarchar(max)", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChapterNumber",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                schema: "dbo",
                table: "Products",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Group1",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Group2",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HsnNumber",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Packing",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesmanCommission",
                schema: "dbo",
                table: "Products",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitKilograms",
                schema: "dbo",
                table: "Products",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                schema: "dbo",
                table: "Products",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "WarrantyMonths",
                schema: "dbo",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyYears",
                schema: "dbo",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "DisplaySubCategory",
                schema: "dbo",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                schema: "dbo",
                table: "Categories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LegacyIconPath",
                schema: "dbo",
                table: "Categories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyNotificationImagePath",
                schema: "dbo",
                table: "Categories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                schema: "dbo",
                table: "Categories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Products_TenantId_Id",
                schema: "dbo",
                table: "Products",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "LegacyCategoryMaps",
                schema: "migration",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    LegacyCategoryId = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyCategoryMaps", x => new { x.TenantId, x.LegacyCategoryId });
                    table.ForeignKey(
                        name: "FK_LegacyCategoryMaps_Categories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalSchema: "dbo",
                        principalTable: "Categories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductInventory",
                schema: "dbo",
                columns: table => new
                {
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MaximumStock = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Godown1Stock = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Godown2Stock = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductInventory", x => new { x.TenantId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_ProductInventory_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductMedia",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    LegacyPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsThumbnail = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMedia_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPricing",
                schema: "dbo",
                columns: table => new
                {
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DealerRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WholesaleRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RetailRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OtherRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    AdditionalVatRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CstRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IgstRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SgstRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CgstRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPricing", x => new { x.TenantId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_ProductPricing_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TenantId_Name",
                schema: "dbo",
                table: "Categories",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyCategoryMaps_TenantId_CategoryId",
                schema: "migration",
                table: "LegacyCategoryMaps",
                columns: new[] { "TenantId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_TenantId_ProductId_SortOrder",
                schema: "dbo",
                table: "ProductMedia",
                columns: new[] { "TenantId", "ProductId", "SortOrder" });

            migrationBuilder.Sql("""
                INSERT INTO dbo.ProductPricing
                    (TenantId, ProductId, PurchaseRate, DealerRate, WholesaleRate, RetailRate, OtherRate,
                     VatRate, AdditionalVatRate, CstRate, IgstRate, SgstRate, CgstRate, UpdatedAtUtc)
                SELECT TenantId, Id, 0, COALESCE(DealerRate, 0), COALESCE(WholesaleRate, 0),
                       COALESCE(RetailRate, 0), 0, NULL, NULL, NULL, 0, 0, 0, SYSUTCDATETIME()
                FROM dbo.Products;

                INSERT INTO dbo.ProductMedia (TenantId, ProductId, LegacyPath, SortOrder, IsThumbnail)
                SELECT p.TenantId, p.Id, media.LegacyPath, media.SortOrder, media.IsThumbnail
                FROM dbo.Products p
                CROSS APPLY (VALUES
                    (p.Image1, 1, CAST(0 AS bit)), (p.Image2, 2, CAST(0 AS bit)),
                    (p.Image3, 3, CAST(0 AS bit)), (p.Image4, 4, CAST(0 AS bit)),
                    (p.Image5, 5, CAST(0 AS bit)), (p.Image6, 6, CAST(0 AS bit)),
                    (p.Image7, 7, CAST(0 AS bit)), (p.Image8, 8, CAST(0 AS bit)),
                    (p.Image9, 9, CAST(0 AS bit)), (p.ThumbnailImage, 0, CAST(1 AS bit))
                ) media(LegacyPath, SortOrder, IsThumbnail)
                WHERE NULLIF(LTRIM(RTRIM(media.LegacyPath)), '') IS NOT NULL;

                UPDATE dbo.Categories
                SET LegacyIconPath = Icon, LegacyNotificationImagePath = NotificationImage;

                UPDATE dbo.Products SET Remark = Specification;
                """);

            foreach (string column in new[] { "DealerRate", "WholesaleRate", "RetailRate", "Image1", "Image2", "Image3", "Image4", "Image5", "Image6", "Image7", "Image8", "Image9", "ThumbnailImage" })
            {
                migrationBuilder.DropColumn(name: column, schema: "dbo", table: "Products");
            }
            migrationBuilder.DropColumn(name: "Specification", schema: "dbo", table: "Products");
            migrationBuilder.DropColumn(name: "Icon", schema: "dbo", table: "Categories");
            migrationBuilder.DropColumn(name: "NotificationImage", schema: "dbo", table: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyCategoryMaps",
                schema: "migration");

            migrationBuilder.DropTable(
                name: "ProductInventory",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductMedia",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductPricing",
                schema: "dbo");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Products_TenantId_Id",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_TenantId_Name",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AlternateItemCode",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ChapterNumber",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Group1",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Group2",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HsnNumber",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ItemType",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Packing",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SalesmanCommission",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Unit",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitKilograms",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WarrantyMonths",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WarrantyYears",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "LegacyIconPath",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "LegacyNotificationImagePath",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                schema: "dbo",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "Remark",
                schema: "dbo",
                table: "Products",
                newName: "Specification");

            migrationBuilder.RenameColumn(
                name: "ManufacturerName",
                schema: "dbo",
                table: "Products",
                newName: "ThumbnailImage");

            migrationBuilder.RenameColumn(
                name: "Location",
                schema: "dbo",
                table: "Products",
                newName: "Image9");

            migrationBuilder.RenameColumn(
                name: "GujaratiName",
                schema: "dbo",
                table: "Products",
                newName: "Image8");

            migrationBuilder.RenameColumn(
                name: "AlternateLocation",
                schema: "dbo",
                table: "Products",
                newName: "Image7");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDisabled",
                schema: "dbo",
                table: "Products",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<decimal>(
                name: "DealerRate",
                schema: "dbo",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image1",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image2",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image3",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image4",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image5",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image6",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RetailRate",
                schema: "dbo",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WholesaleRate",
                schema: "dbo",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "Categories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<bool>(
                name: "DisplaySubCategory",
                schema: "dbo",
                table: "Categories",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                schema: "dbo",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationImage",
                schema: "dbo",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
