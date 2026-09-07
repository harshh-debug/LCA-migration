using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF migration APIs require inline column-name arrays.

namespace Lca.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleLegacySourcesPerCustomerContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LegacyCustomerContactMaps_TenantId_CustomerContactId",
                schema: "migration",
                table: "LegacyCustomerContactMaps");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyCustomerContactMaps_TenantId_CustomerContactId",
                schema: "migration",
                table: "LegacyCustomerContactMaps",
                columns: new[] { "TenantId", "CustomerContactId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LegacyCustomerContactMaps_TenantId_CustomerContactId",
                schema: "migration",
                table: "LegacyCustomerContactMaps");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyCustomerContactMaps_TenantId_CustomerContactId",
                schema: "migration",
                table: "LegacyCustomerContactMaps",
                columns: new[] { "TenantId", "CustomerContactId" },
                unique: true);
        }
    }
}
#pragma warning restore CA1861
