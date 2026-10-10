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
    private static MediaSource? s_mediaSource;
    private static InMemoryRandomAccessStream? s_mediaStream;
    private static MediaPlayer? AudioPlayer
    {
        get => Volatile.Read(ref s_audioPlayer);
        set => Volatile.Write(ref s_audioPlayer, value);
    }

    private static readonly SemaphoreSlim s_audioPlayerSemaphoreSlim = new(1, 1);

    private static long s_lastAudioPlayTimestamp;

    public static async Task PlayAudio(byte[] audio, string audioFormat)
    {
        await s_audioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            DisposeCurrentMedia();

            try
            {
                s_mediaStream = new InMemoryRandomAccessStream();
                using (DataWriter writer = new(s_mediaStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(audio);
                    _ = await writer.StoreAsync().AsTask().ConfigureAwait(false);
                    using IOutputStream outputStream = writer.DetachStream();
                }

                s_mediaStream.Seek(0);
                s_mediaSource = MediaSource.CreateFromStream(s_mediaStream, MimeTypeFor(audioFormat));
            }
            catch (Exception ex)
            {
                DisposeCurrentMedia();
                LoggerManager.Logger.Error(ex, "Error decoding audio: {Audio}, audio format: {AudioFormat}", JsonSerializer.Serialize(audio, JsonOptions.DefaultJso), audioFormat);
                NotificationManager.Notify(NotificationLevel.Error, "Error playing audio. Check the logs for more details.");
                return;
            }

            MediaPlayer mediaPlayer = new();
            AudioPlayer = mediaPlayer;
            mediaPlayer.MediaFailed += static async (player, args) =>
            {
                await DisposeMedia(player, args).ConfigureAwait(false);
            };
            mediaPlayer.MediaEnded += static async (player, _) =>
            {
                await DisposeMedia(player, null).ConfigureAwait(false);
            };

            mediaPlayer.Source = s_mediaSource;
            mediaPlayer.Play();
        }
        catch (Exception ex)
        {
            DisposeCurrentMedia();
            LoggerManager.Logger.Error(ex, "Error playing audio: {Audio}, audio format: {AudioFormat}", JsonSerializer.Serialize(audio, JsonOptions.DefaultJso), audioFormat);
            NotificationManager.Notify(NotificationLevel.Error, "Error playing audio. Check the logs for more details.");
        }
        finally
        {
            _ = s_audioPlayerSemaphoreSlim.Release();
        }
    }

    private static async Task DisposeMedia(MediaPlayer player, MediaPlayerFailedEventArgs? args)
    {
        await s_audioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            if (s_audioPlayer != player)
            {
                return;
            }

            try
            {
                if (args is not null)
                {
                    LoggerManager.Logger.Error("MediaPlayer failed: {Error} - {Message}", args.Error, args.ErrorMessage);
                    NotificationManager.Notify(NotificationLevel.Error, "Error playing audio. Check the logs for more details.");
                }
            }
            finally
            {
                DisposeCurrentMedia();
            }
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

    private static void DisposeCurrentMedia()
    {
        MediaPlayer? player = s_audioPlayer;
        AudioPlayer = null;
        MediaSource? source = s_mediaSource;
        InMemoryRandomAccessStream? mediaStream = s_mediaStream;
        s_mediaSource = null;
        s_mediaStream = null;
        try
        {
            player?.Dispose();
        }
        finally
        {
            try
            {
                source?.Dispose();
            }
            finally
            {
                mediaStream?.Dispose();
            }
        }
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
        if (IsPlaying() && Stopwatch.GetElapsedTime(s_lastAudioPlayTimestamp).TotalMilliseconds < 300)
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
            s_audioPlayer?.Pause();
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
