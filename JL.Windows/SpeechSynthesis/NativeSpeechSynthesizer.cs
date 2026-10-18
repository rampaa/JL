using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using JL.Core.Utilities;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis.Interop;
using Microsoft.Win32;
using Windows.Storage.Streams;

namespace JL.Windows.SpeechSynthesis;

internal sealed class NativeSpeechSynthesizer : IDisposable
{
    private const int SpeakAsynchronously = 1;
    private const int PurgeBeforeSpeaking = 2;
    private const int SpeakPlainText = 16;
    private const uint InfiniteWait = uint.MaxValue;
    private const uint CompletedWait = 0;
    private const uint CancelledWait = 1;
    private const int DesktopVoiceCatalog = 0;
    private const int VoiceCatalogCount = 2;
    private const int AllComClassContexts = 23;
    private const short UncompressedAudioEncoding = 1;
    private const int EmptyOutputAudioFormat = 22; // SAPI's 22 kHz, 16-bit mono format; no audio is written to this stream.
    private const int PrimaryLanguageIdMask = 0x3FF;
    private const int SeekFromBeginning = 0;
    private const int SkipStreamName = 1;
    private const int DefaultStreamWrappingOptions = 0;
    private const int WaveHeaderLength = 44;
    private const int WaveFormatLength = 16;
    private const int ChannelsOffset = 2;
    private const int SamplesPerSecondOffset = 4;
    private const int BytesPerSecondOffset = 8;
    private const int BytesPerFrameOffset = 12;
    private const int BitsPerSampleOffset = 14;
    private static readonly Guid s_waveFormatId = new("C31ADBAE-527F-4FF5-A230-F62BB61FF70C");
    private readonly ISpeechVoice _speechVoice = CreateSpeechVoice();
    private readonly ISpeechStreamOutput _emptySpeechOutput = (ISpeechStreamOutput)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("8DBEF13F-1948-4AA8-8CF0-048EEBED95D8"), true)!)!;
    private readonly IStream _emptyAudioStream = CreateNativeMemoryStream();
    private string? _selectedVoiceName;
    private nint _speechCompletedEvent;

    private static ReadOnlySpan<byte> RiffSignature => "RIFF"u8;
    private static ReadOnlySpan<byte> WaveFormatSignature => "WAVEfmt "u8;
    private static ReadOnlySpan<byte> DataSignature => "data"u8;

    public NativeSpeechSynthesizer()
    {
        ISpeechAudioFormat format = ((ISpeechStreamBase)_emptySpeechOutput).Format;
        try
        {
            format.Type = EmptyOutputAudioFormat;
        }
        finally
        {
            _ = Marshal.ReleaseComObject(format);
        }

        _emptySpeechOutput.BaseStream = _emptyAudioStream;
    }

    public SpeechAudioFormat PreferredAudioFormat { get; private set; }

    private static ISpeechObjectTokens? GetOneCoreVoices()
    {
        // Windows stores these voices separately from classic desktop speech voices.
        const string path = @"SOFTWARE\Microsoft\Speech_OneCore\Voices";
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(path);
        if (key is null)
        {
            return null;
        }

        ISpeechObjectTokenCategory category = (ISpeechObjectTokenCategory)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("A910187F-0C7A-45AC-92CC-59EDAFB77B53"), true)!)!;
        try
        {
            category.SetId(@"HKEY_LOCAL_MACHINE\" + path, false);
            return category.EnumerateTokens("", "");
        }
        finally
        {
            _ = Marshal.ReleaseComObject(category);
        }
    }

    private static ISpeechVoice CreateSpeechVoice()
    {
        return (ISpeechVoice)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("96749377-3391-11D2-9EE3-00C04F797396"), true)!)!;
    }

    public static SpeechVoiceInfo[] GetVoices()
    {
        ISpeechVoice speechVoice = CreateSpeechVoice();
        try
        {
            List<SpeechVoiceInfo> voices = [];
            for (int voiceCatalogIndex = 0; voiceCatalogIndex < VoiceCatalogCount; ++voiceCatalogIndex)
            {
                ISpeechObjectTokens? voiceRegistrations = voiceCatalogIndex is DesktopVoiceCatalog ? speechVoice.GetVoices("", "") : GetOneCoreVoices();
                if (voiceRegistrations is null)
                {
                    continue;
                }

                try
                {
                    int count = voiceRegistrations.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        ISpeechObjectToken voiceRegistration = voiceRegistrations.Item(i);
                        try
                        {
                            bool isJapanese = IsJapaneseVoice(voiceRegistration.GetAttribute("Language"));
                            voices.Add(new SpeechVoiceInfo(voiceRegistration.GetAttribute("Name"), isJapanese));
                        }
                        finally
                        {
                            _ = Marshal.ReleaseComObject(voiceRegistration);
                        }
                    }
                }
                finally
                {
                    _ = Marshal.ReleaseComObject(voiceRegistrations);
                }
            }

            return voices.OrderBy(static voice => !voice.IsJapanese)
                .ThenBy(static voice => voice.Name, StringComparer.InvariantCulture).ToArray();
        }
        finally
        {
            _ = Marshal.ReleaseComObject(speechVoice);
        }
    }

    private static bool IsJapaneseVoice(string languageIds)
    {
        int separator = languageIds.IndexOf(';', StringComparison.Ordinal);
        ReadOnlySpan<char> firstLanguageId = separator < 0 ? languageIds.AsSpan() : languageIds.AsSpan(0, separator);
        const int japaneseLanguageId = 0x11;
        // Windows stores hexadecimal language IDs; the lower ten bits identify the language, excluding its region.
        return int.TryParse(firstLanguageId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int languageId)
            && (languageId & PrimaryLanguageIdMask) is japaneseLanguageId;
    }

    public bool TrySelectVoice(string name)
    {
        if (_selectedVoiceName == name)
        {
            return true;
        }

        for (int voiceCatalogIndex = 0; voiceCatalogIndex < VoiceCatalogCount; ++voiceCatalogIndex)
        {
            ISpeechObjectTokens? voiceRegistrations = voiceCatalogIndex is DesktopVoiceCatalog ? _speechVoice.GetVoices("", "") : GetOneCoreVoices();
            if (voiceRegistrations is null)
            {
                continue;
            }

            try
            {
                int count = voiceRegistrations.Count;
                for (int i = 0; i < count; ++i)
                {
                    ISpeechObjectToken voiceRegistration = voiceRegistrations.Item(i);
                    try
                    {
                        if (voiceRegistration.GetAttribute("Name") == name)
                        {
                            if (!TryGetPreferredFormat(voiceRegistration, out SpeechAudioFormat format))
                            {
                                return false;
                            }

                            _speechVoice.Voice = voiceRegistration;
                            PreferredAudioFormat = format;
                            _selectedVoiceName = name;
                            return true;
                        }
                    }
                    finally
                    {
                        _ = Marshal.ReleaseComObject(voiceRegistration);
                    }
                }
            }
            finally
            {
                _ = Marshal.ReleaseComObject(voiceRegistrations);
            }
        }

        return false;
    }

    private static bool TryGetPreferredFormat(ISpeechObjectToken voiceRegistration, out SpeechAudioFormat preferredFormat)
    {
        object engine = voiceRegistration.CreateInstance(null, AllComClassContexts);
        try
        {
            SpeechSynthesisInterop.GetPreferredAudioFormat(engine, out Guid formatId, out nint nativeFormatAddress);
            try
            {
                if (formatId != s_waveFormatId || Marshal.ReadInt16(nativeFormatAddress) is not UncompressedAudioEncoding)
                {
                    LoggerManager.Logger.Error("The speech engine's preferred audio format is not PCM");
                    preferredFormat = default;
                    return false;
                }

                preferredFormat = new SpeechAudioFormat(Marshal.ReadInt16(nativeFormatAddress), Marshal.ReadInt16(nativeFormatAddress, ChannelsOffset), Marshal.ReadInt32(nativeFormatAddress, SamplesPerSecondOffset),
                    Marshal.ReadInt32(nativeFormatAddress, BytesPerSecondOffset), Marshal.ReadInt16(nativeFormatAddress, BytesPerFrameOffset), Marshal.ReadInt16(nativeFormatAddress, BitsPerSampleOffset));
                return true;
            }
            finally
            {
                Marshal.FreeCoTaskMem(nativeFormatAddress);
            }
        }
        finally
        {
            _ = Marshal.ReleaseComObject(engine);
        }
    }

    private ISpeechStreamOutput AttachAudioOutput(object output)
    {
        if (_speechCompletedEvent is 0)
        {
            _speechCompletedEvent = _speechVoice.SpeakCompleteEvent();
        }

        ISpeechStreamOutput stream = (ISpeechStreamOutput)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("8DBEF13F-1948-4AA8-8CF0-048EEBED95D8"), true)!)!;
        bool initialized = false;
        try
        {
            SetPreferredOutputFormat(stream);

            stream.BaseStream = output;
            _speechVoice.AudioOutputStream = (ISpeechStreamBase)stream;

            initialized = true;
            return stream;
        }
        finally
        {
            if (!initialized)
            {
                _ = Marshal.ReleaseComObject(stream);
            }
        }
    }

    private void SetPreferredOutputFormat(ISpeechStreamOutput stream)
    {
        SpeechAudioFormat audioFormat = PreferredAudioFormat;
        ISpeechAudioFormat format = ((ISpeechStreamBase)stream).Format;
        try
        {
            ISpeechWaveFormat wave = format.ReadWaveFormat();
            try
            {
                wave.FormatTag = audioFormat.EncodingId;
                wave.Channels = audioFormat.ChannelCount;
                wave.SamplesPerSec = audioFormat.SamplesPerSecond;
                wave.AvgBytesPerSec = audioFormat.BytesPerSecond;
                wave.BlockAlign = audioFormat.BytesPerFrame;
                wave.BitsPerSample = audioFormat.BitsPerSample;
                format.SetWaveFormat(wave);
            }
            finally
            {
                _ = Marshal.ReleaseComObject(wave);
            }
        }
        finally
        {
            _ = Marshal.ReleaseComObject(format);
        }
    }

    public void GenerateSpeech(string text, SpeechAudioSource output)
    {
        ISpeechStreamOutput stream = AttachAudioOutput(output);
        try
        {
            _ = _speechVoice.Speak(text, SpeakAsynchronously | SpeakPlainText);
            if (!WaitForSpeechToFinishOrStop(output))
            {
                output.ReportSynthesisFailure();
                return;
            }

            if (!output.CancellationToken.IsCancellationRequested)
            {
                CheckSynthesisResult();
                output.CompleteSynthesis();
            }
        }
        finally
        {
            DetachAudioOutput(stream);
        }
    }

    private void DetachAudioOutput(ISpeechStreamOutput stream)
    {
        try
        {
            // SAPI retains its output; detach it so the cached voice cannot retain the player's audio buffers.
            _speechVoice.AudioOutputStream = (ISpeechStreamBase)_emptySpeechOutput;
        }
        finally
        {
            _ = Marshal.ReleaseComObject(stream);
        }
    }

    private unsafe bool WaitForSpeechToFinishOrStop(SpeechAudioSource output)
    {
        nint* completionSignals = stackalloc nint[2] { _speechCompletedEvent, output.CancellationToken.WaitHandle.SafeWaitHandle.DangerousGetHandle() };
        uint waitResult = WinApi.NativeMethods.WaitForMultipleObjects(2, completionSignals, false, InfiniteWait);
        if (waitResult is CancelledWait)
        {
            _ = _speechVoice.Speak("", SpeakAsynchronously | PurgeBeforeSpeaking); // Purge on the synthesis worker so stopping the player never waits for the engine.
            waitResult = WinApi.NativeMethods.WaitForSingleObject(_speechCompletedEvent, InfiniteWait);
        }

        if (waitResult is not CompletedWait)
        {
            LoggerManager.Logger.Error("Waiting for speech synthesis failed with Windows error {ErrorCode}", Marshal.GetLastPInvokeError());
            return false;
        }

        return true;
    }

    public IRandomAccessStream? GenerateWaveAudio(string text)
    {
        IStream stream = CreateNativeMemoryStream();
        try
        {
            byte[] header = new byte[WaveHeaderLength];
            stream.Write(header, header.Length, 0);
            ISpeechStreamOutput speechStream = AttachAudioOutput(stream);
            try
            {
                _ = _speechVoice.Speak(text, SpeakAsynchronously | SpeakPlainText);
                if (WinApi.NativeMethods.WaitForSingleObject(_speechCompletedEvent, InfiniteWait) is not CompletedWait)
                {
                    LoggerManager.Logger.Error("Waiting for speech synthesis failed with Windows error {ErrorCode}", Marshal.GetLastPInvokeError());
                    return null;
                }

                CheckSynthesisResult();
            }
            finally
            {
                DetachAudioOutput(speechStream);
            }

            stream.Stat(out STATSTG stat, SkipStreamName);
            int dataLength = checked((int)stat.cbSize - header.Length);
            WriteWaveHeader(header, dataLength);
            stream.Seek(0, SeekFromBeginning, 0);
            stream.Write(header, header.Length, 0);
            stream.Seek(0, SeekFromBeginning, 0);
            return CreateWindowsReadableStream(stream);
        }
        finally
        {
            _ = Marshal.ReleaseComObject(stream);
        }
    }

    private void WriteWaveHeader(Span<byte> header, int dataLength)
    {
        const int riffHeaderLength = 8;
        const int formatSizeOffset = 16;
        const int formatDataOffset = 20;
        const int dataChunkOffset = formatDataOffset + WaveFormatLength;
        RiffSignature.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..], dataLength + WaveHeaderLength - riffHeaderLength);
        WaveFormatSignature.CopyTo(header[riffHeaderLength..]);
        BinaryPrimitives.WriteInt32LittleEndian(header[formatSizeOffset..], WaveFormatLength);
        BinaryPrimitives.WriteInt16LittleEndian(header[formatDataOffset..], UncompressedAudioEncoding);
        BinaryPrimitives.WriteInt16LittleEndian(header[(formatDataOffset + ChannelsOffset)..], PreferredAudioFormat.ChannelCount);
        BinaryPrimitives.WriteInt32LittleEndian(header[(formatDataOffset + SamplesPerSecondOffset)..], PreferredAudioFormat.SamplesPerSecond);
        BinaryPrimitives.WriteInt32LittleEndian(header[(formatDataOffset + BytesPerSecondOffset)..], PreferredAudioFormat.BytesPerSecond);
        BinaryPrimitives.WriteInt16LittleEndian(header[(formatDataOffset + BytesPerFrameOffset)..], PreferredAudioFormat.BytesPerFrame);
        BinaryPrimitives.WriteInt16LittleEndian(header[(formatDataOffset + BitsPerSampleOffset)..], PreferredAudioFormat.BitsPerSample);
        DataSignature.CopyTo(header[dataChunkOffset..]);
        BinaryPrimitives.WriteInt32LittleEndian(header[(WaveHeaderLength - sizeof(int))..], dataLength);
    }

    private static IRandomAccessStream CreateWindowsReadableStream(IStream stream)
    {
        Guid streamInterfaceId = new("905A0FE1-BC53-11DF-8C49-001E4FC686DA");
        nint nativeStreamAddress = Marshal.GetIUnknownForObject(stream);
        try
        {
            Marshal.ThrowExceptionForHR(WinApi.NativeMethods.CreateRandomAccessStreamOverStream(nativeStreamAddress, DefaultStreamWrappingOptions, in streamInterfaceId, out nint nativeInterfaceAddress));
            try
            {
                return WinRT.MarshalInterface<IRandomAccessStream>.FromAbi(nativeInterfaceAddress);
            }
            finally
            {
                _ = Marshal.Release(nativeInterfaceAddress);
            }
        }
        finally
        {
            _ = Marshal.Release(nativeStreamAddress);
        }
    }

    private void CheckSynthesisResult()
    {
        ISpeechVoiceStatus status = _speechVoice.Status;
        try
        {
            Marshal.ThrowExceptionForHR(status.LastResult);
        }
        finally
        {
            _ = Marshal.ReleaseComObject(status);
        }
    }

    public void Dispose()
    {
        _ = Marshal.ReleaseComObject(_speechVoice);
        _ = Marshal.ReleaseComObject(_emptySpeechOutput);
        _ = Marshal.ReleaseComObject(_emptyAudioStream);
    }

    private static IStream CreateNativeMemoryStream()
    {
        nint nativeInterfaceAddress = WinApi.NativeMethods.SHCreateMemStream(0, 0);
        if (nativeInterfaceAddress is 0)
        {
            Marshal.ThrowExceptionForHR(unchecked((int)0x8007000E));
        }

        try
        {
            return (IStream)Marshal.GetObjectForIUnknown(nativeInterfaceAddress);
        }
        finally
        {
            _ = Marshal.Release(nativeInterfaceAddress);
        }
    }
}
