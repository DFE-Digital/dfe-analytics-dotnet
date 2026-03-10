namespace Dfe.Analytics.EFCore.AirbyteApi;

public record TriggerJobRequest
{
    public required string ConnectionId { get; set; }
    public required JobType JobType { get; set; }
}
