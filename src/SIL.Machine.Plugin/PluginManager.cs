using System;
using System.Collections.Generic;
using System.Linq;
using McMaster.NETCore.Plugins;

namespace SIL.Machine.Plugin;

public class PluginManager
{
    private readonly List<PluginLoader> _loaders;

    public PluginManager(IEnumerable<string> pluginPaths)
    {
        _loaders = new List<PluginLoader>();
        foreach (string pluginPath in pluginPaths)
        {
            var loader = PluginLoader.CreateFromAssemblyFile(pluginPath, config => config.PreferSharedTypes = true);
            _loaders.Add(loader);
        }
    }

    public IEnumerable<T> Create<T>()
    {
        foreach (PluginLoader loader in _loaders)
        {
            foreach (
                Type type in loader
                    .LoadDefaultAssembly()
                    .GetTypes()
                    .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract)
            )
            {
                yield return (T)Activator.CreateInstance(type);
            }
        }
    }
}
