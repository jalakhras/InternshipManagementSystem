using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateNormalisedName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalisedName",
                table: "AppCandidates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            // Filled for everybody already on the roll, in the same shape the
            // entity writes from now on. Without this every person who existed
            // before today would be findable only by the raw comparison — which
            // is the search this column was added because it fails.
            //
            // Written out as SQL rather than run through the C# folder, because
            // a data migration that needs the application booted is a migration
            // that cannot run where migrations run.
            migrationBuilder.Sql(
                "UPDATE [AppCandidates] SET [NormalisedName] = LOWER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(FullName, NCHAR(0x0640), N''), NCHAR(0x0670), N''), NCHAR(0x064B), N''), NCHAR(0x064C), N''), NCHAR(0x064D), N''), NCHAR(0x064E), N''), NCHAR(0x064F), N''), NCHAR(0x0650), N''), NCHAR(0x0651), N''), NCHAR(0x0652), N''), NCHAR(0x0653), N''), NCHAR(0x0654), N''), NCHAR(0x0655), N''), NCHAR(0x0656), N''), NCHAR(0x0657), N''), NCHAR(0x0658), N''), NCHAR(0x0659), N''), NCHAR(0x065A), N''), NCHAR(0x065B), N''), NCHAR(0x065C), N''), NCHAR(0x065D), N''), NCHAR(0x065E), N''), NCHAR(0x065F), N''), NCHAR(0x06D6), N''), NCHAR(0x06D7), N''), NCHAR(0x06D8), N''), NCHAR(0x06D9), N''), NCHAR(0x06DA), N''), NCHAR(0x06DB), N''), NCHAR(0x06DC), N''), NCHAR(0x06DD), N''), NCHAR(0x06DE), N''), NCHAR(0x06DF), N''), NCHAR(0x06E0), N''), NCHAR(0x06E1), N''), NCHAR(0x06E2), N''), NCHAR(0x06E3), N''), NCHAR(0x06E4), N''), NCHAR(0x06E5), N''), NCHAR(0x06E6), N''), NCHAR(0x06E7), N''), NCHAR(0x06E8), N''), NCHAR(0x06E9), N''), NCHAR(0x06EA), N''), NCHAR(0x06EB), N''), NCHAR(0x06EC), N''), NCHAR(0x06ED), N''), NCHAR(0x0623), NCHAR(0x0627)), NCHAR(0x0625), NCHAR(0x0627)), NCHAR(0x0622), NCHAR(0x0627)), NCHAR(0x0671), NCHAR(0x0627)), NCHAR(0x0629), NCHAR(0x0647)), NCHAR(0x0649), NCHAR(0x064A)), NCHAR(0x0624), NCHAR(0x0648)), NCHAR(0x0626), NCHAR(0x064A)))))");

            migrationBuilder.CreateIndex(
                name: "IX_AppCandidates_TenantId_NormalisedName",
                table: "AppCandidates",
                columns: new[] { "TenantId", "NormalisedName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppCandidates_TenantId_NormalisedName",
                table: "AppCandidates");

            migrationBuilder.DropColumn(
                name: "NormalisedName",
                table: "AppCandidates");
        }
    }
}
