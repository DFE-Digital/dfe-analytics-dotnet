using System.ComponentModel.DataAnnotations;

namespace Dfe.Analytics.EFCore.AirbyteApi;

public class AirbyteApiOptions
{
    [Required]
    public required string BaseAddress { get; set; }

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string ClientSecret { get; set; }
}
