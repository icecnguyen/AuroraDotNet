namespace Aurora.Core.Events;

/// <summary>
/// Marker interface for all events in the system.
/// </summary>
#pragma warning disable CA1040 // Marker interface for type constraint
public interface IEvent
{
}
#pragma warning restore CA1040

/// <summary>
/// Represents an event that can be cancelled by a plugin.
/// </summary>
public interface ICancellableEvent : IEvent
{
    bool IsCancelled { get; set; }
}
