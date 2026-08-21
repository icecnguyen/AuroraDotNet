using System;
using System.Collections.Concurrent;

namespace Aurora.Core.Pooling;

/// <summary>
/// A high-performance object pool for reducing GC allocations (Phase 14).
/// </summary>
public sealed class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _items = new();
    private readonly Action<T>? _resetAction;

    public ObjectPool(Action<T>? resetAction = null)
    {
        _resetAction = resetAction;
    }

    public T Rent()
    {
        if (_items.TryTake(out var item))
        {
            return item;
        }
        return new T();
    }

    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        
        _resetAction?.Invoke(item);
        _items.Add(item);
    }
}
