using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using JL.Core.Utilities;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis.Interop;
using Microsoft.Win32;

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
    private const int ChannelsOffset = 2;
    private const int SamplesPerSecondOffset = 4;
    private const int BytesPerSecondOffset = 8;
    private const int BytesPerFrameOffset = 12;
    private const int BitsPerSampleOffset = 14;
    private static readonly Guid s_waveFormatId = new("C31ADBAE-527F-4FF5-A230-F62BB61FF70C");
    private static readonly Type? s_speechVoiceType = Type.GetTypeFromCLSID(new Guid("96749377-3391-11D2-9EE3-00C04F797396"));
    private static readonly Type? s_customStreamType = Type.GetTypeFromCLSID(new Guid("8DBEF13F-1948-4AA8-8CF0-048EEBED95D8"));
    private static readonly Type? s_objectTokenCategoryType = Type.GetTypeFromCLSID(new Guid("A910187F-0C7A-45AC-92CC-59EDAFB77B53"));

    private readonly ISpeechVoice _speechVoice;
    private readonly ISpeechStreamOutput _emptySpeechOutput;
    private readonly IStream _emptyAudioStream;
    private readonly SpeechAudioOutputRouter _audioOutput = new();
    private ISpeechStreamOutput? _speechOutput;
    private string? _selectedVoiceName;
    private nint _speechCompletedEvent;
    private bool _disposed;

    private NativeSpeechSynthesizer(ISpeechVoice speechVoice, ISpeechStreamOutput emptySpeechOutput, IStream emptyAudioStream)
    {
        _speechVoice = speechVoice;
        _emptySpeechOutput = emptySpeechOutput;
        _emptyAudioStream = emptyAudioStream;
    }

    public static NativeSpeechSynthesizer? Create()
    {
        ISpeechVoice? speechVoice = null;
        ISpeechStreamOutput? emptySpeechOutput = null;
        IStream? emptyAudioStream = null;
        try
        {
            speechVoice = CreateSpeechVoice();
            emptySpeechOutput = CreateSpeechStream();
            emptyAudioStream = CreateNativeMemoryStream();
            if (speechVoice is null || emptySpeechOutput is null || emptyAudioStream is null)
            {
                LoggerManager.Logger.Error("Couldn't create the Windows speech synthesizer");
                return null;
            }

            ISpeechAudioFormat format = emptySpeechOutput.Format;
            try
            {
                format.Type = EmptyOutputAudioFormat;
            }
            finally
            {
                _ = Marshal.ReleaseComObject(format);
            }

            emptySpeechOutput.BaseStream = emptyAudioStream;
            NativeSpeechSynthesizer synthesizer = new(speechVoice, emptySpeechOutput, emptyAudioStream);
            speechVoice = null;
            emptySpeechOutput = null;
            emptyAudioStream = null;
            return synthesizer;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't create the Windows speech synthesizer");
            return null;
        }
        finally
        {
            if (emptyAudioStream is not null)
            {
                _ = Marshal.ReleaseComObject(emptyAudioStream);
            }

            if (emptySpeechOutput is not null)
            {
                _ = Marshal.ReleaseComObject(emptySpeechOutput);
            }

            if (speechVoice is not null)
            {
                _ = Marshal.ReleaseComObject(speechVoice);
            }
        }
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

        if (s_objectTokenCategoryType is null)
        {
            return null;
        }

        object? spObjectTokenCategory = Activator.CreateInstance(s_objectTokenCategoryType);
        if (spObjectTokenCategory is null)
        {
            return null;
        }

        ISpeechObjectTokenCategory category = (ISpeechObjectTokenCategory)spObjectTokenCategory;
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

    private static ISpeechVoice? CreateSpeechVoice()
    {
        return s_speechVoiceType is null ? null : (ISpeechVoice?)Activator.CreateInstance(s_speechVoiceType);
    }

    private static ISpeechStreamOutput? CreateSpeechStream()
    {
        return s_customStreamType is null ? null : (ISpeechStreamOutput?)Activator.CreateInstance(s_customStreamType);
    }

    public static SpeechVoiceInfo[]? GetVoices()
    {
        ISpeechVoice? speechVoice = null;
        try
        {
            speechVoice = CreateSpeechVoice();
            if (speechVoice is null)
            {
                LoggerManager.Logger.Error("Couldn't create the Windows speech voice catalog");
                return null;
            }

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

            return voices.OrderBy(static voice => !voice.IsJapanese).ThenBy(static voice => voice.Name, StringComparer.InvariantCulture).ToArray();
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't enumerate Windows speech voices");
            return null;
        }
        finally
        {
            if (speechVoice is not null)
            {
                _ = Marshal.ReleaseComObject(speechVoice);
            }
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

    public bool TrySelectVoice(string name, out bool voiceNotFound)
    {
        voiceNotFound = false;
        if (_disposed || _audioOutput.Destination is not null)
        {
            LoggerManager.Logger.Error("The speech synthesizer is disposed or still in use");
            return false;
        }

        if (_selectedVoiceName == name)
        {
            return true;
        }

        try
        {
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

                                _selectedVoiceName = null;
                                _speechVoice.Voice = voiceRegistration;
                                if (_speechOutput is not null)
                                {
                                    _ = Marshal.ReleaseComObject(_speechOutput);
                                    _speechOutput = null;
                                }

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

            voiceNotFound = true;
            return false;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't select speech voice {VoiceName}", name);
            return false;
        }
    }

    private static bool TryGetPreferredFormat(ISpeechObjectToken voiceRegistration, out SpeechAudioFormat preferredFormat)
    {
        object engine = voiceRegistration.CreateInstance(null, AllComClassContexts);
        try
        {
            if (!SpeechSynthesisInterop.TryGetPreferredAudioFormat(engine, out Guid formatId, out nint nativeFormatAddress))
            {
                preferredFormat = default;
                return false;
            }

            try
            {
                if (formatId != s_waveFormatId || nativeFormatAddress is 0
                    || Marshal.ReadInt16(nativeFormatAddress) is not UncompressedAudioEncoding)
                {
                    LoggerManager.Logger.Error("The speech engine's preferred audio format is not PCM");
                    preferredFormat = default;
                    return false;
                }

                preferredFormat = new SpeechAudioFormat(UncompressedAudioEncoding, Marshal.ReadInt16(nativeFormatAddress, ChannelsOffset), Marshal.ReadInt32(nativeFormatAddress, SamplesPerSecondOffset),
                    Marshal.ReadInt32(nativeFormatAddress, BytesPerSecondOffset), Marshal.ReadInt16(nativeFormatAddress, BytesPerFrameOffset), Marshal.ReadInt16(nativeFormatAddress, BitsPerSampleOffset));
                if (preferredFormat.ChannelCount <= 0 || preferredFormat.SamplesPerSecond <= 0
                    || preferredFormat.BitsPerSample <= 0 || (preferredFormat.BitsPerSample % 8) is not 0
                    || preferredFormat.BytesPerFrame != preferredFormat.ChannelCount * (preferredFormat.BitsPerSample / 8)
                    || preferredFormat.BytesPerSecond != (long)preferredFormat.SamplesPerSecond * preferredFormat.BytesPerFrame)
                {
                    LoggerManager.Logger.Error("The speech engine returned an invalid PCM audio format");
                    preferredFormat = default;
                    return false;
                }

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

    private bool AttachAudioOutput(ISpeechAudioOutput output)
    {
        if (_disposed || _audioOutput.Destination is not null)
        {
            LoggerManager.Logger.Error("The speech synthesizer is disposed or still in use");
            return false;
        }

        try
        {
            if (_speechCompletedEvent is 0)
            {
                _speechCompletedEvent = _speechVoice.SpeakCompleteEvent();
                if (_speechCompletedEvent is 0)
                {
                    LoggerManager.Logger.Error("The speech engine did not provide a completion event");
                    return false;
                }
            }

            if (_speechOutput is null)
            {
                ISpeechStreamOutput? stream = CreateSpeechStream();
                if (stream is null)
                {
                    return false;
                }

                try
                {
                    SetPreferredOutputFormat(stream);
                    stream.BaseStream = _audioOutput;
                    _speechOutput = stream;
                    stream = null;
                }
                finally
                {
                    if (stream is not null)
                    {
                        _ = Marshal.ReleaseComObject(stream);
                    }
                }
            }

            _audioOutput.Destination = output;
            _speechVoice.AudioOutputStream = _speechOutput;
            return true;
        }
        catch (Exception ex)
        {
            _audioOutput.Destination = null;
            LoggerManager.Logger.Error(ex, "Couldn't attach the speech audio output");
            return false;
        }
    }

    private void SetPreferredOutputFormat(ISpeechStreamOutput stream)
    {
        SpeechAudioFormat audioFormat = PreferredAudioFormat;
        ISpeechAudioFormat format = stream.Format;
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

    public bool GenerateSpeech(string text, SpeechAudioSource output)
    {
        if (!AttachAudioOutput(output))
        {
            return false;
        }

        bool succeeded = false;
        try
        {
            output.BeginSynthesis();
            _ = _speechVoice.Speak(text, SpeakAsynchronously | SpeakPlainText);
            if (WaitForSpeechToFinishOrStop(output))
            {
                output.MarkSynthesisStopped();
                if (!output.CancellationToken.IsCancellationRequested && CheckSynthesisResult())
                {
                    succeeded = output.CompleteSynthesis();
                }
            }
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't synthesize speech");
        }
        finally
        {
            if (!output.IsSynthesisStopped)
            {
                if (!output.CancellationToken.IsCancellationRequested)
                {
                    output.ReportSynthesisFailure();
                }

                output.Dispose();
                if (WaitForSpeechToFinishOrStop(output))
                {
                    output.MarkSynthesisStopped();
                }
            }

            if (output.IsSynthesisStopped && !DetachAudioOutput())
            {
                succeeded = false;
            }
        }

        return succeeded;
    }

    private bool DetachAudioOutput()
    {
        try
        {
            // SAPI retains its output until another stream replaces it.
            _speechVoice.AudioOutputStream = _emptySpeechOutput;
            return true;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't detach the speech audio output");
            return false;
        }
        finally
        {
            _audioOutput.Destination = null;
        }
    }

    public nint StartPcmAudio(string text, SpeechPcmBuffer output)
    {
        if (!AttachAudioOutput(output))
        {
            return 0;
        }

        try
        {
            output.BeginSynthesis();
            _ = _speechVoice.Speak(text, SpeakAsynchronously | SpeakPlainText);
            return _speechCompletedEvent;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't start speech synthesis");
            _ = CancelPcmAudio(output);
            return 0;
        }
    }

    public bool FinishPcmAudio()
    {
        if (_disposed)
        {
            return false;
        }

        bool succeeded = false;
        try
        {
            succeeded = CheckSynthesisResult();
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't read the speech synthesis result");
        }
        finally
        {
            if (!DetachAudioOutput())
            {
                succeeded = false;
            }
        }

        return succeeded;
    }

    public bool CancelPcmAudio(SpeechPcmBuffer output)
    {
        output.Cancel();
        if (!output.IsSynthesisStopped)
        {
            if (WinApi.NativeMethods.WaitForSingleObject(_speechCompletedEvent, InfiniteWait) is not CompletedWait)
            {
                LoggerManager.Logger.Error("Waiting for speech synthesis to stop failed with Windows error {ErrorCode}", Marshal.GetLastPInvokeError());
                return false;
            }

            output.MarkSynthesisStopped();
        }

        return DetachAudioOutput();
    }

    private unsafe bool WaitForSpeechToFinishOrStop(SpeechAudioSource output)
    {
        nint* completionSignals = stackalloc nint[2] { _speechCompletedEvent, output.CancellationToken.WaitHandle.SafeWaitHandle.DangerousGetHandle() };
        uint waitResult = WinApi.NativeMethods.WaitForMultipleObjects(2, completionSignals, false, InfiniteWait);
        if (waitResult is CancelledWait)
        {
            try
            {
                // Keep engine cancellation off the player's thread.
                _ = _speechVoice.Speak("", SpeakAsynchronously | PurgeBeforeSpeaking);
            }
            catch (Exception ex)
            {
                LoggerManager.Logger.Error(ex, "Couldn't stop speech synthesis");
                return false;
            }

            waitResult = WinApi.NativeMethods.WaitForSingleObject(_speechCompletedEvent, InfiniteWait);
        }

        if (waitResult is not CompletedWait)
        {
            LoggerManager.Logger.Error("Waiting for speech synthesis failed with Windows error {ErrorCode}", Marshal.GetLastPInvokeError());
            return false;
        }

        return true;
    }

    private bool CheckSynthesisResult()
    {
        ISpeechVoiceStatus status = _speechVoice.Status;
        try
        {
            return MediaFoundation.CheckResult(status.LastResult, "Synthesizing speech");
        }
        finally
        {
            _ = Marshal.ReleaseComObject(status);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_audioOutput.Destination is SpeechPcmBuffer { IsSynthesisStopped: false }
            or SpeechAudioSource { IsSynthesisStopped: false })
        {
            LoggerManager.Logger.Error("Cannot dispose the speech synthesizer while it is running");
            return;
        }

        _disposed = true;
        _ = Marshal.ReleaseComObject(_speechVoice);
        _audioOutput.Destination = null;
        if (_speechOutput is not null)
        {
            _ = Marshal.ReleaseComObject(_speechOutput);
            _speechOutput = null;
        }

        _ = Marshal.ReleaseComObject(_emptySpeechOutput);
        _ = Marshal.ReleaseComObject(_emptyAudioStream);
    }

    private static IStream? CreateNativeMemoryStream()
    {
        nint nativeInterfaceAddress = WinApi.NativeMethods.SHCreateMemStream(0, 0);
        if (nativeInterfaceAddress is 0)
        {
            LoggerManager.Logger.Error("Couldn't create the empty speech output stream");
            return null;
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
