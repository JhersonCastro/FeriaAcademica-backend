using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeriaAcademica.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalVersionEventReportAndCertificateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Certificates_EventId",
                table: "Certificates");

            migrationBuilder.AddColumn<bool>(
                name: "IsFinal",
                table: "Artifacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EventReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Html = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventReports_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventReports_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_EventId_UserId_Type",
                table: "Certificates",
                columns: new[] { "EventId", "UserId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_WorkId",
                table: "Artifacts",
                column: "WorkId",
                unique: true,
                filter: "\"IsFinal\"");

            migrationBuilder.CreateIndex(
                name: "IX_EventReports_EventId",
                table: "EventReports",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventReports_UpdatedBy",
                table: "EventReports",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventReports");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_EventId_UserId_Type",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Artifacts_WorkId",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "IsFinal",
                table: "Artifacts");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_EventId",
                table: "Certificates",
                column: "EventId");
        }
    }
}
