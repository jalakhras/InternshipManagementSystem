using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <summary>
    /// Drops the section from a passage, which nothing ever put there.
    /// <para>
    /// <c>QuestionGroup.ExamSectionId</c> was dead at every layer at once: no DTO
    /// carried it, no screen offered it, <c>CreateGroupAsync</c> never assigned it
    /// and <c>ToDto</c> never projected it — so no row in any database has ever
    /// held a value, and this drops a column of nulls. <c>DrawBySection</c> pools
    /// a section from <c>Question.ExamSectionId</c> alone, so a passage filed here
    /// would have contributed nothing to the paper even if something had filed it.
    /// </para>
    /// <para>
    /// Removed rather than finished because it is a second answer to a question
    /// that already has one. A passage in Reading whose questions say Listening is
    /// a disagreement with no precedence rule, no screen to see it on, and a
    /// candidate at the far end of it. Filing each of a passage's questions into
    /// the same section gives the same result — <c>Draw</c> takes whole blocks, so
    /// the passage is never half-served — and leaves one place where the answer
    /// lives.
    /// </para>
    /// <para>
    /// What is given up: filing a six-question passage once instead of six times.
    /// Bulk filing would close that, and is worth more than this column was,
    /// because it helps every question rather than only the ones under a passage.
    /// </para>
    /// <para>
    /// <c>Down</c> restores the column, its index and its foreign key. It cannot
    /// restore values, and there are none to restore.
    /// </para>
    /// </summary>
    public partial class Drop_Section_From_Passage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppQuestionGroups_AppExamSections_ExamSectionId",
                table: "AppQuestionGroups");

            migrationBuilder.DropIndex(
                name: "IX_AppQuestionGroups_ExamSectionId",
                table: "AppQuestionGroups");

            migrationBuilder.DropColumn(
                name: "ExamSectionId",
                table: "AppQuestionGroups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExamSectionId",
                table: "AppQuestionGroups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppQuestionGroups_ExamSectionId",
                table: "AppQuestionGroups",
                column: "ExamSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppQuestionGroups_AppExamSections_ExamSectionId",
                table: "AppQuestionGroups",
                column: "ExamSectionId",
                principalTable: "AppExamSections",
                principalColumn: "Id");
        }
    }
}
