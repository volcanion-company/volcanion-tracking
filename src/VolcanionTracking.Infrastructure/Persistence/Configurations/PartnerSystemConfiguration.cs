using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolcanionTracking.Domain.Aggregates.PartnerAggregate;
using VolcanionTracking.Domain.ValueObjects;

namespace VolcanionTracking.Infrastructure.Persistence.Configurations;

public class PartnerSystemConfiguration : IEntityTypeConfiguration<PartnerSystem>
{
    public void Configure(EntityTypeBuilder<PartnerSystem> builder)
    {
        builder.ToTable("PartnerSystems", "write");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Id)
            .ValueGeneratedNever();

        builder.Property(ps => ps.PartnerId)
            .IsRequired();

        builder.Property(ps => ps.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ps => ps.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ps => ps.Description)
            .IsRequired()
            .HasMaxLength(1000);

        // Value Object conversion
        builder.OwnsOne(ps => ps.ApiKey, apiKey =>
        {
            apiKey.Property(a => a.Value)
                .HasColumnName("ApiKey")
                .IsRequired()
                .HasMaxLength(100);

            apiKey.HasIndex(a => a.Value)
                .IsUnique();
        });

        builder.Property(ps => ps.IsActive)
            .IsRequired();

        builder.Property(ps => ps.CreatedAt)
            .IsRequired();

        builder.Property(ps => ps.UpdatedAt);

        builder.Property(ps => ps.DeactivatedAt);

        builder.HasIndex(ps => ps.PartnerId);
        builder.HasIndex(ps => ps.IsActive);
    }
}
