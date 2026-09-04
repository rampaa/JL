using System.Buffers;
using System.Net.WebSockets;
using JL.Core.Config;
using JL.Core.Frontend;
using JL.Core.Statistics;
using JL.Core.Utilities;

namespace JL.Core.Network.WebSocket;

internal sealed class WebSocketConnection : IDisposable
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

    public void Connect(bool tsukikage)
    {
        if (_webSocketCancellationTokenSource is null)
        {
            _webSocketCancellationTokenSource = new CancellationTokenSource();
            ListenWebSocket(tsukikage, _webSocketCancellationTokenSource.Token).SafeFireAndForget("Unexpected error while listening the WebSocket");
        }
    }

    private Task ListenWebSocket(bool tsukikage, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            CoreConfigManager coreConfigManager = CoreConfigManager.Instance;

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
                        while (!cancellationToken.IsCancellationRequested
                            && webSocketClient.State is WebSocketState.Open
                            && (tsukikage ? coreConfigManager.CaptureTextFromTsukikageWebsocket : coreConfigManager.CaptureTextFromWebSocket))
                        {
                            try
                            {
                                ValueWebSocketReceiveResult result = await webSocketClient.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                                if (cancellationToken.IsCancellationRequested
                                    || (tsukikage ? !coreConfigManager.CaptureTextFromTsukikageWebsocket : !coreConfigManager.CaptureTextFromWebSocket))
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

                                    string text = TextUtils.s_utf8NoBom.GetString(buffer.Span[..totalBytesReceived]);
                                    FrontendManager.Frontend.CopyFromWebSocket(text, tsukikage).SafeFireAndForget("Frontend copy from WebSocket failed");
                                }
                                else if (result.MessageType is WebSocketMessageType.Close)
                                {
                                    LoggerManager.Logger.Information("WebSocket server at {WebSocketUri} is closed", _webSocketUri);
                                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"WebSocket server at {_webSocketUri} is closed");
                                    break;
                                }
                            }
                            catch (WebSocketException webSocketException)
                            {
                                if (coreConfigManager is { AutoReconnectToTsukikageWebSocket: false, AutoReconnectToWebSocket: false, CaptureTextFromClipboard: false }
                                    && webSocketClient.State is not WebSocketState.Open
                                    && WebSocketUtils.AllConnectionsAreDisconnected())
                                {
                                    StatsUtils.StopTimeStatStopWatch();
                                }

                                if (!cancellationToken.IsCancellationRequested
                                    && (tsukikage ? coreConfigManager.CaptureTextFromTsukikageWebsocket : coreConfigManager.CaptureTextFromWebSocket))
                                {
                                    LoggerManager.Logger.Warning(webSocketException, "WebSocket server at {WebSocketUri} is closed unexpectedly", _webSocketUri);
                                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"WebSocket server at {_webSocketUri} is closed unexpectedly");
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
                    if (tsukikage ? !coreConfigManager.AutoReconnectToTsukikageWebSocket : !coreConfigManager.AutoReconnectToWebSocket)
                    {
                        if (!coreConfigManager.CaptureTextFromClipboard && WebSocketUtils.AllConnectionsAreDisconnected())
                        {
                            StatsUtils.StopTimeStatStopWatch();
                        }

                        if (!cancellationToken.IsCancellationRequested
                            && (tsukikage ? coreConfigManager.CaptureTextFromTsukikageWebsocket : coreConfigManager.CaptureTextFromWebSocket))
                        {
                            LoggerManager.Logger.Warning(webSocketException, "Couldn't connect to the WebSocket server at {WebSocketUri}, probably because it is not running", _webSocketUri);
                            FrontendManager.Frontend.Notify(NotificationLevel.Warning, $"Couldn't connect to the WebSocket server at {_webSocketUri}, probably because it is not running");
                        }
                    }
                    else
                    {
                        LoggerManager.Logger.Verbose(webSocketException, "Couldn't connect to the WebSocket server at {WebSocketUri}, probably because it is not running", _webSocketUri);
                    }
                }

                catch (OperationCanceledException)
                {
                    LoggerManager.Logger.Debug("Connection was cancelled for the websocket server at {WebSocketUri}.", _webSocketUri);
                    return;
                }

                catch (Exception ex)
                {
                    LoggerManager.Logger.Error(ex, "An unexpected error occured while listening the websocket server at {WebSocketUri}", _webSocketUri);
                    FrontendManager.Frontend.Notify(NotificationLevel.Error, $"An unexpected error occured while listening the websocket server at {_webSocketUri}. Check the logs for more details.");
                    return;
                }
            }
            while (!cancellationToken.IsCancellationRequested
                && (tsukikage ? coreConfigManager is { AutoReconnectToTsukikageWebSocket: true, CaptureTextFromTsukikageWebsocket: true } : coreConfigManager is { AutoReconnectToWebSocket: true, CaptureTextFromWebSocket: true }));
        }, CancellationToken.None);
    }

    public void Dispose()
    {
        _webSocketCancellationTokenSource?.Dispose();
        _webSocketClient?.Dispose();
        _webSocketCancellationTokenSource = null;
        _webSocketClient = null;
    }
}
