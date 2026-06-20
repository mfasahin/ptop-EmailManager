using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalMailSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InternalMails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ToEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    EncryptedSubject = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedBody = table.Column<string>(type: "TEXT", nullable: false),
                    IsHtml = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeletedByRecipient = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeletedBySender = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalMails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternalMails_FromEmail",
                table: "InternalMails",
                column: "FromEmail");

            migrationBuilder.CreateIndex(
                name: "IX_InternalMails_SentAt",
                table: "InternalMails",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_InternalMails_ToEmail",
                table: "InternalMails",
                column: "ToEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternalMails");
        }
    }
}
