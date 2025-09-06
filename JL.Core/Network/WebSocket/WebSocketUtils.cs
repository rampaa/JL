namespace JL.Core.Network.WebSocket;

public static class WebSocketUtils
{
    private static readonly Dictionary<Uri, WebSocketConnection> s_webSocketConnectionsDict = [];

    public static async Task DisconnectFromAllWebSocketConnections()
    {
        foreach (WebSocketConnection connection in s_webSocketConnectionsDict.Values)
        {
            await connection.Disconnect().ConfigureAwait(false);
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
            connection.Connect();
        }
    }

    internal static void ConnectToWebSocket(Uri webSocketUri)
    {
        if (s_webSocketConnectionsDict.TryGetValue(webSocketUri, out WebSocketConnection? existingConnection))
        {
            existingConnection.Connect();
        }
        else
        {
            WebSocketConnection webSocketConnection = new(webSocketUri);
            s_webSocketConnectionsDict[webSocketUri] = webSocketConnection;
            webSocketConnection.Connect();
        }
    }

    internal static bool AllConnectionsAreDisconnected()
    {
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
