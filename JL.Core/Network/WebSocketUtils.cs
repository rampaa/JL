using System.Net.WebSockets;
using JL.Core.Config;
using JL.Core.Statistics;
using JL.Core.Utilities;

namespace JL.Core.Network;

public static class WebSocketUtils
{
    private static Task? s_webSocketTask;
    private static ClientWebSocket? s_webSocketClient;
    private static CancellationTokenSource? s_webSocketCancellationTokenSource;
    public static bool Connected => s_webSocketClient?.State is WebSocketState.Open;

    public static async Task Disconnect()
    {
        if (s_webSocketClient?.State is WebSocketState.Open)
        {
            await s_webSocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure), CancellationToken.None).ConfigureAwait(false);
        }

        if (s_webSocketCancellationTokenSource is null || s_webSocketTask is null)
        {
            return;
        }

        CoreConfigManager.Instance.CaptureTextFromWebSocket = false;
        await s_webSocketCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        s_webSocketCancellationTokenSource.Dispose();
    }

    public static async Task HandleWebSocket()
    {
        if (s_webSocketClient?.State is WebSocketState.Open)
        {
            await s_webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure), CancellationToken.None).ConfigureAwait(false);
        }

        if (s_webSocketCancellationTokenSource is not null)
        {
            await s_webSocketCancellationTokenSource.CancelAsync().ConfigureAwait(false);
            s_webSocketCancellationTokenSource.Dispose();
            s_webSocketCancellationTokenSource = null;
        }

        if (!CoreConfigManager.Instance.CaptureTextFromWebSocket)
        {
            s_webSocketTask = null;
        }
        else
        {
            s_webSocketCancellationTokenSource = new CancellationTokenSource();
            ListenWebSocket(s_webSocketCancellationTokenSource.Token);
        }
    }

    private static void ListenWebSocket(CancellationToken cancellationToken)
    {
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        s_webSocketTask = Task.Run(async () =>
        {
            do
            {
                try
                {
                    using ClientWebSocket webSocketClient = new();
                    await webSocketClient.ConnectAsync(coreConfigManager.WebSocketUri, cancellationToken).ConfigureAwait(false);
                    s_webSocketClient = webSocketClient;

                    // 256-4096
                    Memory<byte> buffer = new byte[1024 * 4];

                    while (coreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested && webSocketClient.State is WebSocketState.Open)
                    {
                        try
                        {
                            ValueWebSocketReceiveResult result = await webSocketClient.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                            if (!coreConfigManager.CaptureTextFromWebSocket || cancellationToken.IsCancellationRequested)
                            {
                                if (webSocketClient.State is WebSocketState.Open)
                                {
                                    await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure), CancellationToken.None).ConfigureAwait(false);
                                    s_webSocketTask = null;
                                }

                                return;
                            }

                            if (result.MessageType is WebSocketMessageType.Text)
                            {
                                using MemoryStream memoryStream = new();
                                await memoryStream.WriteAsync(buffer[..result.Count], CancellationToken.None).ConfigureAwait(false);

                                while (!result.EndOfMessage)
                                {
                                    result = await webSocketClient.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                                    await memoryStream.WriteAsync(buffer[..result.Count], CancellationToken.None).ConfigureAwait(false);
                                }

                                string text = memoryStream.TryGetBuffer(out ArraySegment<byte> messageBuffer)
                                    ? NetworkUtils.s_utf8NoBom.GetString(messageBuffer)
                                    : NetworkUtils.s_utf8NoBom.GetString(memoryStream.ToArray());

                                _ = Task.Run(() => Utils.Frontend.CopyFromWebSocket(text), CancellationToken.None).ConfigureAwait(false);
                            }
                            else if (result.MessageType is WebSocketMessageType.Close)
                            {
                                Utils.Logger.Information("WebSocket server is closed");
                                break;
                            }
                        }
                        catch (WebSocketException webSocketException)
                        {
                            if (coreConfigManager is { AutoReconnectToWebSocket: false, CaptureTextFromClipboard: false })
                            {
                                StatsUtils.StopTimeStatStopWatch();
                            }

                            if (coreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested)
                            {
                                Utils.Logger.Warning(webSocketException, "WebSocket server is closed unexpectedly");
                                // Utils.Frontend.Alert(AlertLevel.Error, "WebSocket server is closed");
                            }
                            else if (webSocketClient.State is WebSocketState.Open)
                            {
                                await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure), CancellationToken.None).ConfigureAwait(false);
                            }

                            break;
                        }
                    }
                }

                catch (WebSocketException webSocketException)
                {
                    if (!coreConfigManager.AutoReconnectToWebSocket)
                    {
                        if (!coreConfigManager.CaptureTextFromClipboard)
                        {
                            StatsUtils.StopTimeStatStopWatch();
                        }

                        if (coreConfigManager.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested)
                        {
                            Utils.Logger.Warning(webSocketException, "Couldn't connect to the WebSocket server, probably because it is not running");
                            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't connect to the WebSocket server, probably because it is not running");
                        }
                    }
                    else
                    {
                        Utils.Logger.Verbose(webSocketException, "Couldn't connect to the WebSocket server, probably because it is not running");
                    }
                }

                finally
                {
                    s_webSocketTask = null;
                }
            }
            while (coreConfigManager is { AutoReconnectToWebSocket: true, CaptureTextFromWebSocket: true } && !cancellationToken.IsCancellationRequested);
        }, cancellationToken);
    }
}
