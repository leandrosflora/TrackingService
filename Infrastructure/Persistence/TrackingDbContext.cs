using Microsoft.EntityFrameworkCore;
using TrackingService.Domain;
using TrackingService.Infrastructure.Outbox;

namespace TrackingService.Infrastructure.Persistence;

public sealed class TrackingDbContext : DbContext
{
    public TrackingDbContext(DbContextOptions<TrackingDbContext> options) : base(options)
    {
    }

    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();
    public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackingEvent>(entity =>
        {
            entity.ToTable("tracking_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ShipmentId).HasColumnName("shipment_id");
            entity.Property(x => x.ProviderEventId).HasColumnName("provider_event_id").HasMaxLength(200).IsRequired();
            entity.Property(x => x.TrackingCode).HasColumnName("tracking_code").HasMaxLength(200).IsRequired();
            entity.Property(x => x.CarrierCode).HasColumnName("carrier_code").HasMaxLength(80).IsRequired();
            entity.Property(x => x.CarrierSequence).HasColumnName("carrier_sequence");
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(x => x.ExceptionCode).HasColumnName("exception_code").HasMaxLength(100);
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at");
            entity.Property(x => x.EstimatedDeliveryDate).HasColumnName("estimated_delivery_date");

            entity.HasIndex(x => new { x.CarrierCode, x.ProviderEventId }).IsUnique();
            entity.HasIndex(x => new { x.ShipmentId, x.OccurredAt });

            entity.OwnsOne(x => x.Location, location =>
            {
                location.Property(x => x.FacilityCode).HasColumnName("facility_code").HasMaxLength(100);
                location.Property(x => x.City).HasColumnName("location_city").HasMaxLength(200);
                location.Property(x => x.State).HasColumnName("location_state").HasMaxLength(100);
                location.Property(x => x.Country).HasColumnName("location_country").HasMaxLength(10);
            });
        });

        modelBuilder.Entity<ShipmentTracking>(entity =>
        {
            entity.ToTable("shipment_tracking");
            entity.HasKey(x => x.ShipmentId);
            entity.Property(x => x.ShipmentId).HasColumnName("shipment_id");
            entity.Property(x => x.TrackingCode).HasColumnName("tracking_code").HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.TrackingCode).IsUnique();
            entity.Property(x => x.CarrierCode).HasColumnName("carrier_code").HasMaxLength(80).IsRequired();
            entity.Property(x => x.CurrentStatus).HasColumnName("current_status").HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.LastEventId).HasColumnName("last_event_id");
            entity.Property(x => x.LastCarrierSequence).HasColumnName("last_carrier_sequence");
            entity.Property(x => x.LastEventOccurredAt).HasColumnName("last_event_occurred_at");
            entity.Property(x => x.LastEventReceivedAt).HasColumnName("last_event_received_at");
            entity.Property(x => x.EstimatedDeliveryDate).HasColumnName("estimated_delivery_date");
            entity.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(x => x.CurrentExceptionCode).HasColumnName("current_exception_code").HasMaxLength(100);
            entity.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.OwnsOne(x => x.LastLocation, location =>
            {
                location.Property(x => x.FacilityCode).HasColumnName("last_facility_code").HasMaxLength(100);
                location.Property(x => x.City).HasColumnName("last_location_city").HasMaxLength(200);
                location.Property(x => x.State).HasColumnName("last_location_state").HasMaxLength(100);
                location.Property(x => x.Country).HasColumnName("last_location_country").HasMaxLength(10);
            });
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inbox_messages");
            entity.HasKey(x => x.MessageId);
            entity.Property(x => x.MessageId).HasColumnName("message_id");
            entity.Property(x => x.MessageType).HasColumnName("message_type").HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(200).IsRequired();
            entity.Property(x => x.MessageType).HasColumnName("message_type").HasMaxLength(200).IsRequired();
            entity.Property(x => x.AggregateKey).HasColumnName("aggregate_key").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            entity.Property(x => x.Attempts).HasColumnName("attempts");
            entity.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
            entity.HasIndex(x => new { x.ProcessedAt, x.NextAttemptAt, x.CreatedAt });
        });
    }
}
