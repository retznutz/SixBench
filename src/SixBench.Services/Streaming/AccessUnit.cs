namespace SixBench.Services.Streaming;

/// <summary>
/// One H.264 access unit (a complete coded picture) in Annex-B format, starting with its start code.
/// </summary>
/// <param name="Data">Annex-B bytes for every NAL unit in the access unit.</param>
/// <param name="IsKeyframe">True when the access unit contains an IDR slice (decoding can start here).</param>
public sealed record AccessUnit(byte[] Data, bool IsKeyframe);
