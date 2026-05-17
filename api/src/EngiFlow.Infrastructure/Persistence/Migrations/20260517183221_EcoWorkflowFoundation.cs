using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngiFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EcoWorkflowFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_engineering_change_orders_status",
                table: "engineering_change_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_eco_events_event_type",
                table: "eco_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_eco_events_new_status",
                table: "eco_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_eco_events_old_status",
                table: "eco_events");

            migrationBuilder.AddColumn<int>(
                name: "review_round",
                table: "engineering_change_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "company_settings",
                columns: table => new
                {
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_approvals_required = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_settings", x => x.company_id);
                    table.CheckConstraint("ck_company_settings_min_approvals_required", "\"min_approvals_required\" >= 1");
                    table.ForeignKey(
                        name: "FK_company_settings_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eco_affected_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engineering_change_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    current_revision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    new_revision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eco_affected_items", x => x.id);
                    table.CheckConstraint("ck_eco_affected_items_action", "\"action\" IN ('Add', 'Modify', 'Remove')");
                    table.ForeignKey(
                        name: "FK_eco_affected_items_engineering_change_orders_engineering_ch~",
                        columns: x => new { x.engineering_change_order_id, x.company_id },
                        principalTable: "engineering_change_orders",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eco_affected_items_users_created_by_user_id_company_id",
                        columns: x => new { x.created_by_user_id, x.company_id },
                        principalTable: "users",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eco_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engineering_change_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    review_round = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eco_approvals", x => x.id);
                    table.CheckConstraint("ck_eco_approvals_decision", "\"decision\" IN ('Approve', 'RequestChanges')");
                    table.ForeignKey(
                        name: "FK_eco_approvals_engineering_change_orders_engineering_change_~",
                        columns: x => new { x.engineering_change_order_id, x.company_id },
                        principalTable: "engineering_change_orders",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eco_approvals_users_approver_user_id_company_id",
                        columns: x => new { x.approver_user_id, x.company_id },
                        principalTable: "users",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eco_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engineering_change_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    object_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eco_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_eco_attachments_engineering_change_orders_engineering_chang~",
                        columns: x => new { x.engineering_change_order_id, x.company_id },
                        principalTable: "engineering_change_orders",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eco_attachments_users_uploaded_by_user_id_company_id",
                        columns: x => new { x.uploaded_by_user_id, x.company_id },
                        principalTable: "users",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eco_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engineering_change_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eco_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_eco_comments_engineering_change_orders_engineering_change_o~",
                        columns: x => new { x.engineering_change_order_id, x.company_id },
                        principalTable: "engineering_change_orders",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eco_comments_users_author_user_id_company_id",
                        columns: x => new { x.author_user_id, x.company_id },
                        principalTable: "users",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                UPDATE users
                SET role = 'Approver'
                WHERE role = 'Reviewer';
                """);

            migrationBuilder.Sql(
                """
                WITH ranked_administrators AS (
                    SELECT id,
                           company_id,
                           ROW_NUMBER() OVER (PARTITION BY company_id ORDER BY created_at, id) AS role_rank
                    FROM users
                    WHERE role = 'Administrator'
                )
                UPDATE users AS target
                SET role = 'Owner'
                FROM ranked_administrators AS ranked
                WHERE target.id = ranked.id
                  AND target.company_id = ranked.company_id
                  AND ranked.role_rank = 1;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO company_settings (company_id, min_approvals_required, created_at, updated_at)
                SELECT id, 1, NOW(), NOW()
                FROM companies
                ON CONFLICT (company_id) DO NOTHING;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role",
                table: "users",
                sql: "\"role\" IN ('Owner', 'Administrator', 'Approver', 'Requester', 'Viewer')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_engineering_change_orders_status",
                table: "engineering_change_orders",
                sql: "\"status\" IN ('Draft', 'UnderReview', 'Approved', 'Canceled', 'Rejected', 'Implemented')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_eco_events_event_type",
                table: "eco_events",
                sql: "\"event_type\" IN ('Created', 'DetailsUpdated', 'SubmittedForReview', 'ReviewDecisionSubmitted', 'Approved', 'ChangesRequested', 'AffectedItemAdded', 'AffectedItemRemoved', 'CommentAdded', 'AttachmentAdded', 'Canceled', 'Rejected', 'Implemented')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_eco_events_new_status",
                table: "eco_events",
                sql: "\"new_status\" IS NULL OR \"new_status\" IN ('Draft', 'UnderReview', 'Approved', 'Canceled', 'Rejected', 'Implemented')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_eco_events_old_status",
                table: "eco_events",
                sql: "\"old_status\" IS NULL OR \"old_status\" IN ('Draft', 'UnderReview', 'Approved', 'Canceled', 'Rejected', 'Implemented')");

            migrationBuilder.CreateIndex(
                name: "ix_eco_affected_items_company_id_eco_id_part_number",
                table: "eco_affected_items",
                columns: new[] { "company_id", "engineering_change_order_id", "part_number" });

            migrationBuilder.CreateIndex(
                name: "IX_eco_affected_items_created_by_user_id_company_id",
                table: "eco_affected_items",
                columns: new[] { "created_by_user_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_eco_affected_items_engineering_change_order_id_company_id",
                table: "eco_affected_items",
                columns: new[] { "engineering_change_order_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_eco_approvals_approver_user_id_company_id",
                table: "eco_approvals",
                columns: new[] { "approver_user_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_eco_approvals_engineering_change_order_id_company_id",
                table: "eco_approvals",
                columns: new[] { "engineering_change_order_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ux_eco_approvals_company_id_eco_id_round_approver",
                table: "eco_approvals",
                columns: new[] { "company_id", "engineering_change_order_id", "review_round", "approver_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_eco_attachments_company_id_eco_id_uploaded_at",
                table: "eco_attachments",
                columns: new[] { "company_id", "engineering_change_order_id", "uploaded_at" });

            migrationBuilder.CreateIndex(
                name: "IX_eco_attachments_engineering_change_order_id_company_id",
                table: "eco_attachments",
                columns: new[] { "engineering_change_order_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_eco_attachments_uploaded_by_user_id_company_id",
                table: "eco_attachments",
                columns: new[] { "uploaded_by_user_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ux_eco_attachments_company_id_object_key",
                table: "eco_attachments",
                columns: new[] { "company_id", "object_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eco_comments_author_user_id_company_id",
                table: "eco_comments",
                columns: new[] { "author_user_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_eco_comments_company_id_eco_id_created_at",
                table: "eco_comments",
                columns: new[] { "company_id", "engineering_change_order_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_eco_comments_engineering_change_order_id_company_id",
                table: "eco_comments",
                columns: new[] { "engineering_change_order_id", "company_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_settings");

            migrationBuilder.DropTable(
                name: "eco_affected_items");

            migrationBuilder.DropTable(
                name: "eco_approvals");

            migrationBuilder.DropTable(
                name: "eco_attachments");

            migrationBuilder.DropTable(
                name: "eco_comments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_engineering_change_orders_status",
                table: "engineering_change_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_eco_events_event_type",
                table: "eco_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_eco_events_new_status",
                table: "eco_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_eco_events_old_status",
                table: "eco_events");

            migrationBuilder.DropColumn(
                name: "review_round",
                table: "engineering_change_orders");

            migrationBuilder.Sql(
                """
                UPDATE users
                SET role = 'Administrator'
                WHERE role = 'Owner';

                UPDATE users
                SET role = 'Requester'
                WHERE role = 'Viewer';

                UPDATE engineering_change_orders
                SET status = 'Rejected'
                WHERE status = 'Canceled';

                UPDATE eco_events
                SET event_type = 'DetailsUpdated'
                WHERE event_type IN (
                    'ReviewDecisionSubmitted',
                    'ChangesRequested',
                    'AffectedItemAdded',
                    'AffectedItemRemoved',
                    'CommentAdded',
                    'AttachmentAdded',
                    'Canceled'
                );

                UPDATE eco_events
                SET old_status = 'Rejected'
                WHERE old_status = 'Canceled';

                UPDATE eco_events
                SET new_status = 'Rejected'
                WHERE new_status = 'Canceled';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role",
                table: "users",
                sql: "\"role\" IN ('Requester', 'Reviewer', 'Approver', 'Administrator')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_engineering_change_orders_status",
                table: "engineering_change_orders",
                sql: "\"status\" IN ('Draft', 'UnderReview', 'Approved', 'Rejected', 'Implemented')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_eco_events_event_type",
                table: "eco_events",
                sql: "\"event_type\" IN ('Created', 'DetailsUpdated', 'SubmittedForReview', 'Approved', 'Rejected', 'Implemented')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_eco_events_new_status",
                table: "eco_events",
                sql: "\"new_status\" IS NULL OR \"new_status\" IN ('Draft', 'UnderReview', 'Approved', 'Rejected', 'Implemented')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_eco_events_old_status",
                table: "eco_events",
                sql: "\"old_status\" IS NULL OR \"old_status\" IN ('Draft', 'UnderReview', 'Approved', 'Rejected', 'Implemented')");
        }
    }
}
