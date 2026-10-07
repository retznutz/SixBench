using System.Buffers;

namespace SixBench.Services.Streaming;

/// <summary>
/// Incrementally splits an H.264 Annex-B byte stream into access units.
/// </summary>
/// <remarks>
/// A new access unit begins at an AUD, SPS, PPS or SEI NAL, or at a slice whose <c>first_mb_in_slice</c> is 0,
/// once the current unit already contains a picture slice. Because a unit is only known to be complete when
/// the next one starts, output lags input by one frame.
/// The latest SPS/PPS are cached and inserted ahead of any IDR that lacks them, so every keyframe
/// unit can start a fresh decoder.
/// </remarks>
public sealed class AnnexBParser
{
    private const int NalAud = 9;
    private const int NalSps = 7;
    private const int NalPps = 8;
    private const int NalSei = 6;
    private const int NalIdr = 5;
    private const int NalSlice = 1;

    private readonly ArrayBufferWriter<byte> _au = new(256 * 1024);
    private byte[] _buffer = new byte[256 * 1024];
    private int _length;
    private int _scan;
    private int _nalStart = -1;

    private bool _auHasVcl;
    private bool _auHasIdr;
    private bool _auHasSps;
    private int _auAudLength;
    private byte[]? _sps;
    private byte[]? _pps;

    /// <summary>
    /// Appends bytes and returns any access units completed by them.
    /// </summary>
    /// <param name="data">The next chunk of the stream.</param>
    /// <returns>Completed access units, in order.</returns>
    public List<AccessUnit> Push(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(_length + data.Length);
        data.CopyTo(_buffer.AsSpan(_length));
        _length += data.Length;

        var output = new List<AccessUnit>();
        var i = _scan;
        while (i + 3 <= _length)
        {
            // If byte i+2 is greater than 1, no start code can begin at i, i+1 or i+2.
            if (_buffer[i + 2] > 1)
            {
                i += 3;
                continue;
            }

            if (_buffer[i] == 0 && _buffer[i + 1] == 0 && _buffer[i + 2] == 1)
            {
                var start = i > 0 && _buffer[i - 1] == 0 && (_nalStart < 0 || i - 1 >= _nalStart + 3) ? i - 1 : i;
                if (_nalStart >= 0)
                {
                    HandleNal(_buffer.AsSpan(_nalStart, start - _nalStart), output);
                }

                _nalStart = start;
                i += 3;
            }
            else
            {
                i++;
            }
        }

        _scan = i;
        Compact();
        return output;
    }

    /// <summary>
    /// Completes the final NAL unit and access unit at end of stream.
    /// </summary>
    /// <returns>The last access unit, if any.</returns>
    public List<AccessUnit> Flush()
    {
        var output = new List<AccessUnit>();
        if (_nalStart >= 0)
        {
            HandleNal(_buffer.AsSpan(_nalStart, _length - _nalStart), output);
            _nalStart = -1;
        }

        EmitAccessUnit(output);
        _length = 0;
        _scan = 0;
        return output;
    }

    private void HandleNal(ReadOnlySpan<byte> nal, List<AccessUnit> output)
    {
        var headerIndex = nal.Length > 2 && nal[2] == 1 ? 3 : 4;
        if (nal.Length <= headerIndex)
        {
            return;
        }

        var type = nal[headerIndex] & 0x1F;
        var isVcl = type is NalSlice or NalIdr;
        var firstMbZero = isVcl && (nal.Length <= headerIndex + 1 || (nal[headerIndex + 1] & 0x80) != 0);
        var startsNewUnit = _auHasVcl
            && (type is NalAud or NalSps or NalPps or NalSei or (>= 14 and <= 18) || firstMbZero);

        if (startsNewUnit)
        {
            EmitAccessUnit(output);
        }

        switch (type)
        {
            case NalSps:
                _sps = nal.ToArray();
                _auHasSps = true;
                break;
            case NalPps:
                _pps = nal.ToArray();
                break;
            case NalIdr:
                _auHasIdr = true;
                break;
            case NalAud when _au.WrittenCount == 0:
                _auAudLength = nal.Length;
                break;
        }

        _auHasVcl |= isVcl;
        _au.Write(nal);
    }

    private void EmitAccessUnit(List<AccessUnit> output)
    {
        if (_au.WrittenCount == 0)
        {
            return;
        }

        byte[] data;
        if (_auHasIdr && !_auHasSps && _sps is not null && _pps is not null)
        {
            var written = _au.WrittenSpan;
            data = new byte[written.Length + _sps.Length + _pps.Length];
            var span = data.AsSpan();
            written[.._auAudLength].CopyTo(span);
            _sps.CopyTo(span[_auAudLength..]);
            _pps.CopyTo(span[(_auAudLength + _sps.Length)..]);
            written[_auAudLength..].CopyTo(span[(_auAudLength + _sps.Length + _pps.Length)..]);
        }
        else
        {
            data = _au.WrittenSpan.ToArray();
        }

        if (_auHasVcl)
        {
            output.Add(new AccessUnit(data, _auHasIdr));
        }

        _au.ResetWrittenCount();
        _auHasVcl = false;
        _auHasIdr = false;
        _auHasSps = false;
        _auAudLength = 0;
    }

    private void Compact()
    {
        var keepFrom = _nalStart >= 0 ? _nalStart : Math.Max(0, _length - 3);
        if (keepFrom <= 0)
        {
            return;
        }

        Buffer.BlockCopy(_buffer, keepFrom, _buffer, 0, _length - keepFrom);
        _length -= keepFrom;
        _scan = Math.Max(0, _scan - keepFrom);
        if (_nalStart >= 0)
        {
            _nalStart -= keepFrom;
        }
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length)
        {
            return;
        }

        Array.Resize(ref _buffer, Math.Max(required, _buffer.Length * 2));
    }
}
