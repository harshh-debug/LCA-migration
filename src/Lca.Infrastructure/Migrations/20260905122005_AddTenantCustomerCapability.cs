using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF migration APIs require inline column-name arrays.

namespace Lca.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantCustomerCapability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OfficePhone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResidentialPhone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagementContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagementMobileNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagementEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AddressLine3 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    City = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    District = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    State = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultShippingAddressLine1 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultShippingAddressLine2 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultShippingAddressLine3 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Gstin = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TinNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CstNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PriceBand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreditDays = table.Column<int>(type: "int", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DefaultTransportName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SalespersonReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.UniqueConstraint("AK_Customers_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Customers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "dbo",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContactChanges",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerContactId = table.Column<long>(type: "bigint", nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMobileChanged = table.Column<bool>(type: "bit", nullable: false),
                    ReviewStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContactChanges", x => x.Id);
                    table.UniqueConstraint("AK_CustomerContactChanges_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CustomerContactChanges_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                    table.UniqueConstraint("AK_CustomerContacts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CustomerContacts_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegacyCustomerAccountingSnapshots",
                schema: "migration",
                columns: table => new
                {
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    DueBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BlockLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PreviousBlockLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LegacyUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyCustomerAccountingSnapshots", x => new { x.TenantId, x.CustomerId });
                    table.ForeignKey(
                        name: "FK_LegacyCustomerAccountingSnapshots_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegacyCustomerMaps",
                schema: "migration",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    LegacyCustomerId = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyCustomerMaps", x => new { x.TenantId, x.LegacyCustomerId });
                    table.ForeignKey(
                        name: "FK_LegacyCustomerMaps_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegacyCustomerContactChangeMaps",
                schema: "migration",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerContactChangeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyCustomerContactChangeMaps", x => new { x.TenantId, x.SourceFingerprint });
                    table.ForeignKey(
                        name: "FK_LegacyCustomerContactChangeMaps_CustomerContactChanges_TenantId_CustomerContactChangeId",
                        columns: x => new { x.TenantId, x.CustomerContactChangeId },
                        principalSchema: "dbo",
                        principalTable: "CustomerContactChanges",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegacyCustomerContactMaps",
                schema: "migration",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerContactId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyCustomerContactMaps", x => new { x.TenantId, x.SourceFingerprint });
                    table.ForeignKey(
                        name: "FK_LegacyCustomerContactMaps_CustomerContacts_TenantId_CustomerContactId",
                        columns: x => new { x.TenantId, x.CustomerContactId },
                        principalSchema: "dbo",
                        principalTable: "CustomerContacts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContactChanges_TenantId_CustomerId_ChangedAtUtc",
                schema: "dbo",
                table: "CustomerContactChanges",
                columns: new[] { "TenantId", "CustomerId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContactChanges_TenantId_ReviewStatus_ChangedAtUtc",
                schema: "dbo",
                table: "CustomerContactChanges",
                columns: new[] { "TenantId", "ReviewStatus", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_TenantId_CustomerId",
                schema: "dbo",
                table: "CustomerContacts",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_AccountNumber",
                schema: "dbo",
                table: "Customers",
                columns: new[] { "TenantId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Area",
                schema: "dbo",
                table: "Customers",
                columns: new[] { "TenantId", "Area" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_IsDisabled_AccountNumber",
                schema: "dbo",
                table: "Customers",
                columns: new[] { "TenantId", "IsDisabled", "AccountNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_PriceBand",
                schema: "dbo",
                table: "Customers",
                columns: new[] { "TenantId", "PriceBand" });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyCustomerContactChangeMaps_TenantId_CustomerContactChangeId",
                schema: "migration",
                table: "LegacyCustomerContactChangeMaps",
                columns: new[] { "TenantId", "CustomerContactChangeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyCustomerContactMaps_TenantId_CustomerContactId",
                schema: "migration",
                table: "LegacyCustomerContactMaps",
                columns: new[] { "TenantId", "CustomerContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyCustomerMaps_TenantId_CustomerId",
                schema: "migration",
                table: "LegacyCustomerMaps",
                columns: new[] { "TenantId", "CustomerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyCustomerAccountingSnapshots",
                schema: "migration");

            migrationBuilder.DropTable(
                name: "LegacyCustomerContactChangeMaps",
                schema: "migration");

            migrationBuilder.DropTable(
                name: "LegacyCustomerContactMaps",
                schema: "migration");

            migrationBuilder.DropTable(
                name: "LegacyCustomerMaps",
                schema: "migration");

            migrationBuilder.DropTable(
                name: "CustomerContactChanges",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CustomerContacts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "dbo");
        }
    }
}
#pragma warning restore CA1861
