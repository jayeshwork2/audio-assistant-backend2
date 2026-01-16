using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase1Features_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old Translations table
            migrationBuilder.DropTable(
                name: "Translations");

            // Recreate Translations table with new schema
            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TranscriptId = table.Column<int>(type: "INTEGER", nullable: true),
                    OriginalText = table.Column<string>(type: "TEXT", nullable: false),
                    TranslatedText = table.Column<string>(type: "TEXT", nullable: false),
                    SourceLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TranslatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Translations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Translations_Transcripts_TranscriptId",
                        column: x => x.TranscriptId,
                        principalTable: "Transcripts",
                        principalColumn: "Id");
                });

            // Add DefaultExportFormat to UserPreferences
            migrationBuilder.AddColumn<string>(
                name: "DefaultExportFormat",
                table: "UserPreferences",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "pdf");

            // Update PreferredResponseStyle default from 'concise' to 'formal'
            migrationBuilder.Sql(
                "UPDATE UserPreferences SET PreferredResponseStyle = 'formal' WHERE PreferredResponseStyle = 'concise'");

            // Add UpdatedAt to MeetingNotes if not exists
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingNotes",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')");

            // Add Decisions column to MeetingNotes if not exists
            migrationBuilder.AddColumn<string>(
                name: "Decisions",
                table: "MeetingNotes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Translations_UserId",
                table: "Translations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Translations_TranscriptId",
                table: "Translations",
                column: "TranscriptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Translations");

            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceTranscriptId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TranslatedText = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Translations_Transcripts_SourceTranscriptId",
                        column: x => x.SourceTranscriptId,
                        principalTable: "Transcripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.DropColumn(
                name: "DefaultExportFormat",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeetingNotes");

            migrationBuilder.DropColumn(
                name: "Decisions",
                table: "MeetingNotes");
        }
    }
}
