namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Used for modifying web request events before they are sent.
/// </summary>
public interface IWebRequestEventEnricher
{
    /// <summary>
    /// Enriches the provided <see cref="Event"/>.
    /// </summary>
    /// <param name="context">The <see cref="EnrichWebRequestEventContext"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the event modification.</returns>
    Task EnrichEvent(EnrichWebRequestEventContext context);
}
