using System.Diagnostics;
using System.Runtime.InteropServices;

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

    public static Task DisconnectFromTsukikageWebSocketConnection()
    {
        return TsukikageWebSocketConnection is not null
            ? TsukikageWebSocketConnection.Disconnect()
            : Task.CompletedTask;
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
        ref WebSocketConnection? connection = ref CollectionsMarshal.GetValueRefOrAddDefault(s_webSocketConnectionsDict, webSocketUri, out bool exists);
        if (!exists)
        {
            Debug.Assert(connection is null);
#pragma warning disable CA2000 // Dispose objects before losing scope
            connection = new WebSocketConnection(webSocketUri);
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        Debug.Assert(connection is not null);
        connection.Connect(false);
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
