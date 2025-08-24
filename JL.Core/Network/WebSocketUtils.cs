namespace JL.Core.Network;

public static class WebSocketUtils
{
    private static Dictionary<Uri, WebSocketConnection> WebSocketConnectionsDict { get; } = [];

    public static async Task DisconnectFromAllWebSocketConnections()
    {
        foreach (WebSocketConnection connection in WebSocketConnectionsDict.Values)
        {
            await connection.Disconnect().ConfigureAwait(false);
        }
    }

    internal static async Task DisconnectFromWebSocket(Uri webSocketUri)
    {
        if (WebSocketConnectionsDict.TryGetValue(webSocketUri, out WebSocketConnection? existingConnection))
        {
            await existingConnection.Disconnect().ConfigureAwait(false);
            _ = WebSocketConnectionsDict.Remove(webSocketUri);
        }
    }

    public static void ConnectToAllWebSockets()
    {
        foreach (WebSocketConnection connection in WebSocketConnectionsDict.Values)
        {
            connection.Connect();
        }
    }

    internal static void ConnectToWebSocket(Uri webSocketUri)
    {
        if (WebSocketConnectionsDict.TryGetValue(webSocketUri, out WebSocketConnection? existingConnection))
        {
            existingConnection.Connect();
        }
        else
        {
            WebSocketConnection webSocketConnection = new(webSocketUri);
            WebSocketConnectionsDict[webSocketUri] = webSocketConnection;
            webSocketConnection.Connect();
        }
    }

    internal static bool AllConnectionsAreDisconnected()
    {
        foreach (WebSocketConnection connection in WebSocketConnectionsDict.Values)
        {
            if (connection.Connected)
            {
                return false;
            }
        }

        return true;
    }
}
