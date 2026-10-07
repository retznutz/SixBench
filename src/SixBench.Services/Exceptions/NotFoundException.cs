namespace SixBench.Services.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist. Mapped to HTTP 404.
/// </summary>
/// <param name="message">Description of what was not found.</param>
public sealed class NotFoundException(string message) : Exception(message);
