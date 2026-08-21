namespace Aurora.Core.Plugins;

/// <summary>
/// Interface for all loadable server plugins.
/// </summary>
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    
    void OnEnable();
    void OnDisable();
}
