using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;

namespace VolcanionTracking.Infrastructure.Persistence.Configurations;

public class TrackingEventReadModelConfiguration : IEntityTypeConfiguration<TrackingEventReadModel>
{
    public void Configure(EntityTypeBuilder<TrackingEventReadModel> builder)
    {
        builder.ToTable("TrackingEventsReadModel", "read");

        builder.HasKey(te => te.Id);

        builder.Property(te => te.Id)
            .ValueGeneratedNever();

        builder.Property(te => te.TrackingEventId)
            .IsRequired();

        builder.Property(te => te.PartnerSystemId)
            .IsRequired();

        builder.Property(te => te.PartnerId)
            .IsRequired();

        builder.Property(te => te.PartnerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(te => te.SystemName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(te => te.EventName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(te => te.EventTimestamp)
            .IsRequired();

        builder.Property(te => te.UserId)
            .HasMaxLength(255);

        builder.Property(te => te.AnonymousId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(te => te.EventPropertiesJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(te => te.IsValid)
            .IsRequired();

        builder.Property(te => te.ValidationErrors)
            .HasMaxLength(2000);

        builder.Property(te => te.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(te => te.ProcessedAt)
            .IsRequired();

        builder.Property(te => te.CreatedAt)
            .IsRequired();

        // Optimized indexes for analytics queries
        builder.HasIndex(te => te.TrackingEventId).IsUnique();
        builder.HasIndex(te => te.PartnerSystemId);
        builder.HasIndex(te => te.PartnerId);
        builder.HasIndex(te => te.EventName);
        builder.HasIndex(te => te.EventTimestamp);
        builder.HasIndex(te => te.UserId);
        builder.HasIndex(te => te.AnonymousId);
        builder.HasIndex(te => new { te.PartnerId, te.EventTimestamp });
        builder.HasIndex(te => new { te.PartnerSystemId, te.EventTimestamp });
        builder.HasIndex(te => new { te.EventName, te.EventTimestamp });

        // Ignore domain events
        //builder.Ignore(te => te.DomainEvents);
    }
}
