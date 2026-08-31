using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <summary>
    /// A second store for the organisation's identity that nothing ever wrote to.
    /// <para>
    /// Its name, logo and brand colour live in the settings store, and that store
    /// is the live one: the shell reads it, so does the screen a candidate opens
    /// their link on, and so does the invitation email. This table held the same
    /// three fields again — plus four for features that do not exist — and no
    /// code path in the product ever constructed a row. Two stores for one fact
    /// is how the two drift apart, and the drift is only ever discovered by
    /// somebody who changed their logo and saw the old one.
    /// </para>
    /// <para>
    /// Dropping it can lose nothing, and that is checked rather than assumed:
    /// there is no <c>new TenantBranding(...)</c> anywhere in the solution, so
    /// the table cannot contain a row on any deployment. The four extra fields —
    /// an alternate name, an icon, a certificate footer, a support address —
    /// describe things the product does not do yet. When it does them, they
    /// belong beside the three that already work, not in a table of their own.
    /// </para>
    /// </summary>
    public partial class Drop_Dead_TenantBranding_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTenantBranding");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTenantBranding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificateFooter = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayNameAlternate = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconBlobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LogoBlobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTenantBranding", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTenantBranding_TenantId",
                table: "AppTenantBranding",
                column: "TenantId",
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
