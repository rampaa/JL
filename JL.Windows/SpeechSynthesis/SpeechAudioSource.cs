using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Channels;
using JL.Core.Utilities;
using JL.Windows.SpeechSynthesis.Interop;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using WinRTBuffer = Windows.Storage.Streams.Buffer;

namespace JL.Windows.SpeechSynthesis;

// SAPI writes audio synchronously while MediaPlayer requests it asynchronously.
// The bounded queue connects them and limits how much audio is kept ahead of playback.
[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
internal sealed class SpeechAudioSource : ISpeechAudioOutput, IDisposable
{
    private const int Success = 0;
    private const int AccessDenied = unchecked((int)0x80030005);
    private const int InvalidFunction = unchecked((int)0x80030001);
    private const int WriteAborted = unchecked((int)0x80004004);
    private const int BufferCapacity = 8;
    private readonly Channel<IBuffer> _queuedAudioBuffers = Channel.CreateBounded<IBuffer>(new BoundedChannelOptions(BufferCapacity) { SingleWriter = true });
    private readonly CancellationTokenSource _playbackCancellation = new();
    private readonly SpeechAudioFormat _engineFormat;
    private readonly SpeechAudioFormat _playbackFormat;
    private readonly int _bufferCapacityBytes;
    private WinRTBuffer? _bufferBeingFilled;
    private nint _bufferDataAddress;
    private int _bufferedByteCount;
    private byte[]? _incompleteFrame;
    private int _incompleteFrameByteCount;
    private bool _hasQueuedFirstBuffer;
    private long _receivedByteCount;
    private long _suppliedByteCount;
    private int _disposed;
    private int _remainingUsers = 2; // Both the synthesis worker and player must finish before the cancellation handle can be disposed.

    public CancellationToken CancellationToken { get; }
    public MediaStreamSource PlaybackSource { get; }

    public SpeechAudioSource(SpeechAudioFormat format, SpeechPlaybackChannels channels)
    {
        _engineFormat = format;
        short outputChannels = channels is SpeechPlaybackChannels.Stereo ? (short)2 : format.ChannelCount;
        Debug.Assert(channels is SpeechPlaybackChannels.EnginePreferred or SpeechPlaybackChannels.Stereo);
        if (outputChannels != format.ChannelCount && format.ChannelCount is not 1)
        {
            LoggerManager.Logger.Warning("Cannot convert {Channels}-channel speech to stereo; using the engine's format", format.ChannelCount);
            outputChannels = format.ChannelCount;
        }

        short bytesPerFrame = (short)(format.BitsPerSample / 8 * outputChannels);
        _playbackFormat = format with { ChannelCount = outputChannels, BytesPerFrame = bytesPerFrame, BytesPerSecond = format.SamplesPerSecond * bytesPerFrame };

        CancellationToken = _playbackCancellation.Token;
        const int bufferDurationMilliseconds = 250;
        int framesPerBuffer = format.SamplesPerSecond * bufferDurationMilliseconds / 1000;
        _bufferCapacityBytes = framesPerBuffer * bytesPerFrame;
        AudioStreamDescriptor descriptor = new(AudioEncodingProperties.CreatePcm((uint)format.SamplesPerSecond, (uint)outputChannels, (uint)format.BitsPerSample));
        PlaybackSource = new MediaStreamSource(descriptor) { CanSeek = false, BufferTime = TimeSpan.Zero };
        PlaybackSource.SampleRequested += MediaStreamSource_SampleRequested;
    }

    private async void MediaStreamSource_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        MediaStreamSourceSampleRequest request = args.Request;
        MediaStreamSourceSampleRequestDeferral deferral = request.GetDeferral();
        try
        {
            if (await _queuedAudioBuffers.Reader.WaitToReadAsync().ConfigureAwait(false)
                && _queuedAudioBuffers.Reader.TryRead(out IBuffer? buffer))
            {
                TimeSpan start = GetPlaybackTime(_suppliedByteCount);
                _suppliedByteCount += buffer.Length;
                MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(buffer, start);
                sample.Duration = GetPlaybackTime(_suppliedByteCount) - start;
                request.Sample = sample;
            }
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't supply speech audio to the player");
            if (!CancellationToken.IsCancellationRequested)
            {
                PlaybackSource.NotifyError(MediaStreamSourceErrorStatus.Other);
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TimeSpan GetPlaybackTime(long byteCount)
    {
        long frameCount = byteCount / _playbackFormat.BytesPerFrame;
        return TimeSpan.FromTicks(frameCount * TimeSpan.TicksPerSecond / _playbackFormat.SamplesPerSecond);
    }

    public unsafe int Write(nint inputDataAddress, int inputByteCount, nint bytesWrittenAddress)
    {
        if (bytesWrittenAddress is not 0)
        {
            Marshal.WriteInt32(bytesWrittenAddress, 0);
        }

        if (CancellationToken.IsCancellationRequested)
        {
            return WriteAborted;
        }

        int inputOffset = 0;
        if (_incompleteFrameByteCount is not 0)
        {
            int bytesToCompleteFrame = Math.Min(_engineFormat.BytesPerFrame - _incompleteFrameByteCount, inputByteCount);
            new ReadOnlySpan<byte>((void*)inputDataAddress, bytesToCompleteFrame).CopyTo(_incompleteFrame.AsSpan(_incompleteFrameByteCount));
            _incompleteFrameByteCount += bytesToCompleteFrame;
            inputOffset += bytesToCompleteFrame;
            if (_incompleteFrameByteCount == _engineFormat.BytesPerFrame)
            {
                fixed (byte* frame = _incompleteFrame)
                {
                    if (!WriteCompleteFrames((nint)frame, _engineFormat.BytesPerFrame))
                    {
                        return WriteAborted;
                    }
                }

                _incompleteFrameByteCount = 0;
            }
        }

        int completeFrameBytes = (inputByteCount - inputOffset) / _engineFormat.BytesPerFrame * _engineFormat.BytesPerFrame;
        if (!WriteCompleteFrames(inputDataAddress + inputOffset, completeFrameBytes))
        {
            return WriteAborted;
        }

        inputOffset += completeFrameBytes;
        if (inputOffset < inputByteCount)
        {
            _incompleteFrame ??= new byte[_engineFormat.BytesPerFrame];
            _incompleteFrameByteCount = inputByteCount - inputOffset;
            new ReadOnlySpan<byte>((void*)(inputDataAddress + inputOffset), _incompleteFrameByteCount).CopyTo(_incompleteFrame);
        }

        _receivedByteCount += inputByteCount;
        if (bytesWrittenAddress is not 0)
        {
            Marshal.WriteInt32(bytesWrittenAddress, inputByteCount);
        }

        return Success;
    }

    private unsafe bool WriteCompleteFrames(nint inputDataAddress, int inputByteCount)
    {
        int inputOffset = 0;
        while (inputOffset < inputByteCount)
        {
            if (_bufferBeingFilled is null)
            {
                _bufferBeingFilled = new WinRTBuffer((uint)_bufferCapacityBytes);
                _bufferDataAddress = SpeechSynthesisInterop.GetBufferDataAddress(_bufferBeingFilled);
            }

            int frameCount = Math.Min((_bufferCapacityBytes - _bufferedByteCount) / _playbackFormat.BytesPerFrame, (inputByteCount - inputOffset) / _engineFormat.BytesPerFrame);
            int inputBytesToCopy = frameCount * _engineFormat.BytesPerFrame;
            int outputBytesToWrite = frameCount * _playbackFormat.BytesPerFrame;
            if (_engineFormat.ChannelCount == _playbackFormat.ChannelCount)
            {
                System.Buffer.MemoryCopy((void*)(inputDataAddress + inputOffset), (void*)(_bufferDataAddress + _bufferedByteCount), outputBytesToWrite, inputBytesToCopy);
            }
            else
            {
                switch (_engineFormat.BitsPerSample)
                {
                    case 8:
                        DuplicateMonoSamplesToStereo<byte, ushort>(inputDataAddress + inputOffset, _bufferDataAddress + _bufferedByteCount, frameCount);
                        break;

                    case 16:
                        DuplicateMonoSamplesToStereo<ushort, uint>(inputDataAddress + inputOffset, _bufferDataAddress + _bufferedByteCount, frameCount);
                        break;

                    case 32:
                        DuplicateMonoSamplesToStereo<uint, ulong>(inputDataAddress + inputOffset, _bufferDataAddress + _bufferedByteCount, frameCount);
                        break;

                    default:
                        DuplicateMonoSamplesToStereo(inputDataAddress + inputOffset, _bufferDataAddress + _bufferedByteCount, frameCount, _engineFormat.BytesPerFrame);
                        break;
                }
            }

            _bufferedByteCount += outputBytesToWrite;
            inputOffset += inputBytesToCopy;
            if ((_bufferedByteCount == _bufferCapacityBytes || !_hasQueuedFirstBuffer) && !QueueFilledBuffer())
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void DuplicateMonoSamplesToStereo<TMonoSample, TStereoFrame>(nint monoDataAddress, nint stereoDataAddress, int sampleCount)
        where TMonoSample : unmanaged, IBinaryInteger<TMonoSample>
        where TStereoFrame : unmanaged, IBinaryInteger<TStereoFrame>
    {
        Debug.Assert(sizeof(TStereoFrame) == sizeof(TMonoSample) * 2);
        ReadOnlySpan<TMonoSample> monoSamples = new((void*)monoDataAddress, sampleCount);
        Span<TStereoFrame> stereoFrames = new((void*)stereoDataAddress, sampleCount);
        int bitsPerSample = sizeof(TMonoSample) * 8;
        // Multiplication places identical sample bits in both halves of the stereo frame, including sign bits.
        TStereoFrame duplicateSampleIntoBothChannels = TStereoFrame.One + (TStereoFrame.One << bitsPerSample);
        for (int i = 0; i < sampleCount; ++i)
        {
            stereoFrames[i] = TStereoFrame.CreateTruncating(monoSamples[i]) * duplicateSampleIntoBothChannels;
        }
    }

    private static unsafe void DuplicateMonoSamplesToStereo(nint monoDataAddress, nint stereoDataAddress, int sampleCount, int bytesPerSample)
    {
        int inputByteCount = sampleCount * bytesPerSample;
        ReadOnlySpan<byte> monoSamples = new((void*)monoDataAddress, inputByteCount);
        Span<byte> stereoSamples = new((void*)stereoDataAddress, inputByteCount * 2);
        for (int inputOffset = 0, outputOffset = 0; inputOffset < inputByteCount; inputOffset += bytesPerSample, outputOffset += bytesPerSample * 2)
        {
            ReadOnlySpan<byte> sample = monoSamples.Slice(inputOffset, bytesPerSample);
            sample.CopyTo(stereoSamples.Slice(outputOffset, bytesPerSample));
            sample.CopyTo(stereoSamples.Slice(outputOffset + bytesPerSample, bytesPerSample));
        }
    }

    private bool QueueFilledBuffer()
    {
        if (_bufferBeingFilled is null)
        {
            return true;
        }

        _bufferBeingFilled.Length = (uint)_bufferedByteCount;
        // SAPI's Write callback is synchronous; only its synthesis worker waits for playback to free space.
        while (!_queuedAudioBuffers.Writer.TryWrite(_bufferBeingFilled))
        {
            if (!_queuedAudioBuffers.Writer.WaitToWriteAsync().AsTask().GetAwaiter().GetResult())
            {
                return false;
            }
        }

        _bufferBeingFilled = null;
        _bufferedByteCount = 0;
        _hasQueuedFirstBuffer = true;
        return true;
    }

    public void CompleteSynthesis()
    {
        if (_incompleteFrameByteCount is not 0)
        {
            LoggerManager.Logger.Error("Speech synthesis ended with an incomplete PCM sample frame");
            ReportSynthesisFailure();
            return;
        }

        _ = QueueFilledBuffer();
        _ = _queuedAudioBuffers.Writer.TryComplete();
    }

    public void ReportSynthesisFailure()
    {
        _ = _queuedAudioBuffers.Writer.TryComplete();
        if (!CancellationToken.IsCancellationRequested)
        {
            PlaybackSource.NotifyError(MediaStreamSourceErrorStatus.Other);
        }
    }

    public void ReleaseSynthesisResources()
    {
        _bufferBeingFilled = null;
        _bufferDataAddress = 0;
        _incompleteFrame = null;
        ReleaseSharedResources();
    }

    private void ReleaseSharedResources()
    {
        if (Interlocked.Decrement(ref _remainingUsers) is 0)
        {
            _playbackCancellation.Dispose();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) is 0)
        {
            _playbackCancellation.Cancel();
            _ = _queuedAudioBuffers.Writer.TryComplete();
            while (_queuedAudioBuffers.Reader.TryRead(out _))
            {
                // Release queued native buffers without waiting for another player callback.
            }

            ReleaseSharedResources();
        }
    }

    public int Read(nint outputDataAddress, int byteCount, nint bytesReadAddress) => AccessDenied;

    public int Seek(long offset, int origin, nint newPosition)
    {
        const int seekCurrent = 1;
        if (offset is not 0 || origin is not seekCurrent)
        {
            return InvalidFunction;
        }

        if (newPosition is not 0)
        {
            Marshal.WriteInt64(newPosition, _receivedByteCount);
        }

        return Success;
    }

    public int Stat(out STATSTG stat, int flags)
    {
        const int streamType = 2;
        const int writeMode = 1;
        stat = new STATSTG { type = streamType, grfMode = writeMode, cbSize = _receivedByteCount };
        return Success;
    }

    public int Commit(int flags) => Success;
    public int SetSize(long byteCount) => InvalidFunction;
    public int CopyTo(nint destinationStreamAddress, long byteCount, nint bytesReadAddress, nint bytesWrittenAddress) => InvalidFunction;
    public int Revert() => InvalidFunction;
    public int LockRegion(long byteOffset, long byteCount, int lockType) => InvalidFunction;
    public int UnlockRegion(long byteOffset, long byteCount, int lockType) => InvalidFunction;
    public int Clone(out nint stream)
    {
        stream = 0;
        return InvalidFunction;
    }
}
