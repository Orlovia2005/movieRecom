using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace plt.Migrations
{
    /// <inheritdoc />
    public partial class servicesdate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "PriceServices",
                table: "Client",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "LastDateId",
                table: "Client",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServicesDate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesDate", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Client_LastDateId",
                table: "Client",
                column: "LastDateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_ServicesDate_LastDateId",
                table: "Client",
                column: "LastDateId",
                principalTable: "ServicesDate",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_ServicesDate_LastDateId",
                table: "Client");

            migrationBuilder.DropTable(
                name: "ServicesDate");

            migrationBuilder.DropIndex(
                name: "IX_Client_LastDateId",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "LastDateId",
                table: "Client");

            migrationBuilder.AlterColumn<int>(
                name: "PriceServices",
                table: "Client",
                type: "integer",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }
    }
}
