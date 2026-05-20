using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngiFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserLifecycleAndTenantSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_email",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "companies",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE users
                SET status = CASE WHEN is_active THEN 'Active' ELSE 'Deactivated' END;
                """);

            migrationBuilder.Sql("""
                UPDATE companies AS company
                SET contact_email =
                (
                    SELECT users.email
                    FROM users
                    WHERE users.company_id = company.id
                    ORDER BY
                        CASE users.role
                            WHEN 'Owner' THEN 0
                            WHEN 'Administrator' THEN 1
                            ELSE 2
                        END,
                        users.created_at
                    LIMIT 1
                )
                WHERE company.contact_email IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");

            migrationBuilder.CreateTable(
                name: "password_setup_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_setup_tokens", x => x.id);
                    table.CheckConstraint("ck_password_setup_tokens_purpose", "\"purpose\" IN ('Invitation', 'Reset')");
                    table.ForeignKey(
                        name: "FK_password_setup_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_events", x => x.id);
                    table.CheckConstraint("ck_user_events_event_type", "\"event_type\" IN ('UserInvited', 'UserActivated', 'UserDeactivated', 'UserReactivated', 'PasswordResetRequested', 'FirstAccessInvitationResent')");
                    table.ForeignKey(
                        name: "FK_user_events_users_actor_id_company_id",
                        columns: x => new { x.actor_id, x.company_id },
                        principalTable: "users",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_events_users_user_id_company_id",
                        columns: x => new { x.user_id, x.company_id },
                        principalTable: "users",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_company_id_status",
                table: "users",
                columns: new[] { "company_id", "status" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_status",
                table: "users",
                sql: "\"status\" IN ('PendingActivation', 'Active', 'Deactivated')");

            migrationBuilder.CreateIndex(
                name: "ix_password_setup_tokens_user_id_purpose_expires_at",
                table: "password_setup_tokens",
                columns: new[] { "user_id", "purpose", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_password_setup_tokens_token_hash",
                table: "password_setup_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_events_actor_id_company_id",
                table: "user_events",
                columns: new[] { "actor_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_events_company_id_user_id_occurred_at",
                table: "user_events",
                columns: new[] { "company_id", "user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_events_user_id_company_id",
                table: "user_events",
                columns: new[] { "user_id", "company_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_setup_tokens");

            migrationBuilder.DropTable(
                name: "user_events");

            migrationBuilder.DropIndex(
                name: "ix_users_company_id_status",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_status",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE users
                SET is_active = status <> 'Deactivated';
                """);

            migrationBuilder.DropColumn(
                name: "status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "companies");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);
        }
    }
}
