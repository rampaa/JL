using System.Net.WebSockets;
using System.Text;
using JL.Core.Utilities;

namespace JL.Core.Network;

public static class WebSocketUtils
{
    private static Task? s_webSocketTask = null;
    private static CancellationTokenSource? s_webSocketCancellationTokenSource = null;

    public static void HandleWebSocket()
    {
        if (!CoreConfig.CaptureTextFromWebSocket)
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
        s_webSocketTask = Task.Factory.StartNew(async () =>
        {
            try
            {
                using ClientWebSocket webSocketClient = new();
                await webSocketClient.ConnectAsync(CoreConfig.WebSocketUri, CancellationToken.None).ConfigureAwait(false);
                byte[] buffer = new byte[1024];

                while (CoreConfig.CaptureTextFromWebSocket && !cancellationToken.IsCancellationRequested && webSocketClient.State is WebSocketState.Open)
                {
                    try
                    {
                        WebSocketReceiveResult result = await webSocketClient.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);

                        if (!CoreConfig.CaptureTextFromWebSocket || cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (result.MessageType is WebSocketMessageType.Text)
                        {
                            using MemoryStream memoryStream = new();
                            await memoryStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken).ConfigureAwait(false);

                            while (!result.EndOfMessage)
                            {
                                result = await webSocketClient.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                                await memoryStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken).ConfigureAwait(false);
                            }

                            _ = memoryStream.Seek(0, SeekOrigin.Begin);

                            string text = Encoding.UTF8.GetString(memoryStream.ToArray());
                            _ = Task.Run(() => Utils.Frontend.CopyFromWebSocket(text), cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (WebSocketException webSocketException)
                    {
                        Utils.Logger.Warning(webSocketException, "WebSocket server is closed unexpectedly");
                        Utils.Frontend.Alert(AlertLevel.Error, "WebSocket server is closed");
                        break;
                    }
                }
            }

            catch (WebSocketException webSocketException)
            {
                Utils.Logger.Warning(webSocketException, "Couldn't connect to the WebSocket server, probably because it is not running");
                Utils.Frontend.Alert(AlertLevel.Error, "Couldn't connect to the WebSocket server, probably because it is not running");
            }
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
