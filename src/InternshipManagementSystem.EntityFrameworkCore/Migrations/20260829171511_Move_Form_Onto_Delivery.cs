using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Move_Form_Onto_Delivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCandidateGroupForms");

            migrationBuilder.AddColumn<Guid>(
                name: "ExamFormId",
                table: "AppAttempts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExamFormId",
                table: "AppAssignments",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExamFormId",
                table: "AppAttempts");

            migrationBuilder.DropColumn(
                name: "ExamFormId",
                table: "AppAssignments");

            migrationBuilder.CreateTable(
                name: "AppCandidateGroupForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExamFormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    SittingOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCandidateGroupForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCandidateGroupForms_AppCandidateGroups_CandidateGroupId",
                        column: x => x.CandidateGroupId,
                        principalTable: "AppCandidateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateGroupForms_CandidateGroupId_ExamFormId",
                table: "AppCandidateGroupForms",
                columns: new[] { "CandidateGroupId", "ExamFormId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateGroupForms_CandidateGroupId_Sequence",
                table: "AppCandidateGroupForms",
                columns: new[] { "CandidateGroupId", "Sequence" },
                unique: true);
        }
    }
}
