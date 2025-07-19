using System.Buffers;
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
                    byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
                    try
                    {
                        Memory<byte> buffer = rentedBuffer;
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
                                    int totalBytesReceived = result.Count;
                                    while (!result.EndOfMessage)
                                    {
                                        if (totalBytesReceived == buffer.Length)
                                        {
                                            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                                            buffer.CopyTo(newBuffer);
                                            ArrayPool<byte>.Shared.Return(rentedBuffer);
                                            rentedBuffer = newBuffer;
                                            buffer = rentedBuffer;
                                        }

                                        result = await webSocketClient.ReceiveAsync(buffer[totalBytesReceived..], CancellationToken.None).ConfigureAwait(false);
                                        totalBytesReceived += result.Count;
                                    }

                                    string text = NetworkUtils.s_utf8NoBom.GetString(buffer.Span[..totalBytesReceived]);
                                    _ = Utils.Frontend.CopyFromWebSocket(text);
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
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rentedBuffer);
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
