using System.Runtime.InteropServices;
using JL.Core.Utilities;

namespace JL.Windows.SpeechSynthesis.Interop;

// Windows exposes these operations through tables of native function pointers, including inherited methods.
// Call them directly to pass native audio buffers without a managed COM wrapper per chunk.
internal static class MediaFoundation
{
    private static readonly Guid s_mediaTypeInterface = new("44AE0FA8-EA31-4109-8D2E-4CAE4997C555");

    public static unsafe bool SetIdentifierAttribute(nint attributes, Guid key, Guid value)
    {
        const int setGuidMethodIndex = 24;
        nint* methods = *(nint**)attributes;
        delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, int> method = (delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, int>)methods[setGuidMethodIndex];
        return CheckResult(method(attributes, &key, &value), "Setting an audio format identifier");
    }

    public static unsafe bool SetNumberAttribute(nint attributes, Guid key, uint value)
    {
        const int setUInt32MethodIndex = 21;
        nint* methods = *(nint**)attributes;
        delegate* unmanaged[Stdcall]<nint, Guid*, uint, int> method = (delegate* unmanaged[Stdcall]<nint, Guid*, uint, int>)methods[setUInt32MethodIndex];
        return CheckResult(method(attributes, &key, value), "Setting an audio format value");
    }

    public static unsafe bool TryReadNumberAttribute(nint attributes, Guid key, out uint number)
    {
        const int getUInt32MethodIndex = 7;
        uint value = 0;
        nint* methods = *(nint**)attributes;
        delegate* unmanaged[Stdcall]<nint, Guid*, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, Guid*, uint*, int>)methods[getUInt32MethodIndex];
        int result = method(attributes, &key, &value);
        number = value;
        return CheckResult(result, "Reading an audio format value");
    }

    public static unsafe bool TryAddAudioStream(nint writer, nint mediaType, out uint streamIndex)
    {
        const int addStreamMethodIndex = 3;
        uint value = 0;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, nint, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, nint, uint*, int>)methods[addStreamMethodIndex];
        int result = method(writer, mediaType, &value);
        streamIndex = value;
        return CheckResult(result, "Adding the MP3 audio stream");
    }

    public static unsafe bool SetEncoderInputFormat(nint writer, uint streamIndex, nint mediaType)
    {
        const int setWriterFormatMethodIndex = 4;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, uint, nint, nint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nint, nint, int>)methods[setWriterFormatMethodIndex];
        return CheckResult(method(writer, streamIndex, mediaType, 0), "Setting the MP3 input format");
    }

    public static unsafe bool StartEncoding(nint writer)
    {
        const int beginWritingMethodIndex = 5;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, int> method = (delegate* unmanaged[Stdcall]<nint, int>)methods[beginWritingMethodIndex];
        return CheckResult(method(writer), "Starting MP3 encoding");
    }

    public static unsafe bool WriteAudioChunk(nint writer, uint streamIndex, nint audioChunk)
    {
        const int writeSampleMethodIndex = 6;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, uint, nint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nint, int>)methods[writeSampleMethodIndex];
        return CheckResult(method(writer, streamIndex, audioChunk), "Writing an MP3 audio chunk");
    }

    public static unsafe bool MarkEndOfAudio(nint writer, uint streamIndex)
    {
        const int endSegmentMethodIndex = 9;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, uint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, int>)methods[endSegmentMethodIndex];
        return CheckResult(method(writer, streamIndex), "Marking the end of audio");
    }

    public static unsafe bool FinishEncoding(nint writer)
    {
        const int finalizeWriterMethodIndex = 11;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, int> method = (delegate* unmanaged[Stdcall]<nint, int>)methods[finalizeWriterMethodIndex];
        return CheckResult(method(writer), "Finishing MP3 encoding");
    }

    public static unsafe bool UnlockBufferData(nint buffer)
    {
        const int unlockBufferMethodIndex = 4;
        nint* methods = *(nint**)buffer;
        delegate* unmanaged[Stdcall]<nint, int> method = (delegate* unmanaged[Stdcall]<nint, int>)methods[unlockBufferMethodIndex];
        return CheckResult(method(buffer), "Unlocking an audio buffer");
    }

    public static unsafe bool SetBufferByteCount(nint buffer, uint length)
    {
        const int setBufferLengthMethodIndex = 6;
        nint* methods = *(nint**)buffer;
        delegate* unmanaged[Stdcall]<nint, uint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, int>)methods[setBufferLengthMethodIndex];
        return CheckResult(method(buffer, length), "Setting the audio buffer length");
    }

    public static unsafe bool AttachBufferToAudioChunk(nint audioChunk, nint buffer)
    {
        const int addBufferMethodIndex = 42;
        nint* methods = *(nint**)audioChunk;
        delegate* unmanaged[Stdcall]<nint, nint, int> method = (delegate* unmanaged[Stdcall]<nint, nint, int>)methods[addBufferMethodIndex];
        return CheckResult(method(audioChunk, buffer), "Attaching an audio buffer");
    }

    public static unsafe bool SetAudioChunkStartTimeTicks(nint audioChunk, long time)
    {
        const int setSampleTimeMethodIndex = 36;
        nint* methods = *(nint**)audioChunk;
        delegate* unmanaged[Stdcall]<nint, long, int> method = (delegate* unmanaged[Stdcall]<nint, long, int>)methods[setSampleTimeMethodIndex];
        return CheckResult(method(audioChunk, time), "Setting the audio timestamp");
    }

    public static unsafe bool SetAudioChunkDurationTicks(nint audioChunk, long durationTicks)
    {
        const int setSampleDurationMethodIndex = 38;
        nint* methods = *(nint**)audioChunk;
        delegate* unmanaged[Stdcall]<nint, long, int> method = (delegate* unmanaged[Stdcall]<nint, long, int>)methods[setSampleDurationMethodIndex];
        return CheckResult(method(audioChunk, durationTicks), "Setting the audio duration");
    }

    public static unsafe bool TryGetAvailableFormatCount(nint types, out uint count)
    {
        const int getMediaTypeCountMethodIndex = 3;
        uint value = 0;
        nint* methods = *(nint**)types;
        delegate* unmanaged[Stdcall]<nint, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, uint*, int>)methods[getMediaTypeCountMethodIndex];
        int result = method(types, &value);
        count = value;
        return CheckResult(result, "Reading the available MP3 formats");
    }

    public static unsafe nint GetAvailableFormat(nint types, uint index)
    {
        const int getMediaTypeMethodIndex = 4;
        nint value = 0;
        nint* methods = *(nint**)types;
        delegate* unmanaged[Stdcall]<nint, uint, nint*, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)methods[getMediaTypeMethodIndex];
        if (!CheckResult(method(types, index, &value), "Reading an MP3 format"))
        {
            return 0;
        }
        try
        {
            // IMFCollection returns IUnknown; obtain the media-type interface before using its vtable.
            return CheckResult(Marshal.QueryInterface(value, in s_mediaTypeInterface, out nint mediaType), "Accessing the MP3 format")
                ? mediaType
                : 0;
        }
        finally
        {
            _ = Marshal.Release(value);
        }
    }

    public static unsafe nint LockBufferData(nint buffer)
    {
        const int lockBufferMethodIndex = 3;
        nint data = 0;
        nint* methods = *(nint**)buffer;
        delegate* unmanaged[Stdcall]<nint, nint*, uint*, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, nint*, uint*, uint*, int>)methods[lockBufferMethodIndex];
        return CheckResult(method(buffer, &data, null, null), "Locking an audio buffer") ? data : 0;
    }

    public static unsafe bool SetTransformType(nint transform, nint mediaType, bool input)
    {
        const int setInputTypeIndex = 15;
        const int setOutputTypeIndex = 16;
        nint* methods = *(nint**)transform;
        delegate* unmanaged[Stdcall]<nint, uint, nint, uint, int> method =
            (delegate* unmanaged[Stdcall]<nint, uint, nint, uint, int>)methods[input ? setInputTypeIndex : setOutputTypeIndex];
        return CheckResult(method(transform, 0, mediaType, 0), "Setting the resampler format");
    }

    public static unsafe bool SendTransformMessage(nint transform, uint message)
    {
        const int processMessageIndex = 23;
        nint* methods = *(nint**)transform;
        delegate* unmanaged[Stdcall]<nint, uint, nuint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nuint, int>)methods[processMessageIndex];
        return CheckResult(method(transform, message, 0), "Updating the resampler state");
    }

    public static unsafe bool ProcessTransformInput(nint transform, nint sample)
    {
        const int processInputIndex = 24;
        nint* methods = *(nint**)transform;
        delegate* unmanaged[Stdcall]<nint, uint, nint, uint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nint, uint, int>)methods[processInputIndex];
        return CheckResult(method(transform, 0, sample, 0), "Submitting audio to the resampler");
    }

    public static unsafe int ProcessTransformOutput(nint transform, ref TransformOutput output)
    {
        const int processOutputIndex = 25;
        nint* methods = *(nint**)transform;
        delegate* unmanaged[Stdcall]<nint, uint, uint, TransformOutput*, uint*, int> method =
            (delegate* unmanaged[Stdcall]<nint, uint, uint, TransformOutput*, uint*, int>)methods[processOutputIndex];
        fixed (TransformOutput* buffer = &output)
        {
            uint status;
            return method(transform, 0, 1, buffer, &status);
        }
    }

    public static unsafe bool TryGetTransformOutputInfo(nint transform, out TransformOutputInfo info)
    {
        const int getOutputStreamInfoIndex = 7;
        TransformOutputInfo value = default;
        nint* methods = *(nint**)transform;
        delegate* unmanaged[Stdcall]<nint, uint, TransformOutputInfo*, int> method =
            (delegate* unmanaged[Stdcall]<nint, uint, TransformOutputInfo*, int>)methods[getOutputStreamInfoIndex];
        int result = method(transform, 0, &value);
        info = value;
        return CheckResult(result, "Reading the resampler output requirements");
    }

    public static unsafe bool TryGetAudioChunkByteCount(nint sample, out uint byteCount)
    {
        const int getTotalLengthIndex = 45;
        uint length = 0;
        nint* methods = *(nint**)sample;
        delegate* unmanaged[Stdcall]<nint, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, uint*, int>)methods[getTotalLengthIndex];
        int result = method(sample, &length);
        byteCount = length;
        return CheckResult(result, "Reading the audio chunk length");
    }

    public static bool CheckResult(int result, string operation)
    {
        if (result >= 0)
        {
            return true;
        }

        LoggerManager.Logger.Error("{Operation} failed with HRESULT 0x{HResult:X8}", operation, result);
        return false;
    }
}
