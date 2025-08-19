using System.Buffers;
using System.Net.WebSockets;
using JL.Core.Config;
using JL.Core.Statistics;
using JL.Core.Utilities;

namespace JL.Core.Network;
public sealed class WebSocketConnection : IDisposable
{
    private ClientWebSocket? _webSocketClient;
    private CancellationTokenSource? _webSocketCancellationTokenSource;
    private readonly Uri _webSocketUri;

    public bool Connected => _webSocketClient?.State is WebSocketState.Open;

    public WebSocketConnection(Uri webSocketUri)
    {
        _webSocketUri = webSocketUri;
    }

    public async Task Disconnect()
    {
        if (_webSocketClient?.State is WebSocketState.Open)
        {
            await _webSocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure), CancellationToken.None).ConfigureAwait(false);
        }

        if (_webSocketCancellationTokenSource is null)
        {
            return;
        }

        await _webSocketCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _webSocketCancellationTokenSource.Dispose();
        _webSocketClient?.Dispose();
        _webSocketCancellationTokenSource = null;
        _webSocketClient = null;
    }

    public void Connect()
    {
        if (_webSocketCancellationTokenSource is null)
        {
            _webSocketCancellationTokenSource = new CancellationTokenSource();
            ListenWebSocket(_webSocketCancellationTokenSource.Token);
        }
    }

    private void ListenWebSocket(CancellationToken cancellationToken)
    {
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        _ = Task.Run(async () =>
        {
            do
            {
                try
                {
                    using ClientWebSocket webSocketClient = new();
                    await webSocketClient.ConnectAsync(_webSocketUri, cancellationToken).ConfigureAwait(false);
                    _webSocketClient = webSocketClient;

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
                                if (coreConfigManager is { AutoReconnectToWebSocket: false, CaptureTextFromClipboard: false }
                                    && webSocketClient.State is not WebSocketState.Open
                                    && WebSocketUtils.AllConnectionsAreDisconnected())
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
                        if (!coreConfigManager.CaptureTextFromClipboard && WebSocketUtils.AllConnectionsAreDisconnected())
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
            }
            while (coreConfigManager is { AutoReconnectToWebSocket: true, CaptureTextFromWebSocket: true } && !cancellationToken.IsCancellationRequested);
        }, cancellationToken);
    }

    public void Dispose()
    {
        _webSocketCancellationTokenSource?.Dispose();
        _webSocketClient?.Dispose();
        _webSocketCancellationTokenSource = null;
        _webSocketClient = null;
    }
}
