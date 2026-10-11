using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JL.Core.Network.WebSocket;

public static class WebSocketUtils
{
    private static readonly Dictionary<Uri, WebSocketConnection> s_webSocketConnectionsDict = [];
    private static readonly Lock s_connectionsLock = new();

    internal static WebSocketConnection? TsukikageWebSocketConnection
    {
        get
        {
            lock (s_connectionsLock)
            {
                return field;
            }
        }
        set
        {
            lock (s_connectionsLock)
            {
                field = value;
            }
        }
    }

    public static Task DisconnectFromAllWebSocketConnections()
    {
        lock (s_connectionsLock)
        {
            if (s_webSocketConnectionsDict.Count is 0)
            {
                return Task.CompletedTask;
            }

            Task[] disconnectTasks = new Task[s_webSocketConnectionsDict.Count];
            int index = 0;
            foreach (WebSocketConnection connection in s_webSocketConnectionsDict.Values)
            {
                disconnectTasks[index] = connection.Disconnect();
                ++index;
            }

            return Task.WhenAll(disconnectTasks);
        }
    }

    public static Task DisconnectFromTsukikageWebSocketConnection()
    {
        lock (s_connectionsLock)
        {
            return TsukikageWebSocketConnection?.Disconnect() ?? Task.CompletedTask;
        }
    }

    internal static Task DisconnectFromWebSocket(Uri webSocketUri)
    {
        lock (s_connectionsLock)
        {
            if (s_webSocketConnectionsDict.TryGetValue(webSocketUri, out WebSocketConnection? existingConnection))
            {
                _ = s_webSocketConnectionsDict.Remove(webSocketUri);
                return existingConnection.Disconnect();
            }

            return Task.CompletedTask;
        }
    }

    public static Task ResetWebSocketConnections()
    {
        lock (s_connectionsLock)
        {
            WebSocketConnection? tsukikageConnection = TsukikageWebSocketConnection;
            int connectionCount = s_webSocketConnectionsDict.Count + (tsukikageConnection is not null ? 1 : 0);
            if (connectionCount is 0)
            {
                return Task.CompletedTask;
            }

            Task[] disconnectTasks = new Task[connectionCount];
            int index = 0;
            foreach (WebSocketConnection connection in s_webSocketConnectionsDict.Values)
            {
                disconnectTasks[index] = connection.Disconnect();
                ++index;
            }
            if (tsukikageConnection is not null)
            {
                disconnectTasks[index] = tsukikageConnection.Disconnect();
            }

            // Detach these connections before waiting so a later connection is not removed by this reset.
            s_webSocketConnectionsDict.Clear();
            TsukikageWebSocketConnection = null;
            return Task.WhenAll(disconnectTasks);
        }
    }

    public static void ConnectToAllWebSockets()
    {
        lock (s_connectionsLock)
        {
            foreach (WebSocketConnection connection in s_webSocketConnectionsDict.Values)
            {
                connection.Connect(false);
            }
        }
    }

    public static void ConnectToTsukikageWebSocket()
    {
        lock (s_connectionsLock)
        {
            TsukikageWebSocketConnection?.Connect(true);
        }
    }

    internal static void ConnectToWebSocket(Uri webSocketUri)
    {
        lock (s_connectionsLock)
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
    }

    internal static bool AllConnectionsAreDisconnected()
    {
        lock (s_connectionsLock)
        {
            if (TsukikageWebSocketConnection?.Connected ?? false)
            {
                return false;
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
}
