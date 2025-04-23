using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Added_Candidate_Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "اسم المرشح الكامل"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false, comment: "البريد الإلكتروني للمرشح"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "رقم هاتف المرشح"),
                    PositionAppliedFor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "الوظيفة المتقدم لها المرشح"),
                    Status = table.Column<int>(type: "int", nullable: false, comment: "حالة المرشح (قيد التقييم / ناجح / راسب)"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCandidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCandidateExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "وقت بدء محاولة الامتحان"),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "وقت انتهاء محاولة الامتحان"),
                    Score = table.Column<double>(type: "float", nullable: false, comment: "نتيجة الامتحان"),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false, comment: "هل اجتاز المرشح الامتحان بنجاح"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCandidateExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCandidateExamAttempts_AppCandidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "AppCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppCandidateExamAttempts_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateExamAttempts_CandidateId",
                table: "AppCandidateExamAttempts",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateExamAttempts_ExamId",
                table: "AppCandidateExamAttempts",
                column: "ExamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCandidateExamAttempts");

            migrationBuilder.DropTable(
                name: "AppCandidates");
        }
    }
}