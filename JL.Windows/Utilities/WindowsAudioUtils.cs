using System.Diagnostics;
using System.IO;
using System.Text.Json;
using JL.Core;
using JL.Core.Frontend;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI.Notification;
using JL.Windows.SpeechSynthesis;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using MediaPlayer = Windows.Media.Playback.MediaPlayer;

namespace JL.Windows.Utilities;

internal static class WindowsAudioUtils
{
    private static MediaPlayer? s_audioPlayer;
    private static MediaPlayer? AudioPlayer => Volatile.Read(ref s_audioPlayer);

    private static readonly SemaphoreSlim s_audioPlayerSemaphoreSlim = new(1, 1);

    private static long s_lastAudioPlayTimestamp;

    public static async Task PlayAudio(byte[] audio, string audioFormat)
    {
        await s_audioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            MediaPlayer? oldPlayer = Interlocked.Exchange(ref s_audioPlayer, null);
            if (oldPlayer is not null)
            {
                oldPlayer.Pause();
                oldPlayer.Source = null;
                oldPlayer.Dispose();
            }

            MediaSource mediaSource;
            InMemoryRandomAccessStream? mediaStream = null;

            try
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                mediaStream = await ToRandomAccessStreamAsync(audio).ConfigureAwait(false);
                mediaSource = MediaSource.CreateFromStream(mediaStream, MimeTypeFor(audioFormat));
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
            catch (Exception ex)
            {
                mediaStream?.Dispose(); // avoid leaking the stream if CreateFromStream throws
                LoggerManager.Logger.Error(ex, "Error decoding audio: {Audio}, audio format: {AudioFormat}", JsonSerializer.Serialize(audio, JsonOptions.DefaultJso), audioFormat);
                NotificationManager.Notify(NotificationLevel.Error, "Error playing audio. Check the logs for more details.");
                return;
            }

            MediaPlayer mediaPlayer = new() { AutoPlay = true, Source = mediaSource };
            _ = Interlocked.Exchange(ref s_audioPlayer, mediaPlayer);

            IRandomAccessStream? capturedMediaStream = mediaStream;

            mediaPlayer.MediaFailed += async (_, args) =>
            {
                LoggerManager.Logger.Error("MediaPlayer failed: {Error} - {Message}", args.Error, args.ErrorMessage);
                NotificationManager.Notify(NotificationLevel.Error, "Error playing audio. Check the logs for more details.");
                await DisposeMedia(mediaPlayer, mediaSource, capturedMediaStream).ConfigureAwait(false);
            };

            mediaPlayer.MediaEnded += (_, _) =>
            {
                _ = DisposeMedia(mediaPlayer, mediaSource, capturedMediaStream);
            };
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error playing audio: {Audio}, audio format: {AudioFormat}", JsonSerializer.Serialize(audio, JsonOptions.DefaultJso), audioFormat);
            NotificationManager.Notify(NotificationLevel.Error, "Error playing audio. Check the logs for more details.");
        }
        finally
        {
            _ = s_audioPlayerSemaphoreSlim.Release();
        }
    }

    private static async Task DisposeMedia(MediaPlayer player, MediaSource source, IRandomAccessStream? mediaStream)
    {
        await s_audioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            if (s_audioPlayer == player)
            {
                _ = Interlocked.Exchange(ref s_audioPlayer, null);
            }

            player.Dispose();
            source.Dispose();
            mediaStream?.Dispose();
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error while disposing audio player");
        }
        finally
        {
            _ = s_audioPlayerSemaphoreSlim.Release();
        }
    }

    private static async Task<InMemoryRandomAccessStream> ToRandomAccessStreamAsync(byte[] data)
    {
        InMemoryRandomAccessStream stream = new();

        using DataWriter writer = new(stream.GetOutputStreamAt(0));
        writer.WriteBytes(data);
        _ = await writer.StoreAsync();
        _ = writer.DetachStream();

        stream.Seek(0);
        return stream;
    }

#pragma warning disable CA1308 // Normalize strings to uppercase
    private static string MimeTypeFor(string audioFormat) => audioFormat switch
    {
        "mp3" => "audio/mpeg",
        "wav" or "wave" => "audio/wav",
        "aac" or "adts" => "audio/aac",
        "m4a" or "mp4" or "mov" or "m4v" => "audio/mp4",
        "wma" or "asf" => "audio/x-ms-wma",
        "3gp" or "3g2" or "3gpp" or "3gp2" => "audio/3gpp",
        "flac" => "audio/flac",
        "mkv" => "audio/x-matroska",
        "ogg" or "oga" => "audio/ogg",
        "opus" => "audio/ogg",
        "webm" => "audio/webm",
        "amr" => "audio/amr",
        "ac3" => "audio/ac3",
        _ => $"audio/{audioFormat.ToLowerInvariant()}"
    };
#pragma warning restore CA1308 // Normalize strings to uppercase

    public static async Task Motivate()
    {
        if (AudioPlayer?.CurrentState is MediaPlayerState.Playing && Stopwatch.GetElapsedTime(s_lastAudioPlayTimestamp).TotalMilliseconds < 300)
        {
            s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();
            return;
        }

        s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();
        try
        {
            string[] filePaths = Directory.GetFiles(Path.Join(AppInfo.ResourcesPath, "Motivation"));
            if (filePaths.Length is 0)
            {
                LoggerManager.Logger.Warning("Motivation folder is empty!");
                NotificationManager.Notify(NotificationLevel.Warning, "Motivation folder is empty!");
                return;
            }

#pragma warning disable CA5394 // Do not use insecure randomness
            string randomFilePath = filePaths[Random.Shared.Next(filePaths.Length)];
#pragma warning restore CA5394 // Do not use insecure randomness

            byte[] audioData = await File.ReadAllBytesAsync(randomFilePath).ConfigureAwait(false);

            await Task.Run(async () =>
            {
                SpeechSynthesisUtils.StopTextToSpeech();
                await PlayAudio(audioData, "mp3").ConfigureAwait(false);
            }).ConfigureAwait(false);

            StatsUtils.IncrementStat(StatType.Imoutos);
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error motivating");
            NotificationManager.Notify(NotificationLevel.Error, "Error motivating. Check the logs for more details.");
        }
    }

    public static bool IsPlaying()
    {
        MediaPlayer? player = AudioPlayer;
        if (player is null)
        {
            return false;
        }

        try
        {
            return player.CurrentState is MediaPlayerState.Playing;
        }
        catch
        {
            return false;
        }
    }

    public static async Task PausePlaying()
    {
        await s_audioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            AudioPlayer?.Pause();
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error while pausing audio player");
        }
        finally
        {
            _ = s_audioPlayerSemaphoreSlim.Release();
        }
    }
}
