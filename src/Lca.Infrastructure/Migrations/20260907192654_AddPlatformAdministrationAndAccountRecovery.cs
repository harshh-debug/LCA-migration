using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF migration APIs require inline column-name arrays.

namespace Lca.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformAdministrationAndAccountRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresPasswordSetup",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PlatformAuditEntries",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformAuditEntries_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditEntries_Action_OccurredAtUtc",
                schema: "dbo",
                table: "PlatformAuditEntries",
                columns: new[] { "Action", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditEntries_ActorUserId",
                schema: "dbo",
                table: "PlatformAuditEntries",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditEntries_OccurredAtUtc",
                schema: "dbo",
                table: "PlatformAuditEntries",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditEntries_TenantId_OccurredAtUtc",
                schema: "dbo",
                table: "PlatformAuditEntries",
                columns: new[] { "TenantId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformAuditEntries",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "RequiresPasswordSetup",
                table: "AspNetUsers");
        }
    }
}
