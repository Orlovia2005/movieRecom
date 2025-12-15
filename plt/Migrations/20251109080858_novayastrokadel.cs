using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace plt.Migrations
{
    /// <inheritdoc />
    public partial class novayastrokadel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "vseok",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "vseok",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
