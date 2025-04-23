using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Added_CandidateExamAnswer_Entity_Relation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCandidateExamAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateExamAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, comment: "الإجابة النصية للمرشح"),
                    AnswerFileUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true, comment: "رابط الملف المرفق للإجابة"),
                    AnswerFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "اسم الملف المرفق"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCandidateExamAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCandidateExamAnswers_AppCandidateExamAttempts_CandidateExamAttemptId",
                        column: x => x.CandidateExamAttemptId,
                        principalTable: "AppCandidateExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppCandidateExamAnswers_AppQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "AppQuestions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateExamAnswers_CandidateExamAttemptId",
                table: "AppCandidateExamAnswers",
                column: "CandidateExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateExamAnswers_QuestionId",
                table: "AppCandidateExamAnswers",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCandidateExamAnswers");
        }
    }
}