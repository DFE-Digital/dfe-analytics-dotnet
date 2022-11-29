namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// The error that is thrown when <see cref="DfeAnalyticsOptions"/> is mis-configured.
/// </summary>
public class DfeAnalyticsConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DfeAnalyticsConfigurationException"/> class with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DfeAnalyticsConfigurationException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DfeAnalyticsConfigurationException"/> class with a specified error
    /// message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
    /// if no inner exception is specified.</param>
    public DfeAnalyticsConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
