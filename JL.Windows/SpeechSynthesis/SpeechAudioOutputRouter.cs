using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using JL.Windows.SpeechSynthesis.Interop;

namespace JL.Windows.SpeechSynthesis;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class SpeechAudioOutputRouter : ISpeechAudioOutput
{
    private const int StreamReverted = unchecked((int)0x80030102);

    public ISpeechAudioOutput? Destination
    {
        get => Volatile.Read(ref field);
        set => Volatile.Write(ref field, value);
    }

    public int Read(nint outputDataAddress, int byteCount, nint bytesReadAddress)
    {
        ISpeechAudioOutput? destination = Destination;
        if (destination is not null)
        {
            return destination.Read(outputDataAddress, byteCount, bytesReadAddress);
        }

        if (bytesReadAddress is not 0)
        {
            Marshal.WriteInt32(bytesReadAddress, 0);
        }

        return StreamReverted;
    }

    public int Write(nint inputDataAddress, int inputByteCount, nint bytesWrittenAddress)
    {
        ISpeechAudioOutput? destination = Destination;
        if (destination is not null)
        {
            return destination.Write(inputDataAddress, inputByteCount, bytesWrittenAddress);
        }

        if (bytesWrittenAddress is not 0)
        {
            Marshal.WriteInt32(bytesWrittenAddress, 0);
        }

        return StreamReverted;
    }

    public int Seek(long offset, int origin, nint newPosition)
    {
        ISpeechAudioOutput? destination = Destination;
        if (destination is not null)
        {
            return destination.Seek(offset, origin, newPosition);
        }

        if (newPosition is not 0)
        {
            Marshal.WriteInt64(newPosition, 0);
        }

        return StreamReverted;
    }

    public int SetSize(long byteCount)
    {
        return Destination?.SetSize(byteCount) ?? StreamReverted;
    }

    public int CopyTo(nint destinationStreamAddress, long byteCount, nint bytesReadAddress, nint bytesWrittenAddress)
    {
        ISpeechAudioOutput? destination = Destination;
        if (destination is not null)
        {
            return destination.CopyTo(destinationStreamAddress, byteCount, bytesReadAddress, bytesWrittenAddress);
        }

        if (bytesReadAddress is not 0)
        {
            Marshal.WriteInt64(bytesReadAddress, 0);
        }

        if (bytesWrittenAddress is not 0)
        {
            Marshal.WriteInt64(bytesWrittenAddress, 0);
        }

        return StreamReverted;
    }

    public int Commit(int flags)
    {
        return Destination?.Commit(flags) ?? StreamReverted;
    }

    public int Revert()
    {
        return Destination?.Revert() ?? StreamReverted;
    }

    public int LockRegion(long byteOffset, long byteCount, int lockType)
    {
        return Destination?.LockRegion(byteOffset, byteCount, lockType) ?? StreamReverted;
    }

    public int UnlockRegion(long byteOffset, long byteCount, int lockType)
    {
        return Destination?.UnlockRegion(byteOffset, byteCount, lockType) ?? StreamReverted;
    }

    public int Stat(out STATSTG stat, int flags)
    {
        ISpeechAudioOutput? destination = Destination;
        if (destination is not null)
        {
            return destination.Stat(out stat, flags);
        }

        stat = default;
        return StreamReverted;
    }

    public int Clone(out nint stream)
    {
        ISpeechAudioOutput? destination = Destination;
        if (destination is not null)
        {
            return destination.Clone(out stream);
        }

        stream = 0;
        return StreamReverted;
    }
}
