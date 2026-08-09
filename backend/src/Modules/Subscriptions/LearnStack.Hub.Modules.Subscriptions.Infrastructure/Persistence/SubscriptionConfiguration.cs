using LearnStack.Hub.Modules.Subscriptions.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnStack.Hub.Modules.Subscriptions.Infrastructure.Persistence;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<HubSubscription>
{
    public void Configure(EntityTypeBuilder<HubSubscription> builder)
    {
        builder.ToTable("subscriptions", t =>
        {
            t.HasCheckConstraint(
                "ck_subscriptions_status",
                "status IN ('Trial','Active','PastDue','Canceled','Expired')");
            t.HasCheckConstraint(
                "ck_subscriptions_payment_provider",
                "payment_provider IS NULL OR payment_provider IN ('stripe','iyzico')");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasConversion<HubSubscriptionId.EfCoreValueConverter>();

        // Cross-module FK columns: plain uuid + index, no EF navigation.
        builder.Property(s => s.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion<LearnStackTenantId.EfCoreValueConverter>();
        builder.HasIndex(s => s.TenantId).IsUnique().HasDatabaseName("ux_subscriptions_tenant_id");

        builder.Property(s => s.PlanId).HasColumnName("plan_id");
        builder.HasIndex(s => s.PlanId).HasDatabaseName("ix_subscriptions_plan_id");

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion(s => s.ToString(), v => Enum.Parse<SubscriptionStatus>(v))
            .IsRequired();

        builder.Property(s => s.TrialStart).HasColumnName("trial_start");
        builder.Property(s => s.TrialEnd).HasColumnName("trial_end");
        builder.Property(s => s.CurrentPeriodStart).HasColumnName("current_period_start");
        builder.Property(s => s.CurrentPeriodEnd).HasColumnName("current_period_end");
        builder.Property(s => s.CancelAtPeriodEnd).HasColumnName("cancel_at_period_end");
        builder.Property(s => s.PaymentProvider).HasColumnName("payment_provider");
        builder.Property(s => s.ProviderSubscriptionId).HasColumnName("provider_subscription_id");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.CreatedBy)
            .HasColumnName("created_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy)
            .HasColumnName("updated_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy)
            .HasColumnName("deleted_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();

        builder.Property(s => s.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
