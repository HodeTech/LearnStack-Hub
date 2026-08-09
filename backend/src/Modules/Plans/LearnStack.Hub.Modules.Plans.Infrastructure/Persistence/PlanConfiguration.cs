using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.SharedKernel.Compliance;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnStack.Hub.Modules.Plans.Infrastructure.Persistence;

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans", t =>
        {
            t.HasCheckConstraint(
                "ck_plans_tier",
                "tier IN ('starter','growth','scale','enterprise','custom')");
            t.HasCheckConstraint(
                "ck_plans_billing_cycle",
                "billing_cycle IN ('monthly','annual')");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion<PlanId.EfCoreValueConverter>();

        builder.Property(p => p.Name).HasColumnName("name").IsRequired();

        builder.Property(p => p.Tier)
            .HasColumnName("tier")
            .HasConversion(
                tier => tier.ToString().ToLowerInvariant(),
                value => Enum.Parse<PlanTier>(value, ignoreCase: true))
            .IsRequired();

        builder.Property(p => p.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasConversion(
                cycle => cycle.ToString().ToLowerInvariant(),
                value => Enum.Parse<BillingCycle>(value, ignoreCase: true))
            .IsRequired();

        builder.Property(p => p.BasePriceUsd).HasColumnName("base_price_usd").HasColumnType("numeric(10,2)");
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active");

        builder.Property<Dictionary<string, bool>>("_features")
            .HasColumnName("features")
            .HasColumnType("jsonb")
            .HasConversion(JsonbConversions.DictionaryConverter<bool>(), JsonbConversions.DictionaryComparer<bool>());

        builder.Property<Dictionary<string, long>>("_limits")
            .HasColumnName("limits")
            .HasColumnType("jsonb")
            .HasConversion(JsonbConversions.DictionaryConverter<long>(), JsonbConversions.DictionaryComparer<long>());

        builder.Property<Dictionary<string, ComplianceCap>>("_complianceDefaults")
            .HasColumnName("compliance_defaults")
            .HasColumnType("jsonb")
            .HasConversion(
                JsonbConversions.DictionaryConverter<ComplianceCap>(),
                JsonbConversions.DictionaryComparer<ComplianceCap>());

        ConfigureAuditColumns(builder);
    }

    private static void ConfigureAuditColumns(EntityTypeBuilder<Plan> builder)
    {
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy)
            .HasColumnName("deleted_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();

        // Optimistic concurrency via the Postgres system xmin column.
        builder.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
