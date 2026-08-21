using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Aurora.Server;

/// <summary>
/// Manages connected players and sessions.
/// </summary>
public sealed class PlayerManager
{
    // Uses Guid for entity/player ID until Phase 7 Player implementation
    private readonly ConcurrentDictionary<Guid, object> _players = new();

    public int PlayerCount => _players.Count;

    public void AddPlayer(Guid playerId, object playerSession)
    {
        ArgumentNullException.ThrowIfNull(playerSession);
        _players.TryAdd(playerId, playerSession);
    }

    public void RemovePlayer(Guid playerId)
    {
        _players.TryRemove(playerId, out _);
    }
}
