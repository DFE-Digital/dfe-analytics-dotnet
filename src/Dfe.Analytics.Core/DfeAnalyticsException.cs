namespace Dfe.Analytics;

/// <summary>
/// The error that is thrown when DfE analytics fails.
/// </summary>
public class DfeAnalyticsException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DfeAnalyticsException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DfeAnalyticsException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DfeAnalyticsException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
    /// if no inner exception is specified.</param>
    public DfeAnalyticsException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
