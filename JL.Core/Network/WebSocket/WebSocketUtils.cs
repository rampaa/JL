namespace JL.Core.Network.WebSocket;

public static class WebSocketUtils
{
    private static readonly Dictionary<Uri, WebSocketConnection> s_webSocketConnectionsDict = [];

    internal static WebSocketConnection? TsukikageWebSocketConnection { get; set; }

    public static async Task DisconnectFromAllWebSocketConnections()
    {
        foreach (WebSocketConnection connection in s_webSocketConnectionsDict.Values)
        {
            await connection.Disconnect().ConfigureAwait(false);
        }
    }

    public static async Task DisconnectFromTsukikageWebSocketConnection()
    {
        if (TsukikageWebSocketConnection is not null)
        {
            await TsukikageWebSocketConnection.Disconnect().ConfigureAwait(false);
        }
    }

    internal static async Task DisconnectFromWebSocket(Uri webSocketUri)
    {
        if (s_webSocketConnectionsDict.TryGetValue(webSocketUri, out WebSocketConnection? existingConnection))
        {
            await existingConnection.Disconnect().ConfigureAwait(false);
            _ = s_webSocketConnectionsDict.Remove(webSocketUri);
        }
    }

    public static void ConnectToAllWebSockets()
    {
        foreach (WebSocketConnection connection in s_webSocketConnectionsDict.Values)
        {
            connection.Connect(false);
        }
    }

    public static void ConnectToTsukikageWebSocket()
    {
        TsukikageWebSocketConnection?.Connect(true);
    }

    internal static void ConnectToWebSocket(Uri webSocketUri)
    {
        if (s_webSocketConnectionsDict.TryGetValue(webSocketUri, out WebSocketConnection? existingConnection))
        {
            existingConnection.Connect(false);
        }
        else
        {
            WebSocketConnection webSocketConnection = new(webSocketUri);
            s_webSocketConnectionsDict[webSocketUri] = webSocketConnection;
            webSocketConnection.Connect(false);
        }
    }

    internal static bool AllConnectionsAreDisconnected()
    {
        if (TsukikageWebSocketConnection?.Connected ?? false)
        {
            return true;
        }

        foreach (WebSocketConnection connection in s_webSocketConnectionsDict.Values)
        {
            if (connection.Connected)
            {
                return false;
            }
        }

        return true;
    }
}
