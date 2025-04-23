using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Added_TrainingManagement_Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSpecializations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "اسم التخصص"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSpecializations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppExams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false, comment: "عنوان الاختبار"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "وصف الاختبار"),
                    SpecializationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "التخصص المرتبط بالاختبار"),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "مستوى الاختبار (مبتدئ/متوسط/متقدم)"),
                    TimeLimitInMinutes = table.Column<int>(type: "int", nullable: false, comment: "الوقت المحدد بالدقائق للامتحان"),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false, comment: "عدد الأسئلة بالاختبار"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, comment: "هل الاختبار مفعل؟"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExams_AppSpecializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalTable: "AppSpecializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTrainees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "اسم المتدرب الكامل"),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, comment: "الرقم الوظيفي للمتدرب"),
                    SpecializationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "معرّف التخصص المرتبط بالمتدرب"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTrainees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTrainees_AppSpecializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalTable: "AppSpecializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false, comment: "نص السؤال"),
                    Type = table.Column<int>(type: "int", nullable: false, comment: "نوع السؤال (اختياري/صح وخطأ/نصي)"),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "خيارات السؤال (مخزنة كـ JSON)"),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false, comment: "الإجابة الصحيحة للسؤال"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQuestions_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraineeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "وقت بدء الامتحان"),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "وقت إنهاء الامتحان"),
                    Score = table.Column<double>(type: "float", nullable: false, comment: "نتيجة الامتحان"),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false, comment: "هل المتدرب نجح بالامتحان؟"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExamAttempts_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppExamAttempts_AppTrainees_TraineeId",
                        column: x => x.TraineeId,
                        principalTable: "AppTrainees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppExamAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "إجابة المتدرب للسؤال"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExamAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExamAnswers_AppExamAttempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "AppExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppExamAnswers_AppQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "AppQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppExamAnswers_ExamAttemptId",
                table: "AppExamAnswers",
                column: "ExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamAnswers_QuestionId",
                table: "AppExamAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamAttempts_ExamId",
                table: "AppExamAttempts",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamAttempts_TraineeId",
                table: "AppExamAttempts",
                column: "TraineeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExams_SpecializationId",
                table: "AppExams",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuestions_ExamId",
                table: "AppQuestions",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTrainees_SpecializationId",
                table: "AppTrainees",
                column: "SpecializationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppExamAnswers");

            migrationBuilder.DropTable(
                name: "AppExamAttempts");

            migrationBuilder.DropTable(
                name: "AppQuestions");

            migrationBuilder.DropTable(
                name: "AppTrainees");

            migrationBuilder.DropTable(
                name: "AppExams");

            migrationBuilder.DropTable(
                name: "AppSpecializations");
        }
    }
}