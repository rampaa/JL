using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using NAudio.Wave;
using OpusSharp.Core.Dynamic;

namespace JL.Core.Audio;

public sealed class OpusWaveStream : WaveStream
{
    private static readonly WaveFormat s_waveFormat = new(48000, 16, 2);
    private const int Channels = 2;

    // Maximum Opus frame duration is 120 ms; at 48 kHz that is 5760 samples per channel.
    private const int MaxSamplesPerChannel = 5760;

    private const int MaxSamplesTotal = MaxSamplesPerChannel * Channels;

    // Opus ID header field offsets (all values little-endian per RFC 7845):
    // Bytes 10–11: pre-skip (samples to discard at stream start due to decoder warm-up)
    private const int PreSkipOffset = 10;
    private const int OpusIdHeaderMinSize = 12;

    private readonly OggPacketReader _reader;
    private readonly OpusDecoder _decoder;
    private readonly short[] _decodeBuffer;

    private int _decodedOffset;
    private int _decodedCount;
    private long _position;
    private int _preSkip;

    public OpusWaveStream(Stream sourceStream)
    {
        _reader = new OggPacketReader(sourceStream);
        _decoder = new OpusDecoder(48000, Channels);
        _decodeBuffer = ArrayPool<short>.Shared.Rent(MaxSamplesTotal);

        Initialize();
    }

    private void Initialize()
    {
        // The first Ogg packet is the Opus ID header, which contains encoder settings.
        // Pre-skip is the number of samples the decoder outputs before reaching actual audio
        // content (decoder warm-up); these leading samples must be discarded.
        if (_reader.TryReadPacket(out Span<byte> head) && head.Length >= OpusIdHeaderMinSize)
        {
            _preSkip = BinaryPrimitives.ReadUInt16LittleEndian(head.Slice(PreSkipOffset, 2)) * Channels;
        }

        // The second Ogg packet is the Opus comment header (metadata/tags). Skip it.
        _ = _reader.TryReadPacket(out _);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        Span<byte> output = buffer.AsSpan(offset, count);
        int totalBytesRead = 0;

        while (totalBytesRead < output.Length)
        {
            if (_decodedCount > 0)
            {
                ReadOnlySpan<byte> currentDecoded = MemoryMarshal.AsBytes(_decodeBuffer.AsSpan(_decodedOffset, _decodedCount));

                int bytesToCopy = Math.Min(currentDecoded.Length, output.Length - totalBytesRead);

                currentDecoded[..bytesToCopy].CopyTo(output[totalBytesRead..]);

                totalBytesRead += bytesToCopy;
                int samplesConsumed = bytesToCopy / 2;
                _decodedOffset += samplesConsumed;
                _decodedCount -= samplesConsumed;
                continue;
            }

            if (!_reader.TryReadPacket(out Span<byte> packet))
            {
                break;
            }

            int samplesRead = _decoder.Decode(packet, packet.Length, _decodeBuffer, MaxSamplesPerChannel, false);
            if (samplesRead <= 0)
            {
                continue;
            }

            _decodedOffset = 0;
            _decodedCount = samplesRead * Channels;

            if (_preSkip > 0)
            {
                int skip = Math.Min(_preSkip, _decodedCount);
                _decodedOffset += skip;
                _decodedCount -= skip;
                _preSkip -= skip;
            }
        }

        _position += totalBytesRead;
        return totalBytesRead;
    }

    // Ogg is a streaming format with no index; total length is not known until the stream
    // is fully consumed. long.MaxValue signals to NAudio that this stream is unbounded.
    public override long Length => long.MaxValue;

    public override WaveFormat WaveFormat => s_waveFormat;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException("Seeking is not supported.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reader.Dispose();
            _decoder.Dispose();
            ArrayPool<short>.Shared.Return(_decodeBuffer);
        }
        base.Dispose(disposing);
    }
}
