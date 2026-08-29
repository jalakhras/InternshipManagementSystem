using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Add_Exam_Sections_And_Named_Forms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExamSectionId",
                table: "AppQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExamSectionId",
                table: "AppQuestionGroups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryMode",
                table: "AppExams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "FixedFormId",
                table: "AppExams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExamSectionId",
                table: "AppExamBlueprintRules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppExamForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WasGenerated = table.Column<bool>(type: "bit", nullable: false),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExamForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExamForms_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppExamSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeLimitInMinutes = table.Column<int>(type: "int", nullable: true),
                    MinimumPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    QuestionsPerForm = table.Column<int>(type: "int", nullable: true),
                    IsQualifying = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExamSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExamSections_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppExamFormQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExamFormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExamFormQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExamFormQuestions_AppExamForms_ExamFormId",
                        column: x => x.ExamFormId,
                        principalTable: "AppExamForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppQuestions_ExamSectionId",
                table: "AppQuestions",
                column: "ExamSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuestionGroups_ExamSectionId",
                table: "AppQuestionGroups",
                column: "ExamSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamFormQuestions_ExamFormId_DisplayOrder",
                table: "AppExamFormQuestions",
                columns: new[] { "ExamFormId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AppExamFormQuestions_ExamFormId_QuestionId",
                table: "AppExamFormQuestions",
                columns: new[] { "ExamFormId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppExamForms_ExamId_Code",
                table: "AppExamForms",
                columns: new[] { "ExamId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppExamForms_ExamId_Status",
                table: "AppExamForms",
                columns: new[] { "ExamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppExamSections_ExamId_DisplayOrder",
                table: "AppExamSections",
                columns: new[] { "ExamId", "DisplayOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_AppQuestionGroups_AppExamSections_ExamSectionId",
                table: "AppQuestionGroups",
                column: "ExamSectionId",
                principalTable: "AppExamSections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppQuestionGroups_AppExamSections_ExamSectionId",
                table: "AppQuestionGroups");

            migrationBuilder.DropTable(
                name: "AppExamFormQuestions");

            migrationBuilder.DropTable(
                name: "AppExamSections");

            migrationBuilder.DropTable(
                name: "AppExamForms");

            migrationBuilder.DropIndex(
                name: "IX_AppQuestions_ExamSectionId",
                table: "AppQuestions");

            migrationBuilder.DropIndex(
                name: "IX_AppQuestionGroups_ExamSectionId",
                table: "AppQuestionGroups");

            migrationBuilder.DropColumn(
                name: "ExamSectionId",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "ExamSectionId",
                table: "AppQuestionGroups");

            migrationBuilder.DropColumn(
                name: "DeliveryMode",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "FixedFormId",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "ExamSectionId",
                table: "AppExamBlueprintRules");
        }
    }
}
