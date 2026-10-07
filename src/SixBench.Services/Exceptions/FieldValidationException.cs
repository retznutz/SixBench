namespace SixBench.Services.Exceptions;

/// <summary>
/// Thrown when input fails validation that is only known to the service (for example the password policy).
/// Mapped to HTTP 400 with an <c>errors</c> dictionary, like model validation.
/// </summary>
/// <param name="errors">Field name → messages. Names are camel-cased to match the JSON request; an empty name is a general error.</param>
public sealed class FieldValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception(string.Join(" ", errors.SelectMany(e => e.Value)))
{
    /// <summary>Field name (camel case) → messages.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; } =
        errors.ToDictionary(e => e.Key.Length == 0 ? e.Key : char.ToLowerInvariant(e.Key[0]) + e.Key[1..], e => e.Value);
}
