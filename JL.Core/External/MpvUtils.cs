using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using JL.Core.Config;
using JL.Core.Network;
using JL.Core.Utilities;

namespace JL.Core.External;

public static class MpvUtils
{
    private static readonly ReadOnlyMemory<byte> s_getPausePropertyCommand = NetworkUtils.s_utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"get_property\",\"pause\"]}\n");
    private static readonly ReadOnlyMemory<byte> s_pauseCommand = NetworkUtils.s_utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"set_property\",\"pause\",true]}\n");
    private static readonly ReadOnlyMemory<byte> s_unpauseCommand = NetworkUtils.s_utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"set_property\",\"pause\",false]}\n");

    private static long s_lastPausedByJLTimestamp;
    private static bool s_pausedByJL; // = false

    private static readonly SemaphoreSlim s_semaphoreSlim = new(1, 1);

    public static async Task PausePlayback(bool previouslyPausedByJL = false)
    {
        await s_semaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            NamedPipeClientStream pipeClient = new(".", CoreConfigManager.Instance.MpvNamedPipePath, PipeDirection.InOut, PipeOptions.Asynchronous);
            await using (pipeClient.ConfigureAwait(false))
            {
                await pipeClient.ConnectAsync(500).ConfigureAwait(false);
                await pipeClient.WriteAsync(s_getPausePropertyCommand).ConfigureAwait(false);

                using StreamReader reader = new(pipeClient);
                string? responseJson = await reader.ReadLineAsync().ConfigureAwait(false);
                if (responseJson is null)
                {
                    return;
                }

                using JsonDocument responseJsonDocument = JsonDocument.Parse(responseJson);
                bool isPaused = responseJsonDocument.RootElement.GetProperty("data").GetBoolean();
                if (!isPaused)
                {
                    await pipeClient.WriteAsync(s_pauseCommand).ConfigureAwait(false);

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
        catch (TimeoutException)
        {
            s_pausedByJL = false;
            LoggerManager.Logger.Warning("Connection timed out. Is mpv currently running with its IPC server properly configured? Make sure to add input-ipc-server={MpvNamedPipePath} to your mpv.conf file.", CoreConfigManager.Instance.MpvNamedPipePath);
        }
        catch (Exception ex)
        {
            s_pausedByJL = false;
            LoggerManager.Logger.Error(ex, "An unexpected error occurred while attempting to pause playback in mpv");
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

        try
        {
            NamedPipeClientStream pipeClient = new(".", CoreConfigManager.Instance.MpvNamedPipePath, PipeDirection.Out, PipeOptions.Asynchronous);
            await using (pipeClient.ConfigureAwait(false))
            {
                await pipeClient.ConnectAsync(500).ConfigureAwait(false);
                await pipeClient.WriteAsync(s_unpauseCommand).ConfigureAwait(false);
            }
        }
        catch (TimeoutException)
        {
            s_pausedByJL = false;
            LoggerManager.Logger.Warning("Connection timed out. Is mpv running?");
        }
        catch (Exception ex)
        {
            s_pausedByJL = false;
            LoggerManager.Logger.Error(ex, "An unexpected error occurred while attempting to resume playback in mpv");
        }
        finally
        {
            _ = s_semaphoreSlim.Release();
        }
    }
}
