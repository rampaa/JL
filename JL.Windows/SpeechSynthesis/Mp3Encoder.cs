using System.Runtime.InteropServices;
using JL.Core.Utilities;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis.Interop;
using Windows.Storage.Streams;

namespace JL.Windows.SpeechSynthesis;

// Mining serializes requests on MTA workers; cached native objects have one active caller.
internal sealed class Mp3Encoder : IDisposable
{
    private const uint InitializeAllMediaComponents = 0;
    private const uint MediaFoundationVersion = 0x20070;
    private const uint MultithreadedComApartment = 0;
    private const uint Mp3BytesPerSecond = 128000 / 8;
    private const uint PcmBitsPerSample = 16;
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

    private readonly nint _writerOptions;
    private nint _cachedMp3Format;
    private nint _cachedSourceFormat;
    private nint _cachedEncoderFormat;
    private SpeechAudioFormat _cachedPcmFormat;
    private AudioResampler? _resampler;
    private SpeechPcmBuffer? _pcmBuffer;
    private uint _cachedSamplesPerSecond;
    private uint _cachedChannelCount;
    private bool _disposed;

    private Mp3Encoder(nint writerOptions)
    {
        _writerOptions = writerOptions;
    }

    public static Mp3Encoder? Create()
    {
        if (!MediaFoundation.CheckResult(WinApi.NativeMethods.MFStartup(MediaFoundationVersion, InitializeAllMediaComponents), "Starting Windows audio encoding"))
        {
            return null;
        }

        nint writerOptions = 0;
        bool completed = false;
        try
        {
            if (!MediaFoundation.CheckResult(WinApi.NativeMethods.MFCreateAttributes(out writerOptions, 2), "Creating audio encoder options")
                || !MediaFoundation.SetIdentifierAttribute(writerOptions, s_containerFormatAttribute, s_mp3ContainerFormat)
                || !MediaFoundation.SetNumberAttribute(writerOptions, s_disableEncoderThrottlingAttribute, 1))
            {
                return null;
            }

            Mp3Encoder encoder = new(writerOptions);
            completed = true;
            return encoder;
        }
        finally
        {
            if (!completed)
            {
                ReleaseNativeObject(writerOptions);
                _ = MediaFoundation.CheckResult(WinApi.NativeMethods.MFShutdown(), "Stopping Windows audio encoding");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_pcmBuffer is { IsSynthesisStopped: false })
        {
            LoggerManager.Logger.Error("Cannot dispose the audio encoder while synthesis is running");
            return;
        }

        _disposed = true;
        ClearPcmFormatCache();
        _pcmBuffer?.Dispose();
        _pcmBuffer = null;
        ReleaseNativeObject(_cachedMp3Format);
        _cachedMp3Format = 0;
        ReleaseNativeObject(_writerOptions);
        _ = MediaFoundation.CheckResult(WinApi.NativeMethods.MFShutdown(), "Stopping Windows audio encoding");
    }

    public InMemoryRandomAccessStream? Encode(NativeSpeechSynthesizer synthesizer, string text)
    {
        if (_disposed || !MediaFoundation.CheckResult(WinApi.NativeMethods.InitializeCom(0, MultithreadedComApartment), "Initializing audio encoding on the worker"))
        {
            return null;
        }

        InMemoryRandomAccessStream? output = null;
        nint mp3StreamAddress = 0;
        nint encoderOutputStream = 0;
        nint mp3Writer = 0;
        bool completed = false;
        try
        {
            SpeechAudioFormat format = synthesizer.PreferredAudioFormat;
            nint mp3Format = GetMp3OutputFormat(format.SamplesPerSecond, format.ChannelCount);
            if (mp3Format is 0)
            {
                LoggerManager.Logger.Error("The Windows MP3 encoder does not support the requested audio format");
                return null;
            }

            output = new InMemoryRandomAccessStream();
            mp3StreamAddress = WinRT.MarshalInterface<IRandomAccessStream>.FromManaged(output);
            if (!MediaFoundation.CheckResult(WinApi.NativeMethods.MFCreateMFByteStreamOnStreamEx(mp3StreamAddress, out encoderOutputStream), "Creating the MP3 output stream")
                || !MediaFoundation.CheckResult(WinApi.NativeMethods.MFCreateSinkWriterFromURL(0, encoderOutputStream, _writerOptions, out mp3Writer), "Creating the MP3 encoder")
                || !MediaFoundation.TryAddAudioStream(mp3Writer, mp3Format, out uint audioStreamIndex)
                || !TryEnsurePcmFormat(format))
            {
                return null;
            }

            if (!MediaFoundation.SetEncoderInputFormat(mp3Writer, audioStreamIndex, _cachedEncoderFormat)
                || !MediaFoundation.StartEncoding(mp3Writer)
                || (_resampler is not null && !_resampler.BeginInput()))
            {
                return null;
            }

            uint encodedBytesPerFrame = _cachedChannelCount * sizeof(short);
            if (!TryEncodeSpeechAudioChunks(synthesizer, text, format, mp3Writer, audioStreamIndex, encodedBytesPerFrame, _resampler, out long endTimeTicks))
            {
                return null;
            }

            if (!WriteEndingSilence(mp3Writer, audioStreamIndex, _cachedSamplesPerSecond, encodedBytesPerFrame, endTimeTicks)
                || !MediaFoundation.MarkEndOfAudio(mp3Writer, audioStreamIndex)
                || !MediaFoundation.FinishEncoding(mp3Writer))
            {
                return null;
            }

            output.Seek(0);
            InMemoryRandomAccessStream compressed = output;
            output = null;
            completed = true;
            return compressed;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't encode speech audio");
            return null;
        }
        finally
        {
            ReleaseNativeObject(mp3Writer);
            if (!completed)
            {
                ClearPcmFormatCache();
            }

            ReleaseNativeObject(encoderOutputStream);
            ReleaseNativeObject(mp3StreamAddress);
            WinApi.NativeMethods.CoUninitialize();

            output?.Dispose();
        }
    }

    private bool TryEnsurePcmFormat(SpeechAudioFormat format)
    {
        if (_cachedEncoderFormat is not 0 && _cachedPcmFormat == format)
        {
            return true;
        }

        ClearPcmFormatCache();
        _cachedEncoderFormat = CreatePcmFormat(_cachedSamplesPerSecond, _cachedChannelCount, PcmBitsPerSample);
        if (_cachedEncoderFormat is 0)
        {
            return false;
        }

        if (format.SamplesPerSecond != _cachedSamplesPerSecond || format.ChannelCount != _cachedChannelCount
            || format.BitsPerSample != PcmBitsPerSample)
        {
            _cachedSourceFormat = CreatePcmFormat((uint)format.SamplesPerSecond, (uint)format.ChannelCount, (uint)format.BitsPerSample);
            if (_cachedSourceFormat is 0)
            {
                return false;
            }

            _resampler = AudioResampler.Create(_cachedSourceFormat, _cachedEncoderFormat);
            if (_resampler is null)
            {
                return false;
            }
        }

        _cachedPcmFormat = format;
        return true;
    }

    private void ClearPcmFormatCache()
    {
        _resampler?.Dispose();
        _resampler = null;
        ReleaseNativeObject(_cachedSourceFormat);
        _cachedSourceFormat = 0;
        ReleaseNativeObject(_cachedEncoderFormat);
        _cachedEncoderFormat = 0;
        _cachedPcmFormat = default;
    }

    private static nint CreatePcmFormat(uint samplesPerSecond, uint channels, uint bitsPerSample)
    {
        int result = WinApi.NativeMethods.MFCreateMediaType(out nint format);
        if (result < 0)
        {
            LoggerManager.Logger.Error("Couldn't create a PCM audio format: {HResult}", result);
            return 0;
        }

        uint bytesPerFrame = channels * bitsPerSample / 8;
        if (MediaFoundation.SetIdentifierAttribute(format, s_mediaKindAttribute, s_audioMediaKind)
            && MediaFoundation.SetIdentifierAttribute(format, s_audioEncodingAttribute, s_uncompressedAudioEncoding)
            && MediaFoundation.SetNumberAttribute(format, s_samplesPerSecondAttribute, samplesPerSecond)
            && MediaFoundation.SetNumberAttribute(format, s_channelCountAttribute, channels)
            && MediaFoundation.SetNumberAttribute(format, s_bitsPerSampleAttribute, bitsPerSample)
            && MediaFoundation.SetNumberAttribute(format, s_bytesPerFrameAttribute, bytesPerFrame)
            && MediaFoundation.SetNumberAttribute(format, s_bytesPerSecondAttribute, samplesPerSecond * bytesPerFrame))
        {
            return format;
        }

        _ = Marshal.Release(format);
        return 0;
    }

    private bool TryEncodeSpeechAudioChunks(NativeSpeechSynthesizer synthesizer, string text, SpeechAudioFormat sourceFormat, nint writer, uint streamIndex, uint outputBytesPerFrame, AudioResampler? resampler, out long endTimeTicks)
    {
        endTimeTicks = 0;
        if (_pcmBuffer is null)
        {
            _pcmBuffer = SpeechPcmBuffer.Create(sourceFormat.BytesPerFrame);
            if (_pcmBuffer is null)
            {
                return false;
            }
        }
        else
        {
            if (!_pcmBuffer.IsSynthesisStopped && !synthesizer.CancelPcmAudio(_pcmBuffer))
            {
                return false;
            }

            if (!_pcmBuffer.Reset(sourceFormat.BytesPerFrame))
            {
                return false;
            }
        }

        SpeechPcmBuffer pcm = _pcmBuffer;
        long inputFrames = 0;
        long inputTimeTicks = 0;
        long outputFrames = 0;
        long outputTimeTicks = 0;
        bool synthesisStarted = false;
        bool outputDetached = false;
        try
        {
            nint completionEvent = synthesizer.StartPcmAudio(text, pcm);
            if (completionEvent is 0)
            {
                return false;
            }

            synthesisStarted = true;
            while (pcm.TryRead(completionEvent, out PcmAudioChunk chunk))
            {
                nint sample = 0;
                try
                {
                    sample = CreatePcmSample(chunk.Buffer);
                    if (sample is 0)
                    {
                        return false;
                    }

                    uint frameCount = (uint)(chunk.ByteCount / sourceFormat.BytesPerFrame);
                    if (resampler is null)
                    {
                        if (!SetChunkTiming(sample, frameCount, _cachedSamplesPerSecond, ref outputFrames, ref outputTimeTicks)
                            || !MediaFoundation.WriteAudioChunk(writer, streamIndex, sample))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!SetChunkTiming(sample, frameCount, (uint)sourceFormat.SamplesPerSecond, ref inputFrames, ref inputTimeTicks)
                            || !resampler.SubmitInputSample(sample)
                            || !WriteResampledChunks(resampler, writer, streamIndex, _cachedSamplesPerSecond, outputBytesPerFrame, ref outputFrames, ref outputTimeTicks))
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    ReleaseNativeObject(sample);
                    _ = Marshal.Release(chunk.Buffer);
                }
            }

            if (!pcm.CompletedSuccessfully)
            {
                return false;
            }

            bool synthesisSucceeded = synthesizer.FinishPcmAudio();
            outputDetached = true;
            if (!synthesisSucceeded)
            {
                return false;
            }

            if (resampler is not null && (!resampler.CompleteInput()
                || !WriteResampledChunks(resampler, writer, streamIndex, _cachedSamplesPerSecond, outputBytesPerFrame, ref outputFrames, ref outputTimeTicks)))
            {
                return false;
            }

            endTimeTicks = outputTimeTicks;
            return true;
        }
        finally
        {
            if (synthesisStarted && !outputDetached)
            {
                _ = synthesizer.CancelPcmAudio(pcm);
            }

            _ = pcm.ReleaseBuffers();
        }
    }

    private static nint CreatePcmSample(nint buffer)
    {
        int result = WinApi.NativeMethods.MFCreateSample(out nint sample);
        if (result < 0)
        {
            LoggerManager.Logger.Error("Couldn't create a PCM audio sample: {HResult}", result);
            return 0;
        }

        if (MediaFoundation.AttachBufferToAudioChunk(sample, buffer))
        {
            return sample;
        }

        _ = Marshal.Release(sample);
        return 0;
    }

    private static bool SetChunkTiming(nint sample, uint frameCount, uint samplesPerSecond, ref long totalFrames, ref long timeTicks)
    {
        long startTimeTicks = timeTicks;
        totalFrames += frameCount;
        timeTicks = totalFrames * TimeSpan.TicksPerSecond / samplesPerSecond;
        return MediaFoundation.SetAudioChunkStartTimeTicks(sample, startTimeTicks)
            && MediaFoundation.SetAudioChunkDurationTicks(sample, timeTicks - startTimeTicks);
    }

    private static bool WriteResampledChunks(AudioResampler resampler, nint writer, uint streamIndex, uint samplesPerSecond, uint bytesPerFrame, ref long totalFrames, ref long timeTicks)
    {
        while (true)
        {
            if (!resampler.TryReadOutputSample(out nint sample))
            {
                return false;
            }

            if (sample is 0)
            {
                return true;
            }

            try
            {
                if (!MediaFoundation.TryGetAudioChunkByteCount(sample, out uint byteCount))
                {
                    return false;
                }

                if (byteCount > 0)
                {
                    if (byteCount % bytesPerFrame is not 0)
                    {
                        LoggerManager.Logger.Error("PCM chunk contains an incomplete sample frame");
                        return false;
                    }

                    if (!SetChunkTiming(sample, byteCount / bytesPerFrame, samplesPerSecond, ref totalFrames, ref timeTicks)
                        || !MediaFoundation.WriteAudioChunk(writer, streamIndex, sample))
                    {
                        return false;
                    }
                }
            }
            finally
            {
                _ = Marshal.Release(sample);
            }
        }
    }

    private static unsafe bool WriteEndingSilence(nint mp3Writer, uint audioStreamIndex, uint samplesPerSecond, uint bytesPerFrame, long startTimeTicks)
    {
        // Decoded-output tests found clipped speech without padding; four silent MP3 frames preserved the ending.
        const uint paddingFrames = 4;
        const uint samplesPerFrame = 1152;
        const uint silentSampleCount = paddingFrames * samplesPerFrame; // MPEG-1 Layer III has 1,152 samples per MP3 frame.
        uint byteCount = silentSampleCount * bytesPerFrame;
        if (!MediaFoundation.CheckResult(WinApi.NativeMethods.MFCreateMemoryBuffer(byteCount, out nint buffer), "Creating the audio padding buffer"))
        {
            return false;
        }

        nint audioChunk = 0;
        try
        {
            nint bufferDataAddress = MediaFoundation.LockBufferData(buffer);
            if (bufferDataAddress is 0)
            {
                return false;
            }

            new Span<byte>((void*)bufferDataAddress, (int)byteCount).Clear();
            return MediaFoundation.UnlockBufferData(buffer)
                && MediaFoundation.SetBufferByteCount(buffer, byteCount)
                && MediaFoundation.CheckResult(WinApi.NativeMethods.MFCreateSample(out audioChunk), "Creating the audio padding chunk")
                && MediaFoundation.AttachBufferToAudioChunk(audioChunk, buffer)
                && MediaFoundation.SetAudioChunkStartTimeTicks(audioChunk, startTimeTicks)
                && MediaFoundation.SetAudioChunkDurationTicks(audioChunk, silentSampleCount * TimeSpan.TicksPerSecond / samplesPerSecond)
                && MediaFoundation.WriteAudioChunk(mp3Writer, audioStreamIndex, audioChunk);
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

        if (!MediaFoundation.CheckResult(WinApi.NativeMethods.MFTranscodeGetAudioOutputAvailableTypes(in s_mp3AudioEncoding, EnumerateAllTransforms, 0, out nint availableFormats), "Finding MP3 output formats"))
        {
            return 0;
        }

        try
        {
            if (!MediaFoundation.TryGetAvailableFormatCount(availableFormats, out uint count))
            {
                return 0;
            }

            for (uint index = 0; index < count; ++index)
            {
                nint availableFormat = MediaFoundation.GetAvailableFormat(availableFormats, index);
                if (availableFormat is 0)
                {
                    return 0;
                }

                try
                {
                    if (!MediaFoundation.TryReadNumberAttribute(availableFormat, s_samplesPerSecondAttribute, out uint samplesPerSecond))
                    {
                        return 0;
                    }

                    if (samplesPerSecond != outputSamplesPerSecond)
                    {
                        continue;
                    }

                    if (!MediaFoundation.TryReadNumberAttribute(availableFormat, s_channelCountAttribute, out uint channels))
                    {
                        return 0;
                    }

                    if (channels != outputChannelCount)
                    {
                        continue;
                    }

                    if (!MediaFoundation.TryReadNumberAttribute(availableFormat, s_bytesPerSecondAttribute, out uint bytesPerSecond))
                    {
                        return 0;
                    }

                    if (bytesPerSecond is Mp3BytesPerSecond)
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
