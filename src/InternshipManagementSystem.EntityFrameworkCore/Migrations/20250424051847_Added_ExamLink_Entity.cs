using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Added_ExamLink_Entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppExamLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecureToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "الرمز السري الفريد للوصول للرابط"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "تاريخ انتهاء صلاحية الرابط"),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false, comment: "عدد المحاولات المسموح بها"),
                    CurrentAttempts = table.Column<int>(type: "int", nullable: false, comment: "عدد المحاولات التي تم استخدامها"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExamLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExamLinks_AppCandidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "AppCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppExamLinks_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppExamLinks_CandidateId",
                table: "AppExamLinks",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamLinks_ExamId",
                table: "AppExamLinks",
                column: "ExamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppExamLinks");
        }
    }
}
