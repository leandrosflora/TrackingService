namespace TrackingService.Domain;

public sealed class TrackingLocation
{
    public string? FacilityCode { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }

    private TrackingLocation()
    {
    }

    public TrackingLocation(string? facilityCode, string? city, string? state, string? country)
    {
        FacilityCode = facilityCode;
        City = city;
        State = state;
        Country = country;
    }
}
