namespace Dfe.Analytics;

/// <summary>
/// The error that is thrown when authentication fails.
/// </summary>
public class DfeAnalyticsAuthenticationException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DfeAnalyticsAuthenticationException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DfeAnalyticsAuthenticationException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DfeAnalyticsAuthenticationException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
    /// if no inner exception is specified.</param>
    public DfeAnalyticsAuthenticationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
