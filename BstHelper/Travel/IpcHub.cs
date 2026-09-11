using BstHelper.Travel.Ipc;

namespace BstHelper.Travel;

public static class IpcHub
{
    public static NavmeshIpc Navmesh { get; private set; } = null!;
    public static LifestreamIpc Lifestream { get; private set; } = null!;

    internal static void Init()
    {
        Navmesh = new NavmeshIpc();
        Lifestream = new LifestreamIpc();
    }
}
