using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Questions_Add_MediaSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "AppQuestions",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                comment: "رابط الوسائط (صورة/صوت/فيديو/مستند) المرتبطة بالسؤال",
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldComment: "رابط وسائط السؤال (صورة، صوت، فيديو)");

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "AppQuestions",
                type: "int",
                nullable: true,
                comment: "نوع الوسائط المرتبطة بالسؤال (صورة، صوت، فيديو، مستند)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "AppQuestions");

            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "AppQuestions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "",
                comment: "رابط وسائط السؤال (صورة، صوت، فيديو)",
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true,
                oldComment: "رابط الوسائط (صورة/صوت/فيديو/مستند) المرتبطة بالسؤال");
        }
    }
}