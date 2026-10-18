using System.Runtime.InteropServices;

namespace JL.Windows.SpeechSynthesis.Interop;

// Windows exposes these operations through tables of native function pointers, including inherited methods.
// Call them directly so encoding can reuse native audio buffers without managed wrappers for each audio chunk.
internal static class MediaFoundation
{
    private const uint FirstAudioStream = 0xFFFFFFFD;
    private static readonly Guid s_mediaTypeInterface = new("44AE0FA8-EA31-4109-8D2E-4CAE4997C555");

    public static unsafe void SetIdentifierAttribute(nint attributes, Guid key, Guid value)
    {
        const int setGuidMethodIndex = 24;
        nint* methods = *(nint**)attributes;
        delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, int> method = (delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, int>)methods[setGuidMethodIndex];
        Marshal.ThrowExceptionForHR(method(attributes, &key, &value));
    }

    public static unsafe void SetNumberAttribute(nint attributes, Guid key, uint value)
    {
        const int setUInt32MethodIndex = 21;
        nint* methods = *(nint**)attributes;
        delegate* unmanaged[Stdcall]<nint, Guid*, uint, int> method = (delegate* unmanaged[Stdcall]<nint, Guid*, uint, int>)methods[setUInt32MethodIndex];
        Marshal.ThrowExceptionForHR(method(attributes, &key, value));
    }

