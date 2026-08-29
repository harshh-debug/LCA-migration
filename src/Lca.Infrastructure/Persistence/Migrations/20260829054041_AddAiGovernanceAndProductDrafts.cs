using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF migration APIs require inline column-name arrays.

namespace Lca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiGovernanceAndProductDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Mobile_ItemMaster]', N'U') IS NULL
                    THROW 51000, 'Required legacy table dbo.Mobile_ItemMaster does not exist.', 1;
                IF OBJECT_ID(N'[dbo].[CategoryMastertbl]', N'U') IS NULL
                    THROW 51000, 'Required legacy table dbo.CategoryMastertbl does not exist.', 1;
                IF OBJECT_ID(N'[dbo].[AIImages]', N'U') IS NULL
                    THROW 51000, 'Required legacy table dbo.AIImages does not exist.', 1;
                IF EXISTS (SELECT 1 FROM [dbo].[AIImages])
                    THROW 51000, 'Profile and reconcile existing dbo.AIImages rows before applying the governance schema migration.', 1;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                schema: "dbo",
                table: "Mobile_ItemMaster",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CreatedSource",
                schema: "dbo",
                table: "Mobile_ItemMaster",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.RenameColumn(
                name: "ImageID",
                schema: "dbo",
                table: "AIImages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ProductID",
                schema: "dbo",
                table: "AIImages",
                newName: "ProductId");

            migrationBuilder.RenameColumn(
                name: "Image",
                schema: "dbo",
                table: "AIImages",
                newName: "ImageUrl");

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                schema: "dbo",
                table: "AIImages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "dbo",
                table: "AIImages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotPosition",
                schema: "dbo",
                table: "AIImages",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "dbo",
                table: "AIImages",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<string>(
                name: "AgentId",
                schema: "dbo",
                table: "AIImages",
                type: "varchar(50)",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                schema: "dbo",
                table: "AIImages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "dbo",
                table: "AIImages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuditLogId",
                schema: "dbo",
                table: "AIImages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprovalQueue",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(30)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DraftPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Pending"),
                    CreatedByAgent = table.Column<string>(type: "varchar(50)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalQueue", x => x.Id);
                    table.CheckConstraint("CK_ApprovalQueue_EntityType", "[EntityType] IN ('Product','Media','MarketingPost','LogisticsBooking')");
                    table.CheckConstraint("CK_ApprovalQueue_Status", "[Status] IN ('Pending','Approved','Rejected')");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AIImages_SlotPosition",
                schema: "dbo",
                table: "AIImages",
                sql: "[SlotPosition] BETWEEN 1 AND 9");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AIImages_Status",
                schema: "dbo",
                table: "AIImages",
                sql: "[Status] IN ('Draft','Approved','Rejected')");

            migrationBuilder.AddForeignKey(
                name: "FK_AIImages_Mobile_ItemMaster",
                schema: "dbo",
                table: "AIImages",
                column: "ProductId",
                principalSchema: "dbo",
                principalTable: "Mobile_ItemMaster",
                principalColumn: "ItemCode",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_AIImages_ProductId_SlotPosition_Status",
                schema: "dbo",
                table: "AIImages",
                columns: new[] { "ProductId", "SlotPosition", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalQueue_TenantId_Status_CreatedAt",
                schema: "dbo",
                table: "ApprovalQueue",
                columns: new[] { "TenantId", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalQueue",
                schema: "dbo");

            migrationBuilder.DropForeignKey(
                name: "FK_AIImages_Mobile_ItemMaster",
                schema: "dbo",
                table: "AIImages");
            migrationBuilder.DropCheckConstraint(name: "CK_AIImages_SlotPosition", schema: "dbo", table: "AIImages");
            migrationBuilder.DropCheckConstraint(name: "CK_AIImages_Status", schema: "dbo", table: "AIImages");
            migrationBuilder.DropIndex(name: "IX_AIImages_ProductId_SlotPosition_Status", schema: "dbo", table: "AIImages");
            migrationBuilder.DropColumn(name: "SlotPosition", schema: "dbo", table: "AIImages");
            migrationBuilder.DropColumn(name: "Status", schema: "dbo", table: "AIImages");
            migrationBuilder.DropColumn(name: "AgentId", schema: "dbo", table: "AIImages");
            migrationBuilder.DropColumn(name: "ApprovedBy", schema: "dbo", table: "AIImages");
            migrationBuilder.DropColumn(name: "ApprovedAt", schema: "dbo", table: "AIImages");
            migrationBuilder.DropColumn(name: "AuditLogId", schema: "dbo", table: "AIImages");
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                schema: "dbo",
                table: "AIImages",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "dbo",
                table: "AIImages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
            migrationBuilder.RenameColumn(name: "Id", schema: "dbo", table: "AIImages", newName: "ImageID");
            migrationBuilder.RenameColumn(name: "ProductId", schema: "dbo", table: "AIImages", newName: "ProductID");
            migrationBuilder.RenameColumn(name: "ImageUrl", schema: "dbo", table: "AIImages", newName: "Image");
            migrationBuilder.DropColumn(name: "IsDraft", schema: "dbo", table: "Mobile_ItemMaster");
            migrationBuilder.DropColumn(name: "CreatedSource", schema: "dbo", table: "Mobile_ItemMaster");
        }
    }
}
#pragma warning restore CA1861
