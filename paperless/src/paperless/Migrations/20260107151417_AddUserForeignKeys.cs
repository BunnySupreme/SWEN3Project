using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace paperless.Migrations
{
    /// <inheritdoc />
    public partial class AddUserForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_sessions_userid",
                table: "sessions",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_documents_userid",
                table: "documents",
                column: "userid");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_users_userid",
                table: "documents",
                column: "userid",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sessions_users_userid",
                table: "sessions",
                column: "userid",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_users_userid",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_sessions_users_userid",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_userid",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "IX_documents_userid",
                table: "documents");
        }
    }
}
