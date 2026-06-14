namespace TrackingService.Infrastructure.Persistence;

public sealed class MockTrackingRepositoryOptions
{
    public const string SectionName = "FeatureFlags:MockTrackingRepository";

    public bool Enabled { get; set; }
}
