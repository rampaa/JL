using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using JL.Core.Config;
using JL.Core.Frontend;
using JL.Core.Utilities;

namespace JL.Core.External;

public static class MpvUtils
{
    private static readonly ReadOnlyMemory<byte> s_getPausePropertyCommand = TextUtils.Utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"get_property\",\"pause\"]}\n");
    private static readonly ReadOnlyMemory<byte> s_pauseCommand = TextUtils.Utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"set_property\",\"pause\",true]}\n");
    private static readonly ReadOnlyMemory<byte> s_unpauseCommand = TextUtils.Utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"set_property\",\"pause\",false]}\n");

    private static long s_lastPausedByJLTimestamp;
    private static bool s_pausedByJL; // = false

    private static readonly SemaphoreSlim s_semaphoreSlim = new(1, 1);

    public static async Task PausePlayback(bool previouslyPausedByJL = false)
    {
        await s_semaphoreSlim.WaitAsync().ConfigureAwait(false);
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(2));
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        try
        {
            NamedPipeClientStream pipeClient = new(".", CoreConfigManager.Instance.MpvNamedPipePath, PipeDirection.InOut, PipeOptions.Asynchronous);
            await using (pipeClient.ConfigureAwait(false))
            {
                await pipeClient.ConnectAsync(500, cancellationToken).ConfigureAwait(false);
                await pipeClient.WriteAsync(s_getPausePropertyCommand, cancellationToken).ConfigureAwait(false);

                using StreamReader reader = new(pipeClient);
                string? responseJson = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (responseJson is null)
                {
                    return;
                }

                using JsonDocument jsonDocument = JsonDocument.Parse(responseJson);
                bool isPaused = jsonDocument.RootElement.GetProperty("data").GetBoolean();

                // TODO: Uncomment this for .NET 10
                //bool isPaused = JsonElement.Parse(responseJson).GetProperty("data").GetBoolean();
                if (!isPaused)
                {
                    await pipeClient.WriteAsync(s_pauseCommand, cancellationToken).ConfigureAwait(false);

                    s_pausedByJL = true;
                    s_lastPausedByJLTimestamp = Stopwatch.GetTimestamp();
                }
                else if (!previouslyPausedByJL)
                {
                    if (Stopwatch.GetElapsedTime(s_lastPausedByJLTimestamp).TotalMilliseconds > 300)
                    {
                        s_pausedByJL = false;
                    }
                    else
                    {
                        s_lastPausedByJLTimestamp = Stopwatch.GetTimestamp();
                    }
                }
            }
        }
        catch (Exception ex) when (ex is TimeoutException || (ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            s_pausedByJL = false;
            LoggerManager.Logger.Warning("Command timed out. Is mpv currently running with its IPC server properly configured? Make sure to add input-ipc-server={MpvNamedPipePath} to your mpv.conf file", CoreConfigManager.Instance.MpvNamedPipePath);
            FrontendManager.Frontend.Notify(NotificationLevel.Warning, $"Command timed out. Is mpv currently running with its IPC server properly configured? Make sure to add input-ipc-server={CoreConfigManager.Instance.MpvNamedPipePath} to your mpv.conf file.");
        }
        catch (Exception ex)
        {
            s_pausedByJL = false;
            LoggerManager.Logger.Error(ex, "An unexpected error occurred while attempting to pause playback in mpv");
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "An unexpected error occurred while attempting to pause playback in mpv");
        }
        finally
        {
            _ = s_semaphoreSlim.Release();
        }
    }

    public static async Task ResumePlayback()
    {
        await s_semaphoreSlim.WaitAsync().ConfigureAwait(false);
        if (!s_pausedByJL)
        {
            _ = s_semaphoreSlim.Release();
            return;
        }

        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(2));
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        try
        {
            NamedPipeClientStream pipeClient = new(".", CoreConfigManager.Instance.MpvNamedPipePath, PipeDirection.Out, PipeOptions.Asynchronous);
            await using (pipeClient.ConfigureAwait(false))
            {
                await pipeClient.ConnectAsync(500, cancellationToken).ConfigureAwait(false);
                await pipeClient.WriteAsync(s_unpauseCommand, cancellationToken).ConfigureAwait(false);
                s_pausedByJL = false;
            }
        }
        catch (Exception ex) when (ex is TimeoutException || (ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            s_pausedByJL = false;
            LoggerManager.Logger.Warning("Command timed out. Is mpv running?");
            FrontendManager.Frontend.Notify(NotificationLevel.Warning, "Command timed out. Is mpv running?");
        }
        catch (Exception ex)
        {
            s_pausedByJL = false;
            LoggerManager.Logger.Error(ex, "An unexpected error occurred while attempting to resume playback in mpv");
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "An unexpected error occurred while attempting to resume playback in mpv");
        }
        finally
        {
            _ = s_semaphoreSlim.Release();
        }
    }
}
