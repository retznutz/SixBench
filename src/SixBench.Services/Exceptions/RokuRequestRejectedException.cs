namespace SixBench.Services.Exceptions;

/// <summary>
/// Thrown when a Roku answers but refuses a request (HTTP 403, or <c>&lt;status&gt;FAILED&lt;/status&gt;</c>),
/// typically because developer mode or "Control by mobile apps" is off. Mapped to HTTP 409.
/// Unlike <see cref="RokuUnreachableException"/>, this never triggers rediscovery.
/// </summary>
/// <param name="message">Why the Roku refused, phrased for the user.</param>
public sealed class RokuRequestRejectedException(string message) : Exception(message);
