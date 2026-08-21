using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Aurora.Network;

/// <summary>
/// Manages active TCP connections to the server.
/// </summary>
public sealed class ConnectionManager : IDisposable
{
    private readonly ConcurrentDictionary<Guid, MinecraftConnection> _connections = new();

    public void AddConnection(MinecraftConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connections.TryAdd(connection.Id, connection);
        
        // When connection is closed, remove it
        connection.OnDisconnected += OnConnectionDisconnected;
    }

    private void OnConnectionDisconnected(object? sender, ConnectionEventArgs e)
    {
        var connection = e.Connection;
        _connections.TryRemove(connection.Id, out _);
        connection.OnDisconnected -= OnConnectionDisconnected;
    }

    public void DisconnectAll()
    {
        foreach (var connection in _connections.Values)
        {
            connection.Disconnect();
        }
        _connections.Clear();
    }

    public void Dispose()
    {
        DisconnectAll();
    }
}
