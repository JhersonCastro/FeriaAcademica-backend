using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeriaAcademica.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ArtifactFileNameAndSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Artifacts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "Artifacts",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "Artifacts");
        }
    }
}
