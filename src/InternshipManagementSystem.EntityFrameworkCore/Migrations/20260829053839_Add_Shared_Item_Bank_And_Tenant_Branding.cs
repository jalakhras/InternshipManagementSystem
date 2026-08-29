using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Add_Shared_Item_Bank_And_Tenant_Branding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "AppTopics",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ExamId",
                table: "AppQuestions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "AppQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LevelId",
                table: "AppQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimesServed",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "AppLevels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppTenantBranding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayNameAlternate = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LogoBlobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IconBlobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    CertificateFooter = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTenantBranding", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTopics_TenantId_CategoryId",
                table: "AppTopics",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppQuestions_TenantId_CategoryId_LevelId_TopicId_Difficulty",
                table: "AppQuestions",
                columns: new[] { "TenantId", "CategoryId", "LevelId", "TopicId", "Difficulty" },
                filter: "[ExamId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppLevels_TenantId_CategoryId",
                table: "AppLevels",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTenantBranding_TenantId",
                table: "AppTenantBranding",
                column: "TenantId",
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTenantBranding");

            migrationBuilder.DropIndex(
                name: "IX_AppTopics_TenantId_CategoryId",
                table: "AppTopics");

            migrationBuilder.DropIndex(
                name: "IX_AppQuestions_TenantId_CategoryId_LevelId_TopicId_Difficulty",
                table: "AppQuestions");

            migrationBuilder.DropIndex(
                name: "IX_AppLevels_TenantId_CategoryId",
                table: "AppLevels");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AppTopics");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "LevelId",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "TimesServed",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AppLevels");

            migrationBuilder.AlterColumn<Guid>(
                name: "ExamId",
                table: "AppQuestions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
