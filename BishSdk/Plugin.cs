using BishRuntime;

namespace BishSdk;

public class PluginExports
{
    // ReSharper disable once CollectionNeverUpdated.Global
    public readonly Dictionary<string, BishObject> Exports = [];
}

public interface IPlugin
{
    void Initialize(PluginExports exports);
}