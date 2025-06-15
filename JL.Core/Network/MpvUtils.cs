using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using JL.Core.Config;
using JL.Core.Utilities;

namespace JL.Core.Network;
public static class MpvUtils
{
    public static long LastPausedByJLTimestamp { get; set; }
    private static bool s_pausedByJL; // = false

    public static async Task PausePlayback(bool previouslyPausedByJL = false)
    {
        try
        {
            NamedPipeClientStream pipeClient = new(".", CoreConfigManager.Instance.MpvNamedPipePath, PipeDirection.InOut);
            await using (pipeClient.ConfigureAwait(false))
            {
                await pipeClient.ConnectAsync(1500).ConfigureAwait(false);

                byte[] getPausePropertyCommand = NetworkUtils.s_utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"get_property\",\"pause\"]}\n");
                await pipeClient.WriteAsync(getPausePropertyCommand).ConfigureAwait(false);

                byte[] buffer = new byte[128];
                int bytesRead = await pipeClient.ReadAsync(buffer).ConfigureAwait(false);
                string response = NetworkUtils.s_utf8NoBom.GetString(buffer, 0, bytesRead);
                bool isPaused = JsonDocument.Parse(response).RootElement.GetProperty("data").GetBoolean();
                if (!isPaused)
                {
                    byte[] pauseCommand = NetworkUtils.s_utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"set_property\",\"pause\",true]}\n");
                    await pipeClient.WriteAsync(pauseCommand).ConfigureAwait(false);

                    s_pausedByJL = true;
                    LastPausedByJLTimestamp = Stopwatch.GetTimestamp();
                }
                else if (!previouslyPausedByJL)
                {
                    if (Stopwatch.GetElapsedTime(LastPausedByJLTimestamp).TotalMilliseconds > 300)
                    {
                        s_pausedByJL = false;
                    }
                    else
                    {
                        LastPausedByJLTimestamp = Stopwatch.GetTimestamp();
                    }
                }
            }
        }
        catch (TimeoutException)
        {
            s_pausedByJL = false;
            Utils.Logger.Warning($"Connection timed out. Is MPV currently running with its IPC server properly configured? Make sure to add input-ipc-server={CoreConfigManager.Instance.MpvNamedPipePath} to your mpv.conf file.");
        }
        catch (Exception ex)
        {
            s_pausedByJL = false;
            Utils.Logger.Error(ex, "An unexpecteed error occurred while attempting to puase playback in MPV");
        }
    }

    public static async Task ResumePlayback()
    {
        if (!s_pausedByJL)
        {
            return;
        }

        try
        {
            NamedPipeClientStream pipeClient = new(".", CoreConfigManager.Instance.MpvNamedPipePath, PipeDirection.Out);
            await using (pipeClient.ConfigureAwait(false))
            {
                await pipeClient.ConnectAsync(1500).ConfigureAwait(false);
                byte[] pauseCommand = NetworkUtils.s_utf8NoBom.GetBytes(/*lang=json,strict*/ "{\"command\":[\"set_property\",\"pause\",false]}\n");
                await pipeClient.WriteAsync(pauseCommand).ConfigureAwait(false);
            }
        }
        catch (TimeoutException)
        {
            s_pausedByJL = false;
            Utils.Logger.Warning("Connection timed out. Is MPV running?");
        }
        catch (Exception ex)
        {
            s_pausedByJL = false;
            Utils.Logger.Error(ex, "An unexpecteed error occurred while attempting to resume playback in MPV");
        }
    }
}
