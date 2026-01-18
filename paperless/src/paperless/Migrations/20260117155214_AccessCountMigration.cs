using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace paperless.Migrations
{
    /// <inheritdoc />
    public partial class AccessCountMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "accesscount",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accesscount",
                table: "documents");
        }
    }
}
