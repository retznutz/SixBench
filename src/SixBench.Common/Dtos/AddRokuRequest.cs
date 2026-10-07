using System.ComponentModel.DataAnnotations;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body to add a Roku manually by IP address.
/// </summary>
public sealed class AddRokuRequest
{
    /// <summary>IPv4 address or host name of the Roku.</summary>
    [Required, StringLength(255, MinimumLength = 1)]
    public string Host { get; set; } = string.Empty;

    /// <summary>ECP port; defaults to 8060.</summary>
    [Range(1, 65535)]
    public int? Port { get; set; }
}
