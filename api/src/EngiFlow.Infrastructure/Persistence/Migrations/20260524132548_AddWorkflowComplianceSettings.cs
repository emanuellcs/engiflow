using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngiFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowComplianceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_self_approval",
                table: "company_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_review_days_before_sla_breach",
                table: "company_settings",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddCheckConstraint(
                name: "ck_company_settings_max_review_days_before_sla_breach",
                table: "company_settings",
                sql: "\"max_review_days_before_sla_breach\" >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_company_settings_max_review_days_before_sla_breach",
                table: "company_settings");

            migrationBuilder.DropColumn(
                name: "allow_self_approval",
                table: "company_settings");

            migrationBuilder.DropColumn(
                name: "max_review_days_before_sla_breach",
                table: "company_settings");
        }
    }
}
