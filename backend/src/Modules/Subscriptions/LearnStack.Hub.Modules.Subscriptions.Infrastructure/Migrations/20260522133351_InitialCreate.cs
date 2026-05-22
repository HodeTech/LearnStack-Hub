using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnStack.Hub.Modules.Subscriptions.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "hub");

        migrationBuilder.CreateTable(
            name: "subscriptions",
            schema: "hub",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                trial_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                trial_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                current_period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                current_period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                payment_provider = table.Column<string>(type: "text", nullable: true),
                provider_subscription_id = table.Column<string>(type: "text", nullable: true),
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
                table.PrimaryKey("PK_subscriptions", x => x.id);
                table.CheckConstraint("ck_subscriptions_payment_provider", "payment_provider IS NULL OR payment_provider IN ('stripe','iyzico')");
                table.CheckConstraint("ck_subscriptions_status", "status IN ('Trial','Active','PastDue','Canceled','Expired')");
            });

        migrationBuilder.CreateIndex(
            name: "ix_subscriptions_plan_id",
            schema: "hub",
            table: "subscriptions",
            column: "plan_id");

        migrationBuilder.CreateIndex(
            name: "ux_subscriptions_tenant_id",
            schema: "hub",
            table: "subscriptions",
            column: "tenant_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "subscriptions",
            schema: "hub");
    }
}
