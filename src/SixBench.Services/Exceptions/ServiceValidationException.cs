namespace SixBench.Services.Exceptions;

/// <summary>
/// Thrown when a request is well-formed but invalid for the current state. Mapped to HTTP 400.
/// </summary>
/// <param name="message">Description of the validation failure.</param>
public sealed class ServiceValidationException(string message) : Exception(message);
