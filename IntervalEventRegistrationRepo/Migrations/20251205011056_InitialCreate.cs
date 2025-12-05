using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IntervalEventRegistrationRepo.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_providers",
                columns: table => new
                {
                    provider_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_providers", x => x.provider_id);
                });

            migrationBuilder.CreateTable(
                name: "halls",
                columns: table => new
                {
                    hall_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    facilities = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_halls", x => x.hall_id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    permissions = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "speakers",
                columns: table => new
                {
                    speaker_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    linkedin_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_speakers", x => x.speaker_id);
                });

            migrationBuilder.CreateTable(
                name: "seats",
                columns: table => new
                {
                    seat_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hall_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    seat_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    row_label = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    section = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seats", x => x.seat_id);
                    table.ForeignKey(
                        name: "FK_seats_halls_hall_id",
                        column: x => x.hall_id,
                        principalTable: "halls",
                        principalColumn: "hall_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    student_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hall_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    organizer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    club_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    club_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_seats = table.Column<int>(type: "integer", nullable: false),
                    registered_count = table.Column<int>(type: "integer", nullable: false),
                    checked_in_count = table.Column<int>(type: "integer", nullable: false),
                    tags = table.Column<string>(type: "text", nullable: true),
                    max_tickets_per_user = table.Column<int>(type: "integer", nullable: false),
                    registration_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    registration_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_events_halls_hall_id",
                        column: x => x.hall_id,
                        principalTable: "halls",
                        principalColumn: "hall_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_events_users_organizer_id",
                        column: x => x.organizer_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_auth_providers",
                columns: table => new
                {
                    user_auth_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    access_token = table.Column<string>(type: "text", nullable: true),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    id_token = table.Column<string>(type: "text", nullable: true),
                    token_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_auth_providers", x => x.user_auth_id);
                    table.ForeignKey(
                        name: "FK_user_auth_providers_auth_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "auth_providers",
                        principalColumn: "provider_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_auth_providers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_speakers",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    speaker_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_speakers", x => new { x.event_id, x.speaker_id });
                    table.ForeignKey(
                        name: "FK_event_speakers_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_event_speakers_speakers_speaker_id",
                        column: x => x.speaker_id,
                        principalTable: "speakers",
                        principalColumn: "speaker_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_staff",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_staff", x => new { x.event_id, x.staff_id });
                    table.ForeignKey(
                        name: "FK_event_staff_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_event_staff_users_staff_id",
                        column: x => x.staff_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    ticket_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    student_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    seat_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ticket_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    qr_code = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    check_in_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.ticket_id);
                    table.ForeignKey(
                        name: "FK_tickets_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tickets_seats_seat_id",
                        column: x => x.seat_id,
                        principalTable: "seats",
                        principalColumn: "seat_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tickets_users_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ticket_checkins",
                columns: table => new
                {
                    checkin_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ticket_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    checkin_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_checkins", x => x.checkin_id);
                    table.ForeignKey(
                        name: "FK_ticket_checkins_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "ticket_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ticket_checkins_users_staff_id",
                        column: x => x.staff_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "auth_providers",
                columns: new[] { "provider_id", "created_at", "description", "provider_name", "updated_at" },
                values: new object[,]
                {
                    { "google", new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(1124), "Google OAuth 2.0", "Google", null },
                    { "local", new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(1126), "Local username/password", "Local", null }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "role_id", "created_at", "description", "permissions", "role_name", "updated_at" },
                values: new object[,]
                {
                    { "admin", new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(985), "System Administrator", "*", "Admin", null },
                    { "organizer", new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(996), "Event Organizer", "events.*,tickets.read,reports.read", "Organizer", null },
                    { "staff", new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(1002), "Event Staff", "checkin.*,events.read", "Staff", null },
                    { "student", new DateTime(2025, 12, 5, 1, 10, 54, 158, DateTimeKind.Utc).AddTicks(1007), "Student/Attendee", "events.read,tickets.own", "Student", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_providers_provider_name",
                table: "auth_providers",
                column: "provider_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_speakers_speaker_id",
                table: "event_speakers",
                column: "speaker_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_staff_staff_id",
                table: "event_staff",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_hall_id",
                table: "events",
                column: "hall_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_organizer_id",
                table: "events",
                column: "organizer_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_role_name",
                table: "roles",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seats_hall_id",
                table: "seats",
                column: "hall_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_checkins_staff_id",
                table: "ticket_checkins",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_checkins_ticket_id",
                table: "ticket_checkins",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_event_id",
                table: "tickets",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_seat_id",
                table: "tickets",
                column: "seat_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_student_id",
                table: "tickets",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_ticket_code",
                table: "tickets",
                column: "ticket_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_auth_providers_provider_id_provider_user_id",
                table: "user_auth_providers",
                columns: new[] { "provider_id", "provider_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_auth_providers_user_id",
                table: "user_auth_providers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_student_code",
                table: "users",
                column: "student_code",
                unique: true,
                filter: "student_code IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_speakers");

            migrationBuilder.DropTable(
                name: "event_staff");

            migrationBuilder.DropTable(
                name: "ticket_checkins");

            migrationBuilder.DropTable(
                name: "user_auth_providers");

            migrationBuilder.DropTable(
                name: "speakers");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "auth_providers");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "seats");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "halls");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
