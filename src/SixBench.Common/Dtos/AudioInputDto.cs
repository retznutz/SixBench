namespace SixBench.Common.Dtos;

/// <summary>
/// An audio capture device that can be assigned to an encoder.
/// </summary>
/// <param name="Name">Device name.</param>
/// <param name="Input">ffmpeg input specifier.</param>
public sealed record AudioInputDto(string Name, string Input);
