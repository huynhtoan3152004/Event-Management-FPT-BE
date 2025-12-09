using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntervalEventRegistrationRepo.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSeatManagementColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "event_id",
                table: "seats",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_rows",
                table: "halls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_seats_per_row",
                table: "halls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "number_of_rows",
                table: "events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "seats_per_row",
                table: "events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "auth_providers",
                keyColumn: "provider_id",
                keyValue: "google",
                column: "created_at",
                value: new DateTime(2025, 12, 9, 5, 19, 28, 201, DateTimeKind.Utc).AddTicks(8362));

            migrationBuilder.UpdateData(
                table: "auth_providers",
                keyColumn: "provider_id",
                keyValue: "local",
                column: "created_at",
                value: new DateTime(2025, 12, 9, 5, 19, 28, 201, DateTimeKind.Utc).AddTicks(8365));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "admin",
                column: "created_at",
                value: new DateTime(2025, 12, 9, 5, 19, 28, 201, DateTimeKind.Utc).AddTicks(8201));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "organizer",
                column: "created_at",
                value: new DateTime(2025, 12, 9, 5, 19, 28, 201, DateTimeKind.Utc).AddTicks(8220));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "staff",
                column: "created_at",
                value: new DateTime(2025, 12, 9, 5, 19, 28, 201, DateTimeKind.Utc).AddTicks(8225));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "student",
                column: "created_at",
                value: new DateTime(2025, 12, 9, 5, 19, 28, 201, DateTimeKind.Utc).AddTicks(8231));

            migrationBuilder.CreateIndex(
                name: "IX_seats_event_id",
                table: "seats",
                column: "event_id");

            migrationBuilder.AddForeignKey(
                name: "FK_seats_events_event_id",
                table: "seats",
                column: "event_id",
                principalTable: "events",
                principalColumn: "event_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_seats_events_event_id",
                table: "seats");

            migrationBuilder.DropIndex(
                name: "IX_seats_event_id",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "event_id",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "max_rows",
                table: "halls");

            migrationBuilder.DropColumn(
                name: "max_seats_per_row",
                table: "halls");

            migrationBuilder.DropColumn(
                name: "number_of_rows",
                table: "events");

            migrationBuilder.DropColumn(
                name: "seats_per_row",
                table: "events");

            migrationBuilder.UpdateData(
                table: "auth_providers",
                keyColumn: "provider_id",
                keyValue: "google",
                column: "created_at",
                value: new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(1124));

            migrationBuilder.UpdateData(
                table: "auth_providers",
                keyColumn: "provider_id",
                keyValue: "local",
                column: "created_at",
                value: new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(1126));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "admin",
                column: "created_at",
                value: new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(985));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "organizer",
                column: "created_at",
                value: new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(996));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "staff",
                column: "created_at",
                value: new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(1002));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "student",
                column: "created_at",
                value: new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(1007));
        }
    }
}
