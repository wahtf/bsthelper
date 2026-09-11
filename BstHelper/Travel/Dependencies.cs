using System.Collections.Generic;
using System.Linq;

namespace BstHelper.Travel;

public static class Dependencies
{
    public const string Lifestream = "Lifestream";
    public const string Navmesh = "vnavmesh";

    public static bool IsLoaded(string internalName)
        => Plugin.PluginInterface.InstalledPlugins.Any(x => x.InternalName == internalName && x.IsLoaded);

    public static List<string> Missing(params string[] internalNames)
        => internalNames.Where(x => !IsLoaded(x)).ToList();
}
