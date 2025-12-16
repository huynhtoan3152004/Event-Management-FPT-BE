using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntervalEventRegistrationRepo.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutTimeToTicketCheckin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "checkout_time",
                table: "ticket_checkins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "auth_providers",
                keyColumn: "provider_id",
                keyValue: "google",
                column: "created_at",
                value: new DateTime(2025, 12, 16, 11, 45, 55, 973, DateTimeKind.Utc).AddTicks(3266));

            migrationBuilder.UpdateData(
                table: "auth_providers",
                keyColumn: "provider_id",
                keyValue: "local",
                column: "created_at",
                value: new DateTime(2025, 12, 16, 11, 45, 55, 973, DateTimeKind.Utc).AddTicks(3271));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "admin",
                column: "created_at",
                value: new DateTime(2025, 12, 16, 11, 45, 55, 973, DateTimeKind.Utc).AddTicks(3014));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "organizer",
                column: "created_at",
                value: new DateTime(2025, 12, 16, 11, 45, 55, 973, DateTimeKind.Utc).AddTicks(3030));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "staff",
                column: "created_at",
                value: new DateTime(2025, 12, 16, 11, 45, 55, 973, DateTimeKind.Utc).AddTicks(3038));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: "student",
                column: "created_at",
                value: new DateTime(2025, 12, 16, 11, 45, 55, 973, DateTimeKind.Utc).AddTicks(3045));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "checkout_time",
                table: "ticket_checkins");

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
        }
    }
}
