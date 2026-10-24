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
            if (!MediaFoundation.CheckResult(Marshal.QueryInterface(nativeObjectAddress, in s_bufferByteAccess, out nint bufferAccessAddress), "Accessing the playback buffer"))
            {
                return 0;
            }
            try
            {
                const int bufferMethodIndex = 3;
                nint bufferDataAddress = 0;
                nint* methods = *(nint**)bufferAccessAddress;
                delegate* unmanaged[Stdcall]<nint, nint*, int> getBuffer = (delegate* unmanaged[Stdcall]<nint, nint*, int>)methods[bufferMethodIndex];
                return MediaFoundation.CheckResult(getBuffer(bufferAccessAddress, &bufferDataAddress), "Reading the playback buffer address")
                    ? bufferDataAddress
                    : 0;
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

    public static unsafe bool TryGetPreferredAudioFormat(object engine, out Guid formatId, out nint waveFormat)
    {
        formatId = default;
        waveFormat = 0;
        nint nativeObjectAddress = Marshal.GetIUnknownForObject(engine);
        try
        {
            if (!MediaFoundation.CheckResult(Marshal.QueryInterface(nativeObjectAddress, in s_speechEngine, out nint speechEngineAddress), "Accessing the speech engine"))
            {
                return false;
            }
            try
            {
                const int outputFormatMethodIndex = 4;
                Guid format = default;
                nint waveFormatAddress = 0;
                nint* methods = *(nint**)speechEngineAddress;
                delegate* unmanaged[Stdcall]<nint, nint, nint, Guid*, nint*, int> getOutputFormat =
                    (delegate* unmanaged[Stdcall]<nint, nint, nint, Guid*, nint*, int>)methods[outputFormatMethodIndex];
                if (!MediaFoundation.CheckResult(getOutputFormat(speechEngineAddress, 0, 0, &format, &waveFormatAddress), "Reading the speech audio format"))
                {
                    Marshal.FreeCoTaskMem(waveFormatAddress);
                    return false;
                }

                formatId = format;
                waveFormat = waveFormatAddress;
                return true;
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
