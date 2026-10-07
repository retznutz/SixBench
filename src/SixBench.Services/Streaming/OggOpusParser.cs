using System.Buffers;

namespace SixBench.Services.Streaming;

/// <summary>
/// Incrementally extracts Opus packets from an Ogg byte stream (as written by ffmpeg's ogg muxer).
/// The OpusHead and OpusTags header packets are skipped.
/// </summary>
public sealed class OggOpusParser
{
    private const int PageHeaderSize = 27;
    private static ReadOnlySpan<byte> CapturePattern => "OggS"u8;
    private static ReadOnlySpan<byte> OpusHead => "OpusHead"u8;
    private static ReadOnlySpan<byte> OpusTags => "OpusTags"u8;

    private readonly ArrayBufferWriter<byte> _packet = new(4096);
    private byte[] _buffer = new byte[64 * 1024];
    private int _length;

    /// <summary>
    /// Appends bytes and returns any Opus packets completed by them.
    /// </summary>
    /// <param name="data">The next chunk of the stream.</param>
    /// <returns>Completed Opus packets, in order.</returns>
    public List<byte[]> Push(ReadOnlySpan<byte> data)
    {
        if (_length + data.Length > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(_length + data.Length, _buffer.Length * 2));
        }

        data.CopyTo(_buffer.AsSpan(_length));
        _length += data.Length;

        var output = new List<byte[]>();
        var offset = 0;
        while (true)
        {
            var available = _buffer.AsSpan(offset, _length - offset);
            if (available.Length < PageHeaderSize)
            {
                break;
            }

            if (!available.StartsWith(CapturePattern))
            {
                // Lost sync: skip to the next capture pattern.
                var next = available[1..].IndexOf(CapturePattern);
                offset += next < 0 ? available.Length - 3 : next + 1;
                _packet.ResetWrittenCount();
                continue;
            }

            var segmentCount = available[26];
            if (available.Length < PageHeaderSize + segmentCount)
            {
                break;
            }

            var segments = available.Slice(PageHeaderSize, segmentCount);
            var bodyLength = 0;
            foreach (var s in segments)
            {
                bodyLength += s;
            }

            var pageLength = PageHeaderSize + segmentCount + bodyLength;
            if (available.Length < pageLength)
            {
                break;
            }

            var continued = (available[5] & 0x01) != 0;
            if (!continued)
            {
                _packet.ResetWrittenCount();
            }

            var body = available.Slice(PageHeaderSize + segmentCount, bodyLength);
            var position = 0;
            foreach (var s in segments)
            {
                _packet.Write(body.Slice(position, s));
                position += s;
                if (s < 255)
                {
                    EmitPacket(output);
                }
            }

            offset += pageLength;
        }

        if (offset > 0)
        {
            Buffer.BlockCopy(_buffer, offset, _buffer, 0, _length - offset);
            _length -= offset;
        }

        return output;
    }

    private void EmitPacket(List<byte[]> output)
    {
        var packet = _packet.WrittenSpan;
        if (packet.Length > 0 && !packet.StartsWith(OpusHead) && !packet.StartsWith(OpusTags))
        {
            output.Add(packet.ToArray());
        }

        _packet.ResetWrittenCount();
    }
}
