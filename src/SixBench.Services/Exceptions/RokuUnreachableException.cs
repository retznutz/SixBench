namespace SixBench.Services.Exceptions;

/// <summary>
/// Thrown when a Roku does not respond to ECP requests. Mapped to HTTP 502.
/// </summary>
public sealed class RokuUnreachableException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="inner">The underlying transport error.</param>
    public RokuUnreachableException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
