using System.IO;
using System.Runtime.CompilerServices;

namespace JL.Windows.Images;

internal ref struct BufferedImageReader
{
    private readonly FileStream _fileStream;
    private readonly Span<byte> _buffer;

    private long _bufferStart;
    private long _streamPosition;
    private int _bufferOffset;
    private int _bufferLength;

    public BufferedImageReader(FileStream fileStream, Span<byte> buffer)
    {
        _fileStream = fileStream;
        _buffer = buffer;
    }

    public readonly long Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bufferStart + _bufferOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySeek(long position)
    {
        if (position < 0)
        {
            return false;
        }

        long bufferEnd = _bufferStart + _bufferLength;
        if (position >= _bufferStart && position <= bufferEnd)
        {
            _bufferOffset = (int)(position - _bufferStart);
            return true;
        }

        _bufferStart = position;
        _bufferOffset = 0;
        _bufferLength = 0;

        if (position != _streamPosition)
        {
            _ = _fileStream.Seek(position, SeekOrigin.Begin);
            _streamPosition = position;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadByte(out byte value)
    {
        if (_bufferOffset >= _bufferLength
            && !FillBuffer())
        {
            value = 0;
            return false;
        }

        value = _buffer[_bufferOffset++];
        return true;
    }

    public bool TryReadExactly(scoped Span<byte> destination)
    {
        int availableByteCount = _bufferLength - _bufferOffset;
        if (destination.Length <= availableByteCount)
        {
            _buffer.Slice(_bufferOffset, destination.Length).CopyTo(destination);
            _bufferOffset += destination.Length;
            return true;
        }

        int destinationOffset = 0;

        while (destinationOffset < destination.Length)
        {
            if (_bufferOffset >= _bufferLength
                && !FillBuffer())
            {
                return false;
            }

            int bytesToCopy = Math.Min(destination.Length - destinationOffset, _bufferLength - _bufferOffset);
            _buffer.Slice(_bufferOffset, bytesToCopy).CopyTo(destination[destinationOffset..]);

            _bufferOffset += bytesToCopy;
            destinationOffset += bytesToCopy;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySkip(long byteCount)
    {
        if (byteCount < 0)
        {
            return false;
        }

        int bufferedByteCount = _bufferLength - _bufferOffset;
        if (byteCount <= bufferedByteCount)
        {
            _bufferOffset += (int)byteCount;
            return true;
        }

        long position = _bufferStart + _bufferOffset;
        return byteCount <= long.MaxValue - position && TrySeek(position + byteCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool FillBuffer()
    {
        _bufferStart = _streamPosition;
        _bufferOffset = 0;
        _bufferLength = _fileStream.Read(_buffer);
        _streamPosition += _bufferLength;
        return _bufferLength > 0;
    }
}
