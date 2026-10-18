using System.Runtime.InteropServices;
using Windows.Storage.Streams;

namespace JL.Windows.SpeechSynthesis.Interop;

internal static class SpeechSynthesisInterop
{
    private static readonly Guid s_bufferByteAccess = new("905A0FEF-BC53-11DF-8C49-001E4FC686DA");
    private static readonly Guid s_speechEngine = new("A74D7C8E-4CC5-4F2F-A6EB-804DEE18500E");

    public static unsafe nint GetBufferDataAddress(IBuffer buffer)
    {
        nint nativeObjectAddress = WinRT.MarshalInterface<IBuffer>.FromManaged(buffer);
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(nativeObjectAddress, in s_bufferByteAccess, out nint bufferAccessAddress));
            try
            {
                const int bufferMethodIndex = 3;
                nint bufferDataAddress;
                nint* methods = *(nint**)bufferAccessAddress;
                delegate* unmanaged[Stdcall]<nint, nint*, int> getBuffer = (delegate* unmanaged[Stdcall]<nint, nint*, int>)methods[bufferMethodIndex];
                Marshal.ThrowExceptionForHR(getBuffer(bufferAccessAddress, &bufferDataAddress));
                return bufferDataAddress;
            }
            finally
            {
                _ = Marshal.Release(bufferAccessAddress);
            }
        }
        finally
        {
            _ = Marshal.Release(nativeObjectAddress);
        }
    }

    public static unsafe void GetPreferredAudioFormat(object engine, out Guid formatId, out nint waveFormat)
    {
        nint nativeObjectAddress = Marshal.GetIUnknownForObject(engine);
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(nativeObjectAddress, in s_speechEngine, out nint speechEngineAddress));
            try
            {
                const int outputFormatMethodIndex = 4;
                Guid format;
                nint waveFormatAddress;
                nint* methods = *(nint**)speechEngineAddress;
                delegate* unmanaged[Stdcall]<nint, nint, nint, Guid*, nint*, int> getOutputFormat =
                    (delegate* unmanaged[Stdcall]<nint, nint, nint, Guid*, nint*, int>)methods[outputFormatMethodIndex];
                Marshal.ThrowExceptionForHR(getOutputFormat(speechEngineAddress, 0, 0, &format, &waveFormatAddress));
                formatId = format;
                waveFormat = waveFormatAddress;
            }
            finally
            {
                _ = Marshal.Release(speechEngineAddress);
            }
        }
        finally
        {
            _ = Marshal.Release(nativeObjectAddress);
        }
    }
}
