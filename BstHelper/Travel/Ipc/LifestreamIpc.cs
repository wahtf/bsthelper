using Dalamud.Plugin.Ipc;

namespace BstHelper.Travel.Ipc;

public class LifestreamIpc
{
    private readonly ICallGateSubscriber<uint, byte, bool> teleport =
        Plugin.PluginInterface.GetIpcSubscriber<uint, byte, bool>("Lifestream.Teleport");

    private readonly ICallGateSubscriber<uint, bool> aethernetTeleportById =
        Plugin.PluginInterface.GetIpcSubscriber<uint, bool>("Lifestream.AethernetTeleportById");

    private readonly ICallGateSubscriber<uint, bool> housingAethernetTeleportById =
        Plugin.PluginInterface.GetIpcSubscriber<uint, bool>("Lifestream.HousingAethernetTeleportById");

    private readonly ICallGateSubscriber<bool> isBusy =
        Plugin.PluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");

    public bool IsBusy => Gate.Call(() => isBusy.InvokeFunc(), false, "Lifestream.IsBusy");

    public bool Teleport(uint aetheryteId, byte subIndex = 0)
    {
        Plugin.Log.Information($"Teleporting to aetheryte {aetheryteId} (sub {subIndex})");
        return Gate.Call(() => teleport.InvokeFunc(aetheryteId, subIndex), false, "Lifestream.Teleport");
    }

    public bool AethernetTeleport(uint aetheryteId)
    {
        Plugin.Log.Information($"Aethernet to shard {aetheryteId}");
        return Gate.Call(() => aethernetTeleportById.InvokeFunc(aetheryteId), false,
            "Lifestream.AethernetTeleportById");
    }

    public bool HousingAethernetTeleport(uint shardRowId)
    {
        Plugin.Log.Information($"Housing aethernet to shard {shardRowId}");
        return Gate.Call(() => housingAethernetTeleportById.InvokeFunc(shardRowId), false,
            "Lifestream.HousingAethernetTeleportById");
    }
}
