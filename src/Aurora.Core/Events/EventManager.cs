using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Aurora.Core.Events;

/// <summary>
/// Simple event pub/sub manager for plugins.
/// </summary>
public sealed class EventManager
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Register<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        
        var type = typeof(TEvent);
        _handlers.AddOrUpdate(
            type,
            _ => new List<Delegate> { handler },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(handler);
                }
                return list;
            });
    }

    public void Dispatch<TEvent>(TEvent ev) where TEvent : IEvent
    {
        if (ev == null) return;
        
        if (_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            Delegate[] snapshot;
            lock (list)
            {
                snapshot = list.ToArray();
            }

            foreach (var handler in snapshot)
            {
                if (handler is Action<TEvent> action)
                {
                    action(ev);
                }
                
                // Fast-fail if cancelled
                if (ev is ICancellableEvent { IsCancelled: true })
                {
                    break;
                }
            }
        }
    }
}
