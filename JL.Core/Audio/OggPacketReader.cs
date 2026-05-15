using System.Buffers;

namespace JL.Core.Audio;

internal sealed class OggPacketReader : IDisposable
{
    private static ReadOnlySpan<byte> CapturePattern => "OggS"u8;

    // Ogg page header layout (per RFC 3533):
    // Bytes  0–3:  capture pattern "OggS"
    // Byte   5:    header type flags
    // Byte   26:   segment count (how many segments are in this page)
    // Bytes  27+:  segment size table (one byte per segment)
    private const int PageHeaderFixedSize = 27;
    private const int HeaderTypeFlagOffset = 5;
    private const int SegmentCountOffset = 26;
    private const int SegmentTableOffset = PageHeaderFixedSize; // immediately follows the fixed header

    // If this bit is set in the header type flags, the first packet on this page
    // is a continuation of the last packet from the previous page.
    private const byte ContinuationFlag = 0x01;

    // Ogg: max 255 segments per page × max 255 bytes per segment.
    private const int MaxPagePayloadSize = 255 * 255;

    private readonly Stream _stream;
    // Holds the fixed page header (27 bytes) + segment size table (up to 255 bytes).
    private readonly byte[] _pageHeader = new byte[PageHeaderFixedSize + 255];

    private readonly byte[] _pageBuffer;
    private readonly byte[] _packetBuffer;
    private int _packetLength;

    private int _segmentCount;
    private int _segmentIndex;
    private int _pageOffset;
    private bool _isPacketContinued;

    public OggPacketReader(Stream stream)
    {
        _stream = stream;
        _pageBuffer = ArrayPool<byte>.Shared.Rent(MaxPagePayloadSize);
        _packetBuffer = ArrayPool<byte>.Shared.Rent(MaxPagePayloadSize);
    }

    public bool TryReadPacket(out Span<byte> packet)
    {
        _packetLength = 0;
        while (true)
        {
            if (_segmentIndex >= _segmentCount)
            {
                if (!ReadPage())
                {
                    packet = [];
                    return false;
                }

                if (_isPacketContinued)
                {
                    if (_packetLength is 0)
                    {
                        // We started reading mid-stream with no data from the packet's earlier
                        // pages. Skip to the end of this continued packet before reading the next.
                        SkipContinuationSegments();
                        continue;
                    }
                }
                else
                {
                    // New page starts fresh; discard any partially accumulated data
                    // from an incomplete packet on the previous page.
                    _packetLength = 0;
                }
            }

            int segmentSize = _pageHeader[SegmentTableOffset + _segmentIndex];

            // Fast path: the entire packet fits in one segment. A segment shorter than 255 bytes
            // signals the last (or only) segment of a packet. Return a span directly into
            // _pageBuffer to avoid a copy.
            // IMPORTANT: this span is invalidated by the next call to TryReadPacket, which
            // overwrites _pageBuffer via ReadPage. The caller must consume it before calling
            // TryReadPacket again.
            if (!_isPacketContinued && _packetLength is 0 && segmentSize < 255)
            {
                packet = _pageBuffer.AsSpan(_pageOffset, segmentSize);
                _pageOffset += segmentSize;
                ++_segmentIndex;
                return true;
            }

            // Slow path: the packet spans multiple segments (and possibly multiple pages).
            // Accumulate into _packetBuffer until we find a segment shorter than 255 bytes,
            // which marks the end of the packet.
            while (_segmentIndex < _segmentCount)
            {
                segmentSize = _pageHeader[SegmentTableOffset + _segmentIndex];
                ++_segmentIndex;

                if (segmentSize > 0)
                {
                    if (_packetLength + segmentSize > _packetBuffer.Length)
                    {
                        // Packet exceeds maximum expected size; stream is likely corrupt.
                        packet = [];
                        return false;
                    }

                    _pageBuffer.AsSpan(_pageOffset, segmentSize).CopyTo(_packetBuffer.AsSpan(_packetLength));
                    _packetLength += segmentSize;
                    _pageOffset += segmentSize;
                }

                if (segmentSize < 255)
                {
                    packet = _packetBuffer.AsSpan(0, _packetLength);
                    _isPacketContinued = false;
                    return true;
                }
            }
        }
    }

    // Skips over segments until the end of the current continued packet.
    // A segment shorter than 255 bytes marks the packet boundary.
    private void SkipContinuationSegments()
    {
        while (_segmentIndex < _segmentCount)
        {
            int segSize = _pageHeader[SegmentTableOffset + _segmentIndex];
            ++_segmentIndex;
            _pageOffset += segSize;
            if (segSize < 255)
            {
                break;
            }
        }
    }

    private bool ReadPage()
    {
        if (!TryReadExactly(_pageHeader.AsSpan(0, PageHeaderFixedSize)))
        {
            return false;
        }

        if (!_pageHeader.AsSpan(0, 4).SequenceEqual(CapturePattern))
        {
            return false;
        }

        _isPacketContinued = (_pageHeader[HeaderTypeFlagOffset] & ContinuationFlag) is not 0;

        int segmentCount = _pageHeader[SegmentCountOffset];
        if (segmentCount > 0)
        {
            if (!TryReadExactly(_pageHeader.AsSpan(PageHeaderFixedSize, segmentCount)))
            {
                return false;
            }
        }

        _segmentCount = segmentCount;
        _segmentIndex = 0;
        _pageOffset = 0;

        int totalSize = 0;
        ReadOnlySpan<byte> sizes = _pageHeader.AsSpan(PageHeaderFixedSize, segmentCount);
        foreach (byte size in sizes)
        {
            totalSize += size;
        }

        if (totalSize > 0)
        {
            if (!TryReadExactly(_pageBuffer.AsSpan(0, totalSize)))
            {
                return false;
            }
        }

        return true;
    }

    // Reads exactly buffer.Length bytes into buffer.
    // Returns false if the stream ends before the buffer is filled (clean EOF or truncated data).
    private bool TryReadExactly(Span<byte> buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = _stream.Read(buffer[totalRead..]);
            if (read is 0)
            {
                return false;
            }
            totalRead += read;
        }
        return true;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_pageBuffer);
        ArrayPool<byte>.Shared.Return(_packetBuffer);
        _stream.Dispose();
    }
}
