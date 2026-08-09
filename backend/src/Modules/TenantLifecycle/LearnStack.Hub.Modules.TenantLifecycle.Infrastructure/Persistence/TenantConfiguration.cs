using LearnStack.Hub.Modules.TenantLifecycle.Domain;
using LearnStack.Hub.SharedKernel.Hosting;
using LearnStack.Hub.SharedKernel.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnStack.Hub.Modules.TenantLifecycle.Infrastructure.Persistence;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<LearnStackTenant>
{
    public void Configure(EntityTypeBuilder<LearnStackTenant> builder)
    {
        builder.ToTable("tenants", t =>
        {
            t.HasCheckConstraint(
                "ck_tenants_status",
                "status IN ('Trial','Active','Suspended','Archived','Terminated')");
            t.HasCheckConstraint(
                "ck_tenants_deployment_mode",
                "deployment_mode IN ('SaaS','Dedicated','SelfHostedOnline','SelfHostedAirGapped')");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion<LearnStackTenantId.EfCoreValueConverter>();

        builder.Property(t => t.Slug).HasColumnName("slug").IsRequired();
        builder.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("ux_tenants_slug");

        builder.Property(t => t.DisplayName).HasColumnName("display_name").IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion(s => s.ToString(), v => Enum.Parse<TenantStatus>(v))
            .IsRequired();

        builder.Property(t => t.DeploymentMode)
            .HasColumnName("deployment_mode")
            .HasConversion(m => m.ToString(), v => Enum.Parse<DeploymentMode>(v))
            .IsRequired();

        builder.Property(t => t.LastPhoneHomeAt).HasColumnName("last_phone_home_at");

        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy)
            .HasColumnName("updated_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy)
            .HasColumnName("deleted_by")
            .HasConversion<OperatorId.EfCoreValueConverter>();

        builder.Property(t => t.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
