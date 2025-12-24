using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;

namespace VolcanionTracking.Infrastructure.Persistence.Configurations;

public class TrackingEventConfiguration : IEntityTypeConfiguration<TrackingEvent>
{
    public void Configure(EntityTypeBuilder<TrackingEvent> builder)
    {
        builder.ToTable("TrackingEvents", "write");

        builder.HasKey(te => te.Id);

        builder.Property(te => te.Id)
            .ValueGeneratedNever();

        builder.Property(te => te.PartnerSystemId)
            .IsRequired();

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

        // JSONB column for PostgreSQL
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

        builder.Property(te => te.CreatedAt)
            .IsRequired();

        // Indexes for query performance
        builder.HasIndex(te => te.PartnerSystemId);
        builder.HasIndex(te => te.EventName);
        builder.HasIndex(te => te.EventTimestamp);
        builder.HasIndex(te => te.CorrelationId);
        builder.HasIndex(te => new { te.PartnerSystemId, te.EventTimestamp });

        // Ignore domain events
        builder.Ignore(te => te.DomainEvents);
    }
}
