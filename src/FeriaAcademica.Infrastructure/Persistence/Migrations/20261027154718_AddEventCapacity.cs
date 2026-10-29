using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeriaAcademica.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Capacity",
                table: "Events",
                sql: "\"Capacity\" IS NULL OR \"Capacity\" >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Capacity",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Events");
        }
    }
}
