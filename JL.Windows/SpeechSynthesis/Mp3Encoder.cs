using System.Runtime.InteropServices;
using JL.Core.Utilities;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis.Interop;
using Windows.Storage.Streams;

namespace JL.Windows.SpeechSynthesis;

internal sealed class Mp3Encoder : IDisposable
{
    private const uint InitializeAllMediaComponents = 0;
    private const uint MediaFoundationVersion = 0x20070;
    private const uint MultithreadedComApartment = 0;
    private const int ComApartmentModeMismatch = unchecked((int)0x80010106);
    private const uint Mp3BytesPerSecond = 128000 / 8;
    private const uint PcmBitsPerSample = 16;
    private const uint AudioDecodingError = 1;
    private const uint AudioDecodingFinished = 2;
    private const uint EnumerateAllTransforms = 0x3F;
    private static readonly Guid s_containerFormatAttribute = new("150ff23f-4abc-478b-ac4f-e1916fba1cca");
    private static readonly Guid s_mp3ContainerFormat = new("e438b912-83f1-4de6-9e3a-9ffbc6dd24d1");
    private static readonly Guid s_disableEncoderThrottlingAttribute = new("08b845d8-2b74-4afe-9d53-be16d2d5ae4f");
    private static readonly Guid s_mediaKindAttribute = new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
    private static readonly Guid s_audioMediaKind = new("73647561-0000-0010-8000-00aa00389b71");
    private static readonly Guid s_audioEncodingAttribute = new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
    private static readonly Guid s_mp3AudioEncoding = new("00000055-0000-0010-8000-00aa00389b71");
    private static readonly Guid s_uncompressedAudioEncoding = new("00000001-0000-0010-8000-00aa00389b71");
    private static readonly Guid s_samplesPerSecondAttribute = new("5faeeae7-0290-4c31-9e8a-c534f68d9dba");
    private static readonly Guid s_channelCountAttribute = new("37e48bf5-645e-4c5b-89de-ada9e29b696a");
    private static readonly Guid s_bitsPerSampleAttribute = new("f2deb57f-40fa-4764-aa33-ed4f2d1ff669");
    private static readonly Guid s_bytesPerFrameAttribute = new("322de230-9eeb-43bd-ab7a-ff412251541d");
    private static readonly Guid s_bytesPerSecondAttribute = new("1aab75c8-cfef-451c-ab95-ac034b8e1731");

    private nint _cachedMp3Format;
    private uint _cachedSamplesPerSecond;
    private uint _cachedChannelCount;

