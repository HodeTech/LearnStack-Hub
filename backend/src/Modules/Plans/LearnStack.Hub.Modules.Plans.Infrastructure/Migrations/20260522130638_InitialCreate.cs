using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnStack.Hub.Modules.Plans.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "hub");

        migrationBuilder.CreateTable(
            name: "plans",
            schema: "hub",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                tier = table.Column<string>(type: "text", nullable: false),
                base_price_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                billing_cycle = table.Column<string>(type: "text", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                compliance_defaults = table.Column<string>(type: "jsonb", nullable: false),
                features = table.Column<string>(type: "jsonb", nullable: false),
                limits = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plans", x => x.id);
                table.CheckConstraint("ck_plans_billing_cycle", "billing_cycle IN ('monthly','annual')");
                table.CheckConstraint("ck_plans_tier", "tier IN ('starter','growth','scale','enterprise','custom')");
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "plans",
            schema: "hub");
    }
}
