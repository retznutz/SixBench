using System.Buffers.Binary;

namespace SixBench.Common.Streaming;

/// <summary>
/// The 12-byte header that prefixes every audio packet on the stream socket.
/// </summary>
/// <remarks>
/// Layout (big-endian):
/// <code>
/// 0..1  'V' 'A'    magic
/// 2     flags      bit0 = mic/upstream, bit1 = discontinuity
/// 3     reserved   0
/// 4..7  sequence   uint32
/// 8..11 timestamp  uint32 milliseconds since the session started
/// </code>
/// Video frames have no header; Annex-B data always begins with <c>00 00</c>, so it never collides with the magic.
/// </remarks>
/// <param name="Flags">Header flags.</param>
/// <param name="Sequence">Monotonic packet sequence number.</param>
/// <param name="TimestampMs">Milliseconds since the session started.</param>
public readonly record struct AudioFrameHeader(AudioFrameFlags Flags, uint Sequence, uint TimestampMs)
{
    /// <summary>Header length in bytes.</summary>
    public const int Size = 12;

    /// <summary>First magic byte ('V').</summary>
    public const byte Magic0 = (byte)'V';

    /// <summary>Second magic byte ('A').</summary>
    public const byte Magic1 = (byte)'A';

    /// <summary>
    /// Returns true when <paramref name="data"/> starts with the audio magic bytes.
    /// </summary>
    /// <param name="data">A received binary message.</param>
    /// <returns>True if the message is an audio frame.</returns>
    public static bool HasMagic(ReadOnlySpan<byte> data) =>
        data.Length >= Size && data[0] == Magic0 && data[1] == Magic1;

    /// <summary>
    /// Writes the header into the first <see cref="Size"/> bytes of <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">Target buffer, at least <see cref="Size"/> bytes.</param>
    /// <exception cref="ArgumentException">The buffer is too small.</exception>
    public void Write(Span<byte> destination)
    {
        if (destination.Length < Size)
        {
            throw new ArgumentException($"Destination must be at least {Size} bytes.", nameof(destination));
        }

        destination[0] = Magic0;
        destination[1] = Magic1;
        destination[2] = (byte)Flags;
        destination[3] = 0;
        BinaryPrimitives.WriteUInt32BigEndian(destination[4..8], Sequence);
        BinaryPrimitives.WriteUInt32BigEndian(destination[8..12], TimestampMs);
    }

    /// <summary>
    /// Attempts to parse a header from the start of <paramref name="source"/>.
    /// </summary>
    /// <param name="source">A received binary message.</param>
    /// <param name="header">The parsed header when successful.</param>
    /// <returns>True if the magic matched and the buffer was long enough.</returns>
    public static bool TryRead(ReadOnlySpan<byte> source, out AudioFrameHeader header)
    {
        if (!HasMagic(source))
        {
            header = default;
            return false;
        }

        header = new AudioFrameHeader(
            (AudioFrameFlags)source[2],
            BinaryPrimitives.ReadUInt32BigEndian(source[4..8]),
            BinaryPrimitives.ReadUInt32BigEndian(source[8..12]));
        return true;
    }

    /// <summary>
    /// Allocates a buffer containing this header followed by <paramref name="payload"/>.
    /// </summary>
    /// <param name="payload">The Opus packet.</param>
    /// <returns>The framed message.</returns>
    public byte[] Frame(ReadOnlySpan<byte> payload)
    {
        var buffer = new byte[Size + payload.Length];
        Write(buffer);
        payload.CopyTo(buffer.AsSpan(Size));
        return buffer;
    }
}

/// <summary>
/// Flags carried in <see cref="AudioFrameHeader"/>.
/// </summary>
[Flags]
public enum AudioFrameFlags : byte
{
    /// <summary>No flags; downstream device audio.</summary>
    None = 0,
    /// <summary>The packet is upstream microphone audio.</summary>
    Mic = 1 << 0,
    /// <summary>The packet follows a gap (pipeline restart); the receiver should reset timing.</summary>
    Discontinuity = 1 << 1,
}
