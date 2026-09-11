using System;
using Dalamud.Configuration;

namespace BstHelper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public int Page { get; set; }

    public bool OnlyShowCatchable { get; set; }

    public bool ShowTrail { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
