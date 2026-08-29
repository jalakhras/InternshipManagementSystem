using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Assessment_Platform_Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppExamLinks_AppCandidates_CandidateId",
                table: "AppExamLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_AppExamLinks_AppExams_ExamId",
                table: "AppExamLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_AppExams_AppSpecializations_SpecializationId",
                table: "AppExams");

            migrationBuilder.DropTable(
                name: "AppCandidateExamAnswers");

            migrationBuilder.DropTable(
                name: "AppExamAnswers");

            migrationBuilder.DropTable(
                name: "AppCandidateExamAttempts");

            migrationBuilder.DropTable(
                name: "AppExamAttempts");

            migrationBuilder.DropTable(
                name: "AppTrainees");

            migrationBuilder.DropTable(
                name: "AppSpecializations");

            migrationBuilder.DropIndex(
                name: "IX_AppExams_SpecializationId",
                table: "AppExams");

            migrationBuilder.DropIndex(
                name: "IX_AppExamLinks_CandidateId",
                table: "AppExamLinks");

            migrationBuilder.DropIndex(
                name: "IX_AppExamLinks_ExamId",
                table: "AppExamLinks");

            migrationBuilder.DropIndex(
                name: "IX_AppExamLinks_SecureToken",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "AllowPartialCredit",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "CodeExpectedOutput",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "CodeLanguage",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "CodeStarterTemplate",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "CorrectAnswer",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "AllowQuestionTimeLimit",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "SpecializationId",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "TotalQuestions",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "CurrentAttempts",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "SecureToken",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "PositionAppliedFor",
                table: "AppCandidates");

            migrationBuilder.RenameColumn(
                name: "MediaFileName",
                table: "AppQuestions",
                newName: "Explanation");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "AppQuestions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "نوع السؤال");

            migrationBuilder.AlterColumn<int>(
                name: "TimeLimitInSeconds",
                table: "AppQuestions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "الحد الزمني لهذا السؤال بالثواني (اختياري)");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "AppQuestions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldComment: "نص السؤال");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "AppQuestions",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float",
                oldComment: "عدد النقاط المخصصة لهذا السؤال");

            migrationBuilder.AlterColumn<string>(
                name: "MediaType",
                table: "AppQuestions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "نوع الوسائط المرتبطة بالسؤال (صورة، صوت، فيديو، مستند)");

            migrationBuilder.AddColumn<byte>(
                name: "Difficulty",
                table: "AppQuestions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<decimal>(
                name: "DifficultyIndex",
                table: "AppQuestions",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscriminationIndex",
                table: "AppQuestions",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MediaBlobName",
                table: "AppQuestions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "AppQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionGroupId",
                table: "AppQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimesAnswered",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TopicId",
                table: "AppQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AppExams",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldComment: "عنوان الامتحان");

            migrationBuilder.AlterColumn<int>(
                name: "TimeLimitInMinutes",
                table: "AppExams",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "المدة الإجمالية المسموح بها لحل الامتحان");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppExams",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "وصف مختصر للامتحان");

            migrationBuilder.AddColumn<bool>(
                name: "AllowBackNavigation",
                table: "AppExams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "AppExams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CollectIntegritySignals",
                table: "AppExams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LevelId",
                table: "AppExams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Mode",
                table: "AppExams",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "OneQuestionAtATime",
                table: "AppExams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PassingPercentage",
                table: "AppExams",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QuestionsPerForm",
                table: "AppExams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleOptions",
                table: "AppExams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleQuestions",
                table: "AppExams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "AppExams",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppExams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxAttempts",
                table: "AppExamLinks",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "عدد المحاولات المسموح بها");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignmentId",
                table: "AppExamLinks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "AttemptsUsed",
                table: "AppExamLinks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSentAt",
                table: "AppExamLinks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "AppExamLinks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstOpenedAt",
                table: "AppExamLinks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "AppExamLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "AppExamLinks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppExamLinks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "AppExamLinks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenPrefix",
                table: "AppExamLinks",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "AppCandidates",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "حالة المرشح (قيد التقييم / ناجح / راسب)");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AppCandidates",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldComment: "رقم هاتف المرشح");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AppCandidates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldComment: "اسم المرشح الكامل");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppCandidates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldComment: "البريد الإلكتروني للمرشح");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "AppCandidates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "AppCandidates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppCandidates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AppCandidates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CandidateGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    SendEmail = table.Column<bool>(type: "bit", nullable: false),
                    LinkCount = table.Column<int>(type: "int", nullable: false),
                    EmailsSent = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndReason = table.Column<byte>(type: "tinyint", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    IsGraded = table.Column<bool>(type: "bit", nullable: false),
                    NeedsManualReview = table.Column<bool>(type: "bit", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    ScorePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    ShuffleSeed = table.Column<int>(type: "int", nullable: false),
                    IntegrityFlagCount = table.Column<int>(type: "int", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCandidateGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCandidateGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCategorySets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SingularName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PluralName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubjectSingularName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubjectPluralName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GroupSingularName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GroupPluralName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCategorySets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppExamBlueprintRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Difficulty = table.Column<byte>(type: "tinyint", nullable: true),
                    QuestionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppExamBlueprintRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExamBlueprintRules_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppIntegritySignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Magnitude = table.Column<int>(type: "int", nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppIntegritySignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppQuestionGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    StimulusText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StimulusBlobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StimulusMediaType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
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
                    table.PrimaryKey("PK_AppQuestionGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQuestionGroups_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnswerBlobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AnswerFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    AwardedScore = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    NeedsManualReview = table.Column<bool>(type: "bit", nullable: false),
                    ReviewComment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RubricScores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeSpentSeconds = table.Column<int>(type: "int", nullable: true),
                    WasPasted = table.Column<bool>(type: "bit", nullable: false),
                    KeystrokeCount = table.Column<int>(type: "int", nullable: false),
                    BackspaceCount = table.Column<int>(type: "int", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppAnswers_AppAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "AppAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppAttemptQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    OptionOrder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAttemptQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppAttemptQuestions_AppAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "AppAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppCandidateGroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CandidateGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCandidateGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCandidateGroupMembers_AppCandidateGroups_CandidateGroupId",
                        column: x => x.CandidateGroupId,
                        principalTable: "AppCandidateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppCandidateGroupMembers_AppCandidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "AppCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppQuestions_ExamId_TopicId_Difficulty",
                table: "AppQuestions",
                columns: new[] { "ExamId", "TopicId", "Difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_AppQuestions_QuestionGroupId",
                table: "AppQuestions",
                column: "QuestionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExams_TenantId_CategoryId_LevelId",
                table: "AppExams",
                columns: new[] { "TenantId", "CategoryId", "LevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppExams_TenantId_Status",
                table: "AppExams",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppExamLinks_AssignmentId",
                table: "AppExamLinks",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamLinks_TenantId_CandidateId",
                table: "AppExamLinks",
                columns: new[] { "TenantId", "CandidateId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppExamLinks_TokenHash",
                table: "AppExamLinks",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidates_TenantId_CategoryId",
                table: "AppCandidates",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidates_TenantId_Email",
                table: "AppCandidates",
                columns: new[] { "TenantId", "Email" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppAnswers_AttemptId_QuestionId",
                table: "AppAnswers",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAnswers_TenantId_NeedsManualReview",
                table: "AppAnswers",
                columns: new[] { "TenantId", "NeedsManualReview" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAssignments_TenantId_ExamId",
                table: "AppAssignments",
                columns: new[] { "TenantId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAttemptQuestions_AttemptId_Position",
                table: "AppAttemptQuestions",
                columns: new[] { "AttemptId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAttemptQuestions_AttemptId_QuestionId",
                table: "AppAttemptQuestions",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAttempts_IsSubmitted_DeadlineAt",
                table: "AppAttempts",
                columns: new[] { "IsSubmitted", "DeadlineAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAttempts_TenantId_CandidateId",
                table: "AppAttempts",
                columns: new[] { "TenantId", "CandidateId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAttempts_TenantId_ExamId",
                table: "AppAttempts",
                columns: new[] { "TenantId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAttempts_TenantId_NeedsManualReview_IsSubmitted",
                table: "AppAttempts",
                columns: new[] { "TenantId", "NeedsManualReview", "IsSubmitted" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateGroupMembers_CandidateGroupId_CandidateId",
                table: "AppCandidateGroupMembers",
                columns: new[] { "CandidateGroupId", "CandidateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateGroupMembers_CandidateId",
                table: "AppCandidateGroupMembers",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateGroups_TenantId_CategoryId",
                table: "AppCandidateGroups",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCategories_TenantId_Code",
                table: "AppCategories",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppCategorySets_TenantId",
                table: "AppCategorySets",
                column: "TenantId",
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamBlueprintRules_ExamId",
                table: "AppExamBlueprintRules",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIntegritySignals_AttemptId_Type",
                table: "AppIntegritySignals",
                columns: new[] { "AttemptId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_AppLevels_TenantId_Code",
                table: "AppLevels",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuestionGroups_ExamId",
                table: "AppQuestionGroups",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTopics_ParentId",
                table: "AppTopics",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTopics_TenantId_Code",
                table: "AppTopics",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AppQuestions_AppQuestionGroups_QuestionGroupId",
                table: "AppQuestions",
                column: "QuestionGroupId",
                principalTable: "AppQuestionGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppQuestions_AppQuestionGroups_QuestionGroupId",
                table: "AppQuestions");

            migrationBuilder.DropTable(
                name: "AppAnswers");

            migrationBuilder.DropTable(
                name: "AppAssignments");

            migrationBuilder.DropTable(
                name: "AppAttemptQuestions");

            migrationBuilder.DropTable(
                name: "AppCandidateGroupMembers");

            migrationBuilder.DropTable(
                name: "AppCategories");

            migrationBuilder.DropTable(
                name: "AppCategorySets");

            migrationBuilder.DropTable(
                name: "AppExamBlueprintRules");

            migrationBuilder.DropTable(
                name: "AppIntegritySignals");

            migrationBuilder.DropTable(
                name: "AppLevels");

            migrationBuilder.DropTable(
                name: "AppQuestionGroups");

            migrationBuilder.DropTable(
                name: "AppTopics");

            migrationBuilder.DropTable(
                name: "AppAttempts");

            migrationBuilder.DropTable(
                name: "AppCandidateGroups");

            migrationBuilder.DropIndex(
                name: "IX_AppQuestions_ExamId_TopicId_Difficulty",
                table: "AppQuestions");

            migrationBuilder.DropIndex(
                name: "IX_AppQuestions_QuestionGroupId",
                table: "AppQuestions");

            migrationBuilder.DropIndex(
                name: "IX_AppExams_TenantId_CategoryId_LevelId",
                table: "AppExams");

            migrationBuilder.DropIndex(
                name: "IX_AppExams_TenantId_Status",
                table: "AppExams");

            migrationBuilder.DropIndex(
                name: "IX_AppExamLinks_AssignmentId",
                table: "AppExamLinks");

            migrationBuilder.DropIndex(
                name: "IX_AppExamLinks_TenantId_CandidateId",
                table: "AppExamLinks");

            migrationBuilder.DropIndex(
                name: "IX_AppExamLinks_TokenHash",
                table: "AppExamLinks");

            migrationBuilder.DropIndex(
                name: "IX_AppCandidates_TenantId_CategoryId",
                table: "AppCandidates");

            migrationBuilder.DropIndex(
                name: "IX_AppCandidates_TenantId_Email",
                table: "AppCandidates");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "DifficultyIndex",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "DiscriminationIndex",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "MediaBlobName",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "QuestionGroupId",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "TimesAnswered",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "AllowBackNavigation",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "CollectIntegritySignals",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "LevelId",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "OneQuestionAtATime",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "PassingPercentage",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "QuestionsPerForm",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "ShuffleOptions",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "ShuffleQuestions",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppExams");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "AttemptsUsed",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "EmailSentAt",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "FirstOpenedAt",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "TokenPrefix",
                table: "AppExamLinks");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AppCandidates");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "AppCandidates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppCandidates");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AppCandidates");

            migrationBuilder.RenameColumn(
                name: "Explanation",
                table: "AppQuestions",
                newName: "MediaFileName");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                comment: "نوع السؤال",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<int>(
                name: "TimeLimitInSeconds",
                table: "AppQuestions",
                type: "int",
                nullable: true,
                comment: "الحد الزمني لهذا السؤال بالثواني (اختياري)",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "AppQuestions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                comment: "نص السؤال",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<double>(
                name: "Score",
                table: "AppQuestions",
                type: "float",
                nullable: false,
                comment: "عدد النقاط المخصصة لهذا السؤال",
                oldClrType: typeof(decimal),
                oldType: "decimal(9,2)",
                oldPrecision: 9,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "MediaType",
                table: "AppQuestions",
                type: "int",
                nullable: true,
                comment: "نوع الوسائط المرتبطة بالسؤال (صورة، صوت، فيديو، مستند)",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPartialCredit",
                table: "AppQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "السماح بالحصول على درجات جزئية للأسئلة متعددة الخيارات");

            migrationBuilder.AddColumn<string>(
                name: "CodeExpectedOutput",
                table: "AppQuestions",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true,
                comment: "المخرجات المتوقعة من تنفيذ الكود");

            migrationBuilder.AddColumn<string>(
                name: "CodeLanguage",
                table: "AppQuestions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "لغة البرمجة المطلوبة");

            migrationBuilder.AddColumn<string>(
                name: "CodeStarterTemplate",
                table: "AppQuestions",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true,
                comment: "نص الكود الابتدائي الذي يظهر للطالب (Code Starter)");

            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswer",
                table: "AppQuestions",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                comment: "الإجابة الصحيحة للسؤال");

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "AppQuestions",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                comment: "رابط الوسائط (صورة/صوت/فيديو/مستند) المرتبطة بالسؤال");

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "AppQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                comment: "خيارات السؤال بصيغة JSON");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AppExams",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                comment: "عنوان الامتحان",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<int>(
                name: "TimeLimitInMinutes",
                table: "AppExams",
                type: "int",
                nullable: false,
                comment: "المدة الإجمالية المسموح بها لحل الامتحان",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppExams",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                comment: "وصف مختصر للامتحان",
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowQuestionTimeLimit",
                table: "AppExams",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "هل يُسمح بتحديد وقت لكل سؤال بشكل مستقل؟");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppExams",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "هل الامتحان مفعل أم لا");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "AppExams",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                comment: "مستوى الامتحان (مبتدئ/متوسط/متقدم)");

            migrationBuilder.AddColumn<Guid>(
                name: "SpecializationId",
                table: "AppExams",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "التخصص المرتبط بالامتحان");

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestions",
                table: "AppExams",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "عدد الأسئلة الكلي");

            migrationBuilder.AlterColumn<int>(
                name: "MaxAttempts",
                table: "AppExamLinks",
                type: "int",
                nullable: false,
                comment: "عدد المحاولات المسموح بها",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CurrentAttempts",
                table: "AppExamLinks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "عدد المحاولات التي تم استخدامها");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "AppExamLinks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "تاريخ انتهاء صلاحية الرابط");

            migrationBuilder.AddColumn<string>(
                name: "SecureToken",
                table: "AppExamLinks",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                comment: "الرمز السري الفريد للوصول للرابط");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AppCandidates",
                type: "int",
                nullable: false,
                comment: "حالة المرشح (قيد التقييم / ناجح / راسب)",
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AppCandidates",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                comment: "رقم هاتف المرشح",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AppCandidates",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                comment: "اسم المرشح الكامل",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppCandidates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                comment: "البريد الإلكتروني للمرشح",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<string>(
                name: "PositionAppliedFor",
                table: "AppCandidates",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                comment: "الوظيفة المتقدم لها المرشح");

            migrationBuilder.CreateTable(
                name: "AppCandidateExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "وقت انتهاء محاولة الامتحان"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false, comment: "هل اجتاز المرشح الامتحان بنجاح"),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NeedsManualReview = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "هل تحتوي المحاولة على أسئلة تحتاج مراجعة يدوية"),
                    Score = table.Column<double>(type: "float", nullable: false, comment: "نتيجة الامتحان"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "وقت بدء محاولة الامتحان")
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

            migrationBuilder.CreateTable(
                name: "AppSpecializations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "اسم التخصص")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSpecializations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCandidateExamAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateExamAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, comment: "الإجابة النصية للمرشح"),
                    AnswerFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "اسم الملف المرفق"),
                    AnswerFileUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true, comment: "رابط الملف المرفق للإجابة"),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartialScore = table.Column<double>(type: "float", nullable: true),
                    ReviewComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "AppTrainees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "معرّف التخصص المرتبط بالمتدرب"),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, comment: "الرقم الوظيفي للمتدرب"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "اسم المتدرب الكامل"),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "معرّف المستخدم المرتبط بالمتدرب (اختياري)")
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
                name: "AppExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraineeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "وقت إنهاء الامتحان"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsGraded = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "هل تم تصحيح المحاولة تلقائيًا أو يدويًا"),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false, comment: "هل المتدرب نجح بالامتحان؟"),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "هل تم ارسال الاجابات"),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NeedsManualReview = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "هل تحتوي المحاولة على أسئلة تحتاج مراجعة يدوية"),
                    Score = table.Column<double>(type: "float", nullable: false, comment: "نتيجة الامتحان"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "وقت بدء الامتحان")
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
                    AnswerFileName = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true, comment: "مرفق الاجابه للإجابة"),
                    AnswerFileUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true, comment: "رابط مرفق الاجابه للإجابة"),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true, comment: "هل الإجابة صحيحة (مراجعة تلقائية أو يدوية)"),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartialScore = table.Column<double>(type: "float", nullable: true, comment: "الدرجة الجزئية لهذا الجواب إن وجدت"),
                    ReviewComments = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false, comment: "ملاحظات المدقق اليدوي للإجابة")
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
                name: "IX_AppExams_SpecializationId",
                table: "AppExams",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamLinks_CandidateId",
                table: "AppExamLinks",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamLinks_ExamId",
                table: "AppExamLinks",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamLinks_SecureToken",
                table: "AppExamLinks",
                column: "SecureToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateExamAnswers_CandidateExamAttemptId",
                table: "AppCandidateExamAnswers",
                column: "CandidateExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateExamAnswers_QuestionId",
                table: "AppCandidateExamAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateExamAttempts_CandidateId",
                table: "AppCandidateExamAttempts",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidateExamAttempts_ExamId",
                table: "AppCandidateExamAttempts",
                column: "ExamId");

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
                name: "IX_AppTrainees_SpecializationId",
                table: "AppTrainees",
                column: "SpecializationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppExamLinks_AppCandidates_CandidateId",
                table: "AppExamLinks",
                column: "CandidateId",
                principalTable: "AppCandidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppExamLinks_AppExams_ExamId",
                table: "AppExamLinks",
                column: "ExamId",
                principalTable: "AppExams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppExams_AppSpecializations_SpecializationId",
                table: "AppExams",
                column: "SpecializationId",
                principalTable: "AppSpecializations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
