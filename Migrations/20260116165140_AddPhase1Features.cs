using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase1Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Translations_Transcripts_SourceTranscriptId",
                table: "Translations");

            migrationBuilder.RenameColumn(
                name: "SourceTranscriptId",
                table: "Translations",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Translations",
                newName: "TranslatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Translations_SourceTranscriptId",
                table: "Translations",
                newName: "IX_Translations_UserId");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "TransactionLogs",
                newName: "TransactionType");

            migrationBuilder.AddColumn<string>(
                name: "DefaultExportFormat",
                table: "UserPreferences",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreferredSTTProvider",
                table: "UserPreferences",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalText",
                table: "Translations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceLanguage",
                table: "Translations",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TranscriptId",
                table: "Translations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "TransactionLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestData",
                table: "TransactionLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponseData",
                table: "TransactionLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Decisions",
                table: "MeetingNotes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingNotes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Translations_TranscriptId",
                table: "Translations",
                column: "TranscriptId");

            migrationBuilder.AddForeignKey(
                name: "FK_Translations_Transcripts_TranscriptId",
                table: "Translations",
                column: "TranscriptId",
                principalTable: "Transcripts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Translations_Users_UserId",
                table: "Translations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Translations_Transcripts_TranscriptId",
                table: "Translations");

            migrationBuilder.DropForeignKey(
                name: "FK_Translations_Users_UserId",
                table: "Translations");

            migrationBuilder.DropIndex(
                name: "IX_Translations_TranscriptId",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "DefaultExportFormat",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredSTTProvider",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "OriginalText",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "SourceLanguage",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "TranscriptId",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "TransactionLogs");

            migrationBuilder.DropColumn(
                name: "RequestData",
                table: "TransactionLogs");

            migrationBuilder.DropColumn(
                name: "ResponseData",
                table: "TransactionLogs");

            migrationBuilder.DropColumn(
                name: "Decisions",
                table: "MeetingNotes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeetingNotes");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Translations",
                newName: "SourceTranscriptId");

            migrationBuilder.RenameColumn(
                name: "TranslatedAt",
                table: "Translations",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Translations_UserId",
                table: "Translations",
                newName: "IX_Translations_SourceTranscriptId");

            migrationBuilder.RenameColumn(
                name: "TransactionType",
                table: "TransactionLogs",
                newName: "Action");

            migrationBuilder.AddForeignKey(
                name: "FK_Translations_Transcripts_SourceTranscriptId",
                table: "Translations",
                column: "SourceTranscriptId",
                principalTable: "Transcripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