    public Mp3Encoder()
    {
        Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFStartup(MediaFoundationVersion, InitializeAllMediaComponents));
    }

    public void Dispose()
    {
        ReleaseNativeObject(_cachedMp3Format);
        _cachedMp3Format = 0;
        _ = WinApi.NativeMethods.MFShutdown();
    }

    public InMemoryRandomAccessStream? Encode(IRandomAccessStream wave, int preferredSamplesPerSecond, short preferredChannelCount)
    {
        wave.Seek(0);
        int comInitializationResult = WinApi.NativeMethods.InitializeCom(0, MultithreadedComApartment);
        if (comInitializationResult is < 0 and not ComApartmentModeMismatch)
        {
            Marshal.ThrowExceptionForHR(comInitializationResult);
        }

        InMemoryRandomAccessStream? output = null;
        nint waveStreamAddress = 0;
        nint mp3StreamAddress = 0;
        nint decoderInputStream = 0;
        nint encoderOutputStream = 0;
        nint audioDecoder = 0;
        nint mp3Writer = 0;
        nint writerOptions = 0;
        nint uncompressedFormat = 0;
        try
        {
            output = new InMemoryRandomAccessStream();
            waveStreamAddress = WinRT.MarshalInterface<IRandomAccessStream>.FromManaged(wave);
            mp3StreamAddress = WinRT.MarshalInterface<IRandomAccessStream>.FromManaged(output);
            Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFCreateMFByteStreamOnStreamEx(waveStreamAddress, out decoderInputStream));
            Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFCreateMFByteStreamOnStreamEx(mp3StreamAddress, out encoderOutputStream));
            Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFCreateSourceReaderFromByteStream(decoderInputStream, 0, out audioDecoder));
            Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFCreateAttributes(out writerOptions, 2));
            MediaFoundation.SetIdentifierAttribute(writerOptions, s_containerFormatAttribute, s_mp3ContainerFormat);
            MediaFoundation.SetNumberAttribute(writerOptions, s_disableEncoderThrottlingAttribute, 1);
            Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFCreateSinkWriterFromURL(0, encoderOutputStream, writerOptions, out mp3Writer));
            nint mp3Format = GetMp3OutputFormat(preferredSamplesPerSecond, preferredChannelCount);
            if (mp3Format is 0)
            {
                LoggerManager.Logger.Error("The Windows MP3 encoder does not support the requested audio format");
                return null;
            }

            uint audioStreamIndex = MediaFoundation.AddAudioStream(mp3Writer, mp3Format);
            Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFCreateMediaType(out uncompressedFormat));
            MediaFoundation.SetIdentifierAttribute(uncompressedFormat, s_mediaKindAttribute, s_audioMediaKind);
            MediaFoundation.SetIdentifierAttribute(uncompressedFormat, s_audioEncodingAttribute, s_uncompressedAudioEncoding);
            uint encodedSamplesPerSecond = _cachedSamplesPerSecond;
            uint encodedChannelCount = _cachedChannelCount;
            uint encodedBytesPerFrame = encodedChannelCount * sizeof(short);
            MediaFoundation.SetNumberAttribute(uncompressedFormat, s_samplesPerSecondAttribute, encodedSamplesPerSecond);
            MediaFoundation.SetNumberAttribute(uncompressedFormat, s_channelCountAttribute, encodedChannelCount);
            MediaFoundation.SetNumberAttribute(uncompressedFormat, s_bitsPerSampleAttribute, PcmBitsPerSample);
            MediaFoundation.SetNumberAttribute(uncompressedFormat, s_bytesPerFrameAttribute, encodedBytesPerFrame);
            MediaFoundation.SetNumberAttribute(uncompressedFormat, s_bytesPerSecondAttribute, encodedSamplesPerSecond * encodedBytesPerFrame);
            MediaFoundation.SetDecodedAudioFormat(audioDecoder, uncompressedFormat);
            MediaFoundation.SetEncoderInputFormat(mp3Writer, audioStreamIndex, uncompressedFormat);
            MediaFoundation.StartEncoding(mp3Writer);
            if (!EncodeSpeechAudioChunks(audioDecoder, mp3Writer, audioStreamIndex, out long audioEndTimeTicks))
            {
                return null;
            }

            WriteEndingSilence(mp3Writer, audioStreamIndex, encodedSamplesPerSecond, encodedBytesPerFrame, audioEndTimeTicks);
            MediaFoundation.MarkEndOfAudio(mp3Writer, audioStreamIndex);
            MediaFoundation.FinishEncoding(mp3Writer);
            output.Seek(0);
            InMemoryRandomAccessStream compressed = output;
            output = null;
            return compressed;
        }
        finally
        {
            ReleaseNativeObject(mp3Writer);
            ReleaseNativeObject(audioDecoder);
            ReleaseNativeObject(uncompressedFormat);
            ReleaseNativeObject(writerOptions);
            ReleaseNativeObject(encoderOutputStream);
            ReleaseNativeObject(decoderInputStream);
            ReleaseNativeObject(mp3StreamAddress);
            ReleaseNativeObject(waveStreamAddress);
            if (comInitializationResult >= 0)
            {
                WinApi.NativeMethods.CoUninitialize();
            }

            output?.Dispose();
        }
    }

    private static bool EncodeSpeechAudioChunks(nint audioDecoder, nint mp3Writer, uint audioStreamIndex, out long audioEndTimeTicks)
    {
        audioEndTimeTicks = 0;
        while (true)
        {
            nint audioChunk = MediaFoundation.ReadDecodedAudioChunk(audioDecoder, out uint flags, out long startTimeTicks);
            if (audioChunk is not 0)
            {
                try
                {
                    long durationTicks = MediaFoundation.GetAudioChunkDurationTicks(audioChunk);
                    audioEndTimeTicks = startTimeTicks + durationTicks;
                    MediaFoundation.WriteAudioChunk(mp3Writer, audioStreamIndex, audioChunk);
                }
                finally
                {
                    _ = Marshal.Release(audioChunk);
                }
            }

            if ((flags & AudioDecodingError) is not 0)
            {
                LoggerManager.Logger.Error("The audio decoder failed while encoding speech");
                return false;
            }

            if ((flags & AudioDecodingFinished) is not 0)
            {
                return true;
            }
        }
    }

    private static unsafe void WriteEndingSilence(nint mp3Writer, uint audioStreamIndex, uint samplesPerSecond, uint bytesPerFrame, long startTimeTicks)
    {
        // Decoded-output tests found clipped speech without padding; four silent MP3 frames preserved the ending.
        const uint paddingFrames = 4;
        const uint samplesPerFrame = 1152;
        const uint silentSampleCount = paddingFrames * samplesPerFrame; // MPEG-1 Layer III has 1,152 samples per MP3 frame.
        uint byteCount = silentSampleCount * bytesPerFrame;
        Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFCreateMemoryBuffer(byteCount, out nint buffer));
        nint audioChunk = 0;
        try
        {
            nint bufferDataAddress = MediaFoundation.LockBufferData(buffer);
            try
            {
                new Span<byte>((void*)bufferDataAddress, (int)byteCount).Clear();
            }
            finally
            {
                MediaFoundation.UnlockBufferData(buffer);
            }

            MediaFoundation.SetBufferByteCount(buffer, byteCount);
            Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFCreateSample(out audioChunk));
            MediaFoundation.AttachBufferToAudioChunk(audioChunk, buffer);
            MediaFoundation.SetAudioChunkStartTimeTicks(audioChunk, startTimeTicks);
            MediaFoundation.SetAudioChunkDurationTicks(audioChunk, silentSampleCount * TimeSpan.TicksPerSecond / samplesPerSecond);
            MediaFoundation.WriteAudioChunk(mp3Writer, audioStreamIndex, audioChunk);
        }
        finally
        {
            ReleaseNativeObject(audioChunk);
            ReleaseNativeObject(buffer);
        }
    }

    private nint GetMp3OutputFormat(int preferredSamplesPerSecond, short preferredChannelCount)
    {
        // At 128 kbps, the Windows encoder offers 32, 44.1 and 48 kHz.
        // Keep the source rate when supported; otherwise choose the next higher rate, up to 48 kHz.
        uint outputSamplesPerSecond = preferredSamplesPerSecond switch
        {
            <= 32000 => 32000,
            <= 44100 => 44100,
            _ => 48000
        };
        uint outputChannelCount = preferredChannelCount is 1 ? 1u : 2u;
        if (_cachedMp3Format is not 0 && _cachedSamplesPerSecond == outputSamplesPerSecond && _cachedChannelCount == outputChannelCount)
        {
            return _cachedMp3Format;
        }

        Marshal.ThrowExceptionForHR(WinApi.NativeMethods.MFTranscodeGetAudioOutputAvailableTypes(in s_mp3AudioEncoding, EnumerateAllTransforms, 0, out nint availableFormats));
        try
        {
            uint count = MediaFoundation.GetAvailableFormatCount(availableFormats);
            for (uint index = 0; index < count; ++index)
            {
                nint availableFormat = MediaFoundation.GetAvailableFormat(availableFormats, index);
                try
                {
                    if (MediaFoundation.ReadNumberAttribute(availableFormat, s_samplesPerSecondAttribute) == outputSamplesPerSecond
                        && MediaFoundation.ReadNumberAttribute(availableFormat, s_channelCountAttribute) == outputChannelCount
                        && MediaFoundation.ReadNumberAttribute(availableFormat, s_bytesPerSecondAttribute) is Mp3BytesPerSecond)
                    {
                        ReleaseNativeObject(_cachedMp3Format);
                        _cachedMp3Format = availableFormat;
                        _cachedSamplesPerSecond = outputSamplesPerSecond;
                        _cachedChannelCount = outputChannelCount;
                        availableFormat = 0; // The cache now owns the native format reference.
                        return _cachedMp3Format;
                    }
                }
                finally
                {
                    ReleaseNativeObject(availableFormat);
                }
            }

            return 0;
        }
        finally
        {
            _ = Marshal.Release(availableFormats);
        }
    }

    private static void ReleaseNativeObject(nint instance)
    {
        if (instance is not 0)
        {
            _ = Marshal.Release(instance);
        }
    }
}
