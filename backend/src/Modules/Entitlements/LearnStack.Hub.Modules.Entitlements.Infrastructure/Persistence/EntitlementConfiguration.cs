using LearnStack.Hub.Modules.Entitlements.Domain;
using LearnStack.Hub.SharedKernel.Compliance;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnStack.Hub.Modules.Entitlements.Infrastructure.Persistence;

internal sealed class EntitlementConfiguration : IEntityTypeConfiguration<Entitlement>
{
    public void Configure(EntityTypeBuilder<Entitlement> builder)
    {
        builder.ToTable("entitlements");

        // PK is the tenant id (no surrogate); Entity<LearnStackTenantId>.
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("tenant_id")
            .HasConversion<LearnStackTenantId.EfCoreValueConverter>();

        builder.Property(e => e.Tier).HasColumnName("tier").IsRequired();

        builder.Property<Dictionary<string, bool>>("_features")
            .HasColumnName("features")
            .HasColumnType("jsonb")
            .HasConversion(JsonbConversions.DictionaryConverter<bool>(), JsonbConversions.DictionaryComparer<bool>());

        builder.Property<Dictionary<string, long>>("_limits")
            .HasColumnName("limits")
            .HasColumnType("jsonb")
            .HasConversion(JsonbConversions.DictionaryConverter<long>(), JsonbConversions.DictionaryComparer<long>());

        builder.Property<Dictionary<string, ComplianceCap>>("_complianceCaps")
            .HasColumnName("compliance_caps")
            .HasColumnType("jsonb")
            .HasConversion(
                JsonbConversions.DictionaryConverter<ComplianceCap>(),
                JsonbConversions.DictionaryComparer<ComplianceCap>());

        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        builder.Property(e => e.GraceUntil).HasColumnName("grace_until");
        builder.Property(e => e.Generation).HasColumnName("generation");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
