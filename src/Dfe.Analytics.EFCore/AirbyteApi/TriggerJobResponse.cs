namespace Dfe.Analytics.EFCore.AirbyteApi;

public record TriggerJobResponse
{
    public required long JobId { get; set; }
    public required JobStatus Status { get; set; }
    public required JobType JobType { get; set; }
}
