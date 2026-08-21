using System;
using System.Threading;

namespace Aurora.Threading;

/// <summary>
/// Represents an abstract unit of work to be executed by a Worker.
/// </summary>
public interface IWorkItem
{
    void Execute();
}

/// <summary>
/// A simple work item that executes an Action delegate.
/// </summary>
public sealed class ActionWorkItem : IWorkItem
{
    private readonly Action _action;

    public ActionWorkItem(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _action = action;
    }

    public void Execute()
    {
        _action();
    }
}
