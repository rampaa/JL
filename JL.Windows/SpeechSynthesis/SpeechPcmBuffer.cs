using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using JL.Core.Utilities;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis.Interop;

namespace JL.Windows.SpeechSynthesis;

// SAPI is the single producer. The mining worker is the single consumer.
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class SpeechPcmBuffer : ISpeechAudioOutput, IDisposable
{
    private const int Success = 0;
    private const int InvalidFunction = unchecked((int)0x80030001);
    private const int InvalidArgument = unchecked((int)0x80070057);
    private const int WriteAborted = unchecked((int)0x80004004);
    private const int BufferSizeBytes = 32768;
    private const int QueueCapacity = 4;
    private const int QueueMask = QueueCapacity - 1;
    private const uint InfiniteWait = uint.MaxValue;
    private const uint DataAvailable = 0;
    private const uint SpeechCompleted = 1;
    private const int SynthesisRunning = -1;
    private const int SynthesisCompleted = 1;
    private const int SynthesisStopped = 2;
    private readonly AutoResetEvent _dataAvailable;
    private readonly AutoResetEvent _spaceAvailable;
    private readonly PcmAudioChunk[] _chunks = new PcmAudioChunk[QueueCapacity];
    private int _readIndex;
    private int _writeIndex;
    private int _consumerWaiting;
    private int _producerWaiting;
    private int _synthesisState;
    private int _canceled;
    private int _bytesPerFrame;
    private int _bufferCapacityBytes;
    private nint _buffer;
    private nint _bufferDataAddress;
    private int _bufferedBytes;
    private long _receivedBytes;
    private bool _disposed;

    private SpeechPcmBuffer(short bytesPerFrame, AutoResetEvent dataAvailable, AutoResetEvent spaceAvailable)
    {
        _dataAvailable = dataAvailable;
        _spaceAvailable = spaceAvailable;
        _bytesPerFrame = bytesPerFrame;
        _bufferCapacityBytes = BufferSizeBytes / bytesPerFrame * bytesPerFrame;
    }

    public bool IsSynthesisStopped => Volatile.Read(ref _synthesisState) is not SynthesisRunning;
    public bool CompletedSuccessfully => Volatile.Read(ref _synthesisState) is SynthesisCompleted && Volatile.Read(ref _canceled) is 0;

    public void BeginSynthesis() => Volatile.Write(ref _synthesisState, SynthesisRunning);
    public void MarkSynthesisStopped() => Volatile.Write(ref _synthesisState, SynthesisStopped);

    public static SpeechPcmBuffer? Create(short bytesPerFrame)
    {
        if (bytesPerFrame <= 0)
        {
            LoggerManager.Logger.Error("Invalid PCM sample frame size: {BytesPerFrame}", bytesPerFrame);
            return null;
        }

        AutoResetEvent? dataAvailable = null;
        AutoResetEvent? spaceAvailable = null;
        try
        {
            dataAvailable = new AutoResetEvent(false);
            spaceAvailable = new AutoResetEvent(false);
            SpeechPcmBuffer buffer = new(bytesPerFrame, dataAvailable, spaceAvailable);
            dataAvailable = null;
            spaceAvailable = null;
            return buffer;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't create the speech audio queue");
            return null;
        }
        finally
        {
            dataAvailable?.Dispose();
            spaceAvailable?.Dispose();
        }
    }

    public bool Reset(short bytesPerFrame)
    {
        if (_disposed || bytesPerFrame <= 0 || !ReleaseBuffers())
        {
            return false;
        }

        if (_bytesPerFrame != bytesPerFrame)
        {
            _bytesPerFrame = bytesPerFrame;
            _bufferCapacityBytes = BufferSizeBytes / bytesPerFrame * bytesPerFrame;
        }

        _receivedBytes = 0;
        _readIndex = 0;
        _writeIndex = 0;
        _synthesisState = 0;
        _canceled = 0;
        return true;
    }

    public bool TryRead(out PcmAudioChunk chunk) => TryRead(0, out chunk);

    public bool TryRead(nint speechCompletedEvent, out PcmAudioChunk chunk)
    {
        while (true)
        {
            int readIndex = _readIndex;
            if (readIndex != Volatile.Read(ref _writeIndex))
            {
                int slot = readIndex & QueueMask;
                chunk = _chunks[slot];
                _chunks[slot] = default;

                _ = Interlocked.Exchange(ref _readIndex, unchecked(readIndex + 1));
                if (Volatile.Read(ref _producerWaiting) is not 0)
                {
                    _ = _spaceAvailable.Set();
                }

                return true;
            }

            if (Volatile.Read(ref _synthesisState) > 0 || Volatile.Read(ref _canceled) is not 0)
            {
                if (readIndex == Volatile.Read(ref _writeIndex))
                {
                    chunk = default;
                    return false;
                }

                continue;
            }

            if (WaitForData(speechCompletedEvent))
            {
                _ = Complete();
            }
        }
    }

    private unsafe bool WaitForData(nint speechCompletedEvent)
    {
        _ = Interlocked.Exchange(ref _consumerWaiting, 1);
        try
        {
            if (_readIndex != Volatile.Read(ref _writeIndex) || Volatile.Read(ref _synthesisState) > 0
                || Volatile.Read(ref _canceled) is not 0)
            {
                return false;
            }

            if (speechCompletedEvent is 0)
            {
                _ = _dataAvailable.WaitOne();
                return false;
            }

            nint* signals = stackalloc nint[2] { _dataAvailable.SafeWaitHandle.DangerousGetHandle(), speechCompletedEvent };
            uint result = WinApi.NativeMethods.WaitForMultipleObjects(2, signals, false, InfiniteWait);
            if (result is DataAvailable or SpeechCompleted)
            {
                return result is SpeechCompleted;
            }

            LoggerManager.Logger.Error("Waiting for speech audio failed with Windows error {ErrorCode}", Marshal.GetLastPInvokeError());
            Cancel();
            return false;
        }
        finally
        {
            Volatile.Write(ref _consumerWaiting, 0);
        }
    }

    public void Cancel()
    {
        _ = Interlocked.Exchange(ref _canceled, 1);
        if (Volatile.Read(ref _consumerWaiting) is not 0)
        {
            _ = _dataAvailable.Set();
        }

        if (Volatile.Read(ref _producerWaiting) is not 0)
        {
            _ = _spaceAvailable.Set();
        }
    }

    public bool Complete()
    {
        bool succeeded = Volatile.Read(ref _canceled) is 0;
        if (succeeded && _bufferedBytes > 0)
        {
            if (_bufferedBytes % _bytesPerFrame is not 0)
            {
                LoggerManager.Logger.Error("Speech synthesis ended with an incomplete PCM sample frame");
                Cancel();
                succeeded = false;
            }
            else
            {
                succeeded = QueueBuffer();
            }
        }

        _ = Interlocked.Exchange(ref _synthesisState, SynthesisCompleted);
        if (Volatile.Read(ref _consumerWaiting) is not 0)
        {
            _ = _dataAvailable.Set();
        }

        return succeeded;
    }

    public unsafe int Write(nint inputDataAddress, int inputByteCount, nint bytesWrittenAddress)
    {
        if (bytesWrittenAddress is not 0)
        {
            Marshal.WriteInt32(bytesWrittenAddress, 0);
        }

        if (inputByteCount < 0 || (inputDataAddress is 0 && inputByteCount > 0))
        {
            return InvalidArgument;
        }

        if (Volatile.Read(ref _canceled) is not 0)
        {
            return WriteAborted;
        }

        int offset = 0;
        while (offset < inputByteCount)
        {
            if (!EnsureBuffer())
            {
                return WriteAborted;
            }

            int length = Math.Min(_bufferCapacityBytes - _bufferedBytes, inputByteCount - offset);
            new ReadOnlySpan<byte>((void*)(inputDataAddress + offset), length)
                .CopyTo(new Span<byte>((void*)(_bufferDataAddress + _bufferedBytes), length));
            _bufferedBytes += length;
            offset += length;
            if (_bufferedBytes == _bufferCapacityBytes && !QueueBuffer())
            {
                return WriteAborted;
            }
        }

        Volatile.Write(ref _receivedBytes, _receivedBytes + inputByteCount);
        if (bytesWrittenAddress is not 0)
        {
            Marshal.WriteInt32(bytesWrittenAddress, inputByteCount);
        }

        return Success;
    }

    private bool EnsureBuffer()
    {
        if (_buffer is not 0)
        {
            return true;
        }

        if (!MediaFoundation.CheckResult(WinApi.NativeMethods.MFCreateMemoryBuffer((uint)_bufferCapacityBytes, out nint buffer), "Creating a speech audio buffer"))
        {
            Cancel();
            return false;
        }

        _bufferDataAddress = MediaFoundation.LockBufferData(buffer);
        if (_bufferDataAddress is 0)
        {
            _ = Marshal.Release(buffer);
            Cancel();
            return false;
        }

        _buffer = buffer;
        return true;
    }

    private bool QueueBuffer()
    {
        int writeIndex = _writeIndex;
        while (unchecked(writeIndex - Volatile.Read(ref _readIndex)) is QueueCapacity)
        {
            _ = Interlocked.Exchange(ref _producerWaiting, 1);
            try
            {
                if (Volatile.Read(ref _canceled) is not 0)
                {
                    return false;
                }

                if (unchecked(writeIndex - Volatile.Read(ref _readIndex)) is QueueCapacity)
                {
                    _ = _spaceAvailable.WaitOne();
                }
            }
            finally
            {
                Volatile.Write(ref _producerWaiting, 0);
            }
        }

        if (Volatile.Read(ref _canceled) is not 0)
        {
            return false;
        }

        Debug.Assert(_buffer is not 0);
        if (!MediaFoundation.UnlockBufferData(_buffer))
        {
            Cancel();
            return false;
        }

        _bufferDataAddress = 0;
        if (!MediaFoundation.SetBufferByteCount(_buffer, (uint)_bufferedBytes))
        {
            Cancel();
            return false;
        }

        _chunks[writeIndex & QueueMask] = new PcmAudioChunk(_buffer, _bufferedBytes);
        _buffer = 0;
        _bufferedBytes = 0;
        _ = Interlocked.Exchange(ref _writeIndex, unchecked(writeIndex + 1));
        if (Volatile.Read(ref _consumerWaiting) is not 0)
        {
            _ = _dataAvailable.Set();
        }

        return true;
    }

    public int Seek(long offset, int origin, nint newPosition)
    {
        const int seekCurrent = 1;
        if (offset is not 0 || origin is not seekCurrent)
        {
            return InvalidFunction;
        }

        if (newPosition is not 0)
        {
            Marshal.WriteInt64(newPosition, Volatile.Read(ref _receivedBytes));
        }

        return Success;
    }

    public int Stat(out STATSTG stat, int flags)
    {
        const int streamType = 2;
        const int writeMode = 1;
        stat = new STATSTG { type = streamType, grfMode = writeMode, cbSize = Volatile.Read(ref _receivedBytes) };
        return Success;
    }

    public bool ReleaseBuffers()
    {
        if (!IsSynthesisStopped)
        {
            LoggerManager.Logger.Error("Cannot release speech buffers while synthesis is running");
            return false;
        }

        bool succeeded = true;
        if (_buffer is not 0)
        {
            try
            {
                if (_bufferDataAddress is not 0)
                {
                    succeeded = MediaFoundation.UnlockBufferData(_buffer);
                }
            }
            finally
            {
                _ = Marshal.Release(_buffer);
                _buffer = 0;
                _bufferDataAddress = 0;
            }
        }

        while (_readIndex != _writeIndex)
        {
            int slot = _readIndex & QueueMask;
            _ = Marshal.Release(_chunks[slot].Buffer);
            _chunks[slot] = default;
            _readIndex = unchecked(_readIndex + 1);
        }

        _bufferedBytes = 0;
        return succeeded;
    }

    public void Dispose()
    {
        if (_disposed || !IsSynthesisStopped)
        {
            return;
        }

        _disposed = true;
        _ = ReleaseBuffers();
        _dataAvailable.Dispose();
        _spaceAvailable.Dispose();
    }

    public int Read(nint outputDataAddress, int byteCount, nint bytesReadAddress) => InvalidFunction;
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
