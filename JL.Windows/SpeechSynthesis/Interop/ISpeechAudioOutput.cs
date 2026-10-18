using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace JL.Windows.SpeechSynthesis.Interop;

#pragma warning disable CA1515 // Runtime COM exposes this interface to SAPI only when it is public.
// SAPI expects the exact IStream method order. PreserveSig returns errors directly without throwing.
[ComVisible(true), Guid("0000000C-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISpeechAudioOutput
{
    [PreserveSig]
    public int Read(nint outputDataAddress, int byteCount, nint bytesReadAddress);
    [PreserveSig]
    public int Write(nint inputDataAddress, int inputByteCount, nint bytesWrittenAddress);
    [PreserveSig]
    public int Seek(long offset, int origin, nint newPosition);
    [PreserveSig]
    public int SetSize(long byteCount);
    [PreserveSig]
    public int CopyTo(nint destinationStreamAddress, long byteCount, nint bytesReadAddress, nint bytesWrittenAddress);
    [PreserveSig]
    public int Commit(int flags);
    [PreserveSig]
    public int Revert();
    [PreserveSig]
    public int LockRegion(long byteOffset, long byteCount, int lockType);
    [PreserveSig]
    public int UnlockRegion(long byteOffset, long byteCount, int lockType);
    [PreserveSig]
    public int Stat(out STATSTG stat, int flags);
    [PreserveSig]
    public int Clone(out nint stream);
}

#pragma warning restore CA1515
