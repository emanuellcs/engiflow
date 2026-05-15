using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngiFlow.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Creates the initial PostgreSQL schema for EngiFlow tenant, user, ECO, and audit-event persistence.
    /// </summary>
    /// <remarks>
    /// The schema stores strongly typed identifiers as UUID columns, enums as bounded
    /// strings, and composite tenant-aware relationships so audit and workflow records
    /// cannot reference users or ECOs from another company.
    /// </remarks>
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.UniqueConstraint("ak_users_id_company_id", x => new { x.id, x.company_id });
                    table.CheckConstraint("ck_users_role", "\"role\" IN ('Requester', 'Reviewer', 'Approver', 'Administrator')");
                    table.ForeignKey(
                        name: "FK_users_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "engineering_change_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineering_change_orders", x => x.id);
                    table.UniqueConstraint("ak_engineering_change_orders_id_company_id", x => new { x.id, x.company_id });
                    table.CheckConstraint("ck_engineering_change_orders_priority", "\"priority\" IN ('Low', 'Medium', 'High', 'Critical')");
                    table.CheckConstraint("ck_engineering_change_orders_status", "\"status\" IN ('Draft', 'UnderReview', 'Approved', 'Rejected', 'Implemented')");
                    table.ForeignKey(
                        name: "FK_engineering_change_orders_users_created_by_user_id_company_~",
                        columns: x => new { x.created_by_user_id, x.company_id },
                        principalTable: "users",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eco_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engineering_change_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    old_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    new_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eco_events", x => x.id);
                    table.CheckConstraint("ck_eco_events_event_type", "\"event_type\" IN ('Created', 'DetailsUpdated', 'SubmittedForReview', 'Approved', 'Rejected', 'Implemented')");
                    table.CheckConstraint("ck_eco_events_new_status", "\"new_status\" IS NULL OR \"new_status\" IN ('Draft', 'UnderReview', 'Approved', 'Rejected', 'Implemented')");
                    table.CheckConstraint("ck_eco_events_old_status", "\"old_status\" IS NULL OR \"old_status\" IN ('Draft', 'UnderReview', 'Approved', 'Rejected', 'Implemented')");
                    table.ForeignKey(
                        name: "FK_eco_events_engineering_change_orders_engineering_change_ord~",
                        columns: x => new { x.engineering_change_order_id, x.company_id },
                        principalTable: "engineering_change_orders",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eco_events_users_actor_user_id_company_id",
                        columns: x => new { x.actor_user_id, x.company_id },
                        principalTable: "users",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_companies_is_active",
                table: "companies",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_eco_events_actor_user_id_company_id",
                table: "eco_events",
                columns: new[] { "actor_user_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_eco_events_company_id_eco_id_occurred_at",
                table: "eco_events",
                columns: new[] { "company_id", "engineering_change_order_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_eco_events_engineering_change_order_id_company_id",
                table: "eco_events",
                columns: new[] { "engineering_change_order_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_engineering_change_orders_company_id_created_at",
                table: "engineering_change_orders",
                columns: new[] { "company_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_engineering_change_orders_company_id_status",
                table: "engineering_change_orders",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_engineering_change_orders_created_by_user_id_company_id",
                table: "engineering_change_orders",
                columns: new[] { "created_by_user_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_users_company_id_role",
                table: "users",
                columns: new[] { "company_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ux_users_company_id_email",
                table: "users",
                columns: new[] { "company_id", "email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eco_events");

            migrationBuilder.DropTable(
                name: "engineering_change_orders");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
