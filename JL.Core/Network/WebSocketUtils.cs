using System.Net.WebSockets;
using System.Text;
using JL.Core.Config;
using JL.Core.Statistics;
using JL.Core.Utilities;

namespace JL.Core.Network;

public static class WebSocketUtils
{
    private static Task? s_webSocketTask;
    private static CancellationTokenSource? s_webSocketCancellationTokenSource;

    private static readonly Encoding s_utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    public static bool Connected => !s_webSocketTask?.IsCompleted ?? false;

    public static void HandleWebSocket()
    {
        if (!CoreConfigManager.CaptureTextFromWebSocket)
        {
            s_webSocketTask = null;
        }
        else if (s_webSocketTask is null)
        {
            s_webSocketCancellationTokenSource?.Dispose();
            s_webSocketCancellationTokenSource = new CancellationTokenSource();
            ListenWebSocket(s_webSocketCancellationTokenSource.Token);
        }
        else
        {
            s_webSocketCancellationTokenSource!.Cancel();
            s_webSocketCancellationTokenSource.Dispose();
            s_webSocketCancellationTokenSource = new CancellationTokenSource();
            ListenWebSocket(s_webSocketCancellationTokenSource.Token);
        }
    }

    private static void ListenWebSocket(CancellationToken cancellationToken)
    {
        s_webSocketTask = Task.Run(async () =>
        {
            do
            {
                try
                {
                    using ClientWebSocket webSocketClient = new();
                    await webSocketClient.ConnectAsync(CoreConfigManager.WebSocketUri, CancellationToken.None).ConfigureAwait(false);
                    Memory<byte> buffer = new byte[1024];

                    while (CoreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested && webSocketClient.State is WebSocketState.Open)
                    {
                        try
                        {
                            ValueWebSocketReceiveResult result = await webSocketClient.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                            if (!CoreConfigManager.CaptureTextFromWebSocket || cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            if (result.MessageType is WebSocketMessageType.Text)
                            {
                                using MemoryStream memoryStream = new();
                                await memoryStream.WriteAsync(buffer[..result.Count], cancellationToken).ConfigureAwait(false);

                                while (!result.EndOfMessage)
                                {
                                    result = await webSocketClient.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                                    await memoryStream.WriteAsync(buffer[..result.Count], cancellationToken).ConfigureAwait(false);
                                }

                                _ = memoryStream.Seek(0, SeekOrigin.Begin);

                                string text = s_utf8NoBom.GetString(memoryStream.ToArray());
                                _ = Task.Run(async () => await Utils.Frontend.CopyFromWebSocket(text).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                            }
                        }
                        catch (WebSocketException webSocketException)
                        {
                            if (!CoreConfigManager.AutoReconnectToWebSocket)
                            {
                                if (!CoreConfigManager.CaptureTextFromClipboard)
                                {
                                    StatsUtils.StatsStopWatch.Stop();
                                    StatsUtils.StopStatsTimer();
                                }
                            }
                            else
                            {
                                if (CoreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested)
                                {
                                    await Task.Delay(200).ConfigureAwait(false);
                                }
                            }

                            if (CoreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested)
                            {
                                Utils.Logger.Warning(webSocketException, "WebSocket server is closed unexpectedly");
                                Utils.Frontend.Alert(AlertLevel.Error, "WebSocket server is closed");
                            }

                            break;
                        }
                    }
                }

                catch (WebSocketException webSocketException)
                {
                    if (!CoreConfigManager.AutoReconnectToWebSocket)
                    {
                        if (!CoreConfigManager.CaptureTextFromClipboard)
                        {
                            StatsUtils.StatsStopWatch.Stop();
                            StatsUtils.StopStatsTimer();
                        }

                        if (CoreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested)
                        {
                            Utils.Logger.Warning(webSocketException, "Couldn't connect to the WebSocket server, probably because it is not running");
                            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't connect to the WebSocket server, probably because it is not running");
                        }
                    }
                    else
                    {
                        Utils.Logger.Verbose(webSocketException, "Couldn't connect to the WebSocket server, probably because it is not running");
                        if (CoreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(200).ConfigureAwait(false);
                        }
                    }
                }
            }
            while (CoreConfigManager.AutoReconnectToWebSocket && CoreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested);
        }, cancellationToken);
    }
}
