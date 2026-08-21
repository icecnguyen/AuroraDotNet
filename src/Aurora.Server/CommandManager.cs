using System;
using System.Collections.Concurrent;

namespace Aurora.Server;

/// <summary>
/// Manages chat command registration and execution.
/// </summary>
public sealed class CommandManager
{
    private readonly ConcurrentDictionary<string, Action<string[]>> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterCommand(string name, Action<string[]> handler)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(handler);
        _commands.TryAdd(name, handler);
    }

    public bool ExecuteCommand(string commandLine)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        
        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var name = parts[0];
        if (_commands.TryGetValue(name, out var handler))
        {
            var args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            handler(args);
            return true;
        }

        return false;
    }
}
