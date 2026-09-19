using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Aurora.World.Entities;

namespace Aurora.Server;

/// <summary>
/// Manages connected player entities and sessions.
/// </summary>
public sealed class PlayerManager
{
    private readonly ConcurrentDictionary<Guid, Player> _players = new();

    public int PlayerCount => _players.Count;

    public IEnumerable<Player> AllPlayers => _players.Values;

    public void AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        _players.TryAdd(player.Uuid, player);
    }

    public void RemovePlayer(Guid playerId)
    {
        _players.TryRemove(playerId, out _);
    }

    public Player? GetPlayer(Guid playerId)
    {
        _players.TryGetValue(playerId, out var player);
        return player;
    }

    public Player? GetPlayer(string username)
    {
        if (string.IsNullOrEmpty(username)) return null;
        foreach (var player in _players.Values)
        {
            if (string.Equals(player.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }
        }
        return null;
    }
}