    public static unsafe uint ReadNumberAttribute(nint attributes, Guid key)
    {
        const int getUInt32MethodIndex = 7;
        uint value;
        nint* methods = *(nint**)attributes;
        delegate* unmanaged[Stdcall]<nint, Guid*, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, Guid*, uint*, int>)methods[getUInt32MethodIndex];
        Marshal.ThrowExceptionForHR(method(attributes, &key, &value));
        return value;
    }

    public static unsafe uint AddAudioStream(nint writer, nint mediaType)
    {
        const int addStreamMethodIndex = 3;
        uint value;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, nint, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, nint, uint*, int>)methods[addStreamMethodIndex];
        Marshal.ThrowExceptionForHR(method(writer, mediaType, &value));
        return value;
    }

    public static unsafe void SetDecodedAudioFormat(nint reader, nint mediaType)
    {
        const int setReaderFormatMethodIndex = 7;
        nint* methods = *(nint**)reader;
        delegate* unmanaged[Stdcall]<nint, uint, nint, nint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nint, nint, int>)methods[setReaderFormatMethodIndex];
        Marshal.ThrowExceptionForHR(method(reader, FirstAudioStream, 0, mediaType));
    }

    public static unsafe void SetEncoderInputFormat(nint writer, uint streamIndex, nint mediaType)
    {
        const int setWriterFormatMethodIndex = 4;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, uint, nint, nint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nint, nint, int>)methods[setWriterFormatMethodIndex];
        Marshal.ThrowExceptionForHR(method(writer, streamIndex, mediaType, 0));
    }

    public static unsafe void StartEncoding(nint writer)
    {
        const int beginWritingMethodIndex = 5;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, int> method = (delegate* unmanaged[Stdcall]<nint, int>)methods[beginWritingMethodIndex];
        Marshal.ThrowExceptionForHR(method(writer));
    }

    public static unsafe void WriteAudioChunk(nint writer, uint streamIndex, nint audioChunk)
    {
        const int writeSampleMethodIndex = 6;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, uint, nint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nint, int>)methods[writeSampleMethodIndex];
        Marshal.ThrowExceptionForHR(method(writer, streamIndex, audioChunk));
    }

    public static unsafe long GetAudioChunkDurationTicks(nint audioChunk)
    {
        const int getSampleDurationMethodIndex = 37;
        long value;
        nint* methods = *(nint**)audioChunk;
        delegate* unmanaged[Stdcall]<nint, long*, int> method = (delegate* unmanaged[Stdcall]<nint, long*, int>)methods[getSampleDurationMethodIndex];
        Marshal.ThrowExceptionForHR(method(audioChunk, &value));
        return value;
    }

    public static unsafe void MarkEndOfAudio(nint writer, uint streamIndex)
    {
        const int endSegmentMethodIndex = 9;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, uint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, int>)methods[endSegmentMethodIndex];
        Marshal.ThrowExceptionForHR(method(writer, streamIndex));
    }

    public static unsafe void FinishEncoding(nint writer)
    {
        const int finalizeWriterMethodIndex = 11;
        nint* methods = *(nint**)writer;
        delegate* unmanaged[Stdcall]<nint, int> method = (delegate* unmanaged[Stdcall]<nint, int>)methods[finalizeWriterMethodIndex];
        Marshal.ThrowExceptionForHR(method(writer));
    }

    public static unsafe void UnlockBufferData(nint buffer)
    {
        const int unlockBufferMethodIndex = 4;
        nint* methods = *(nint**)buffer;
        delegate* unmanaged[Stdcall]<nint, int> method = (delegate* unmanaged[Stdcall]<nint, int>)methods[unlockBufferMethodIndex];
        Marshal.ThrowExceptionForHR(method(buffer));
    }

    public static unsafe void SetBufferByteCount(nint buffer, uint length)
    {
        const int setBufferLengthMethodIndex = 6;
        nint* methods = *(nint**)buffer;
        delegate* unmanaged[Stdcall]<nint, uint, int> method = (delegate* unmanaged[Stdcall]<nint, uint, int>)methods[setBufferLengthMethodIndex];
        Marshal.ThrowExceptionForHR(method(buffer, length));
    }

    public static unsafe void AttachBufferToAudioChunk(nint audioChunk, nint buffer)
    {
        const int addBufferMethodIndex = 42;
        nint* methods = *(nint**)audioChunk;
        delegate* unmanaged[Stdcall]<nint, nint, int> method = (delegate* unmanaged[Stdcall]<nint, nint, int>)methods[addBufferMethodIndex];
        Marshal.ThrowExceptionForHR(method(audioChunk, buffer));
    }

    public static unsafe void SetAudioChunkStartTimeTicks(nint audioChunk, long time)
    {
        const int setSampleTimeMethodIndex = 36;
        nint* methods = *(nint**)audioChunk;
        delegate* unmanaged[Stdcall]<nint, long, int> method = (delegate* unmanaged[Stdcall]<nint, long, int>)methods[setSampleTimeMethodIndex];
        Marshal.ThrowExceptionForHR(method(audioChunk, time));
    }

    public static unsafe void SetAudioChunkDurationTicks(nint audioChunk, long durationTicks)
    {
        const int setSampleDurationMethodIndex = 38;
        nint* methods = *(nint**)audioChunk;
        delegate* unmanaged[Stdcall]<nint, long, int> method = (delegate* unmanaged[Stdcall]<nint, long, int>)methods[setSampleDurationMethodIndex];
        Marshal.ThrowExceptionForHR(method(audioChunk, durationTicks));
    }

    public static unsafe uint GetAvailableFormatCount(nint types)
    {
        const int getMediaTypeCountMethodIndex = 3;
        uint value;
        nint* methods = *(nint**)types;
        delegate* unmanaged[Stdcall]<nint, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, uint*, int>)methods[getMediaTypeCountMethodIndex];
        Marshal.ThrowExceptionForHR(method(types, &value));
        return value;
    }

    public static unsafe nint GetAvailableFormat(nint types, uint index)
    {
        const int getMediaTypeMethodIndex = 4;
        nint value;
        nint* methods = *(nint**)types;
        delegate* unmanaged[Stdcall]<nint, uint, nint*, int> method = (delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)methods[getMediaTypeMethodIndex];
        Marshal.ThrowExceptionForHR(method(types, index, &value));
        try
        {
            // IMFCollection returns IUnknown; obtain the media-type interface before using its vtable.
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(value, in s_mediaTypeInterface, out nint mediaType));
            return mediaType;
        }
        finally
        {
            _ = Marshal.Release(value);
        }
    }

    public static unsafe nint ReadDecodedAudioChunk(nint reader, out uint flags, out long startTimeTicks)
    {
        const int readSampleMethodIndex = 9;
        uint sampleFlags;
        long sampleTime;
        nint audioChunk;
        nint* methods = *(nint**)reader;
        delegate* unmanaged[Stdcall]<nint, uint, uint, uint*, uint*, long*, nint*, int> readSample =
            (delegate* unmanaged[Stdcall]<nint, uint, uint, uint*, uint*, long*, nint*, int>)methods[readSampleMethodIndex];
        Marshal.ThrowExceptionForHR(readSample(reader, FirstAudioStream, 0, null, &sampleFlags, &sampleTime, &audioChunk));
        flags = sampleFlags;
        startTimeTicks = sampleTime;
        return audioChunk;
    }

    public static unsafe nint LockBufferData(nint buffer)
    {
        const int lockBufferMethodIndex = 3;
        nint data;
        nint* methods = *(nint**)buffer;
        delegate* unmanaged[Stdcall]<nint, nint*, uint*, uint*, int> method = (delegate* unmanaged[Stdcall]<nint, nint*, uint*, uint*, int>)methods[lockBufferMethodIndex];
        Marshal.ThrowExceptionForHR(method(buffer, &data, null, null));
        return data;
    }
}
