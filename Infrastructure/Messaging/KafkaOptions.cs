namespace TrackingService.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "tracking-service";
    public KafkaTopicsOptions Topics { get; set; } = new();
}

public sealed class KafkaTopicsOptions
{
    public string ShipmentCreated { get; set; } = "shipment.created";
    public string ShipmentStatusUpdated { get; set; } = "shipment.status.updated";
}
