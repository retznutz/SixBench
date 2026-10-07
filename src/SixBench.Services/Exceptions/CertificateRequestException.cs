namespace SixBench.Services.Exceptions;

/// <summary>
/// Thrown when Let's Encrypt does not issue a certificate, or it cannot be installed. Mapped to HTTP 502.
/// </summary>
public sealed class CertificateRequestException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">What went wrong, suitable for showing to the user.</param>
    /// <param name="inner">The underlying error.</param>
    /// <param name="orderStillValid">True if the failure was transient (e.g. a network error) and the same order can be retried.</param>
    public CertificateRequestException(string message, Exception? inner = null, bool orderStillValid = false)
        : base(message, inner)
    {
        OrderStillValid = orderStillValid;
    }

    /// <summary>True if the same order can be retried; false if a new order (and TXT value) is needed.</summary>
    public bool OrderStillValid { get; }
}
