using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace plt.Migrations
{
    /// <inheritdoc />
    public partial class dlvs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_ServicesDate_LastDateId",
                table: "Client");

            migrationBuilder.DropForeignKey(
                name: "FK_Client_Users_ProfiId",
                table: "Client");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicesDate",
                table: "ServicesDate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Client",
                table: "Client");

            migrationBuilder.RenameTable(
                name: "ServicesDate",
                newName: "ServiceDates");

            migrationBuilder.RenameTable(
                name: "Client",
                newName: "Clients");

            migrationBuilder.RenameIndex(
                name: "IX_Client_ProfiId",
                table: "Clients",
                newName: "IX_Clients_ProfiId");

            migrationBuilder.RenameIndex(
                name: "IX_Client_LastDateId",
                table: "Clients",
                newName: "IX_Clients_LastDateId");

            migrationBuilder.AddColumn<double>(
                name: "Price",
                table: "ServiceDates",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ProfiId",
                table: "ServiceDates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "SecondName",
                table: "Clients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Clients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Clients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Clients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "service_dates_pkey",
                table: "ServiceDates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "clients_pkey",
                table: "Clients",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDates_ClientId",
                table: "ServiceDates",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDates_ProfiId",
                table: "ServiceDates",
                column: "ProfiId");

            migrationBuilder.AddForeignKey(
                name: "fk_clients_service_dates_last_date_id",
                table: "Clients",
                column: "LastDateId",
                principalTable: "ServiceDates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_clients_users_profi_id",
                table: "Clients",
                column: "ProfiId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_service_dates_clients_client_id",
                table: "ServiceDates",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_service_dates_users_profi_id",
                table: "ServiceDates",
                column: "ProfiId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clients_service_dates_last_date_id",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "fk_clients_users_profi_id",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "fk_service_dates_clients_client_id",
                table: "ServiceDates");

            migrationBuilder.DropForeignKey(
                name: "fk_service_dates_users_profi_id",
                table: "ServiceDates");

            migrationBuilder.DropPrimaryKey(
                name: "service_dates_pkey",
                table: "ServiceDates");

            migrationBuilder.DropIndex(
                name: "IX_ServiceDates_ClientId",
                table: "ServiceDates");

            migrationBuilder.DropIndex(
                name: "IX_ServiceDates_ProfiId",
                table: "ServiceDates");

            migrationBuilder.DropPrimaryKey(
                name: "clients_pkey",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ServiceDates");

            migrationBuilder.DropColumn(
                name: "ProfiId",
                table: "ServiceDates");

            migrationBuilder.RenameTable(
                name: "ServiceDates",
                newName: "ServicesDate");

            migrationBuilder.RenameTable(
                name: "Clients",
                newName: "Client");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_ProfiId",
                table: "Client",
                newName: "IX_Client_ProfiId");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_LastDateId",
                table: "Client",
                newName: "IX_Client_LastDateId");

            migrationBuilder.AlterColumn<string>(
                name: "SecondName",
                table: "Client",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Client",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Client",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Client",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicesDate",
                table: "ServicesDate",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Client",
                table: "Client",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_ServicesDate_LastDateId",
                table: "Client",
                column: "LastDateId",
                principalTable: "ServicesDate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_Users_ProfiId",
                table: "Client",
                column: "ProfiId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
