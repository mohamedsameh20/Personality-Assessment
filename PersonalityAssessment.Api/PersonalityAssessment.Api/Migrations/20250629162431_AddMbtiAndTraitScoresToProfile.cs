using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalityAssessment.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMbtiAndTraitScoresToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "PersonalityProfiles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MbtiType",
                table: "PersonalityProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TraitScoresJson",
                table: "PersonalityProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "PersonalityProfiles");

            migrationBuilder.DropColumn(
                name: "MbtiType",
                table: "PersonalityProfiles");

            migrationBuilder.DropColumn(
                name: "TraitScoresJson",
                table: "PersonalityProfiles");
        }
    }
}
