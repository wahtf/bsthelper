using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Ipc;

namespace BstHelper.Travel.Ipc;

public class NavmeshIpc
{
    private readonly ICallGateSubscriber<bool> navIsReady =
        Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");

    private readonly ICallGateSubscriber<float> buildProgress =
        Plugin.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Nav.BuildProgress");

    private readonly ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>> pathfind =
        Plugin.PluginInterface.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>("vnavmesh.Nav.Pathfind");

    private readonly ICallGateSubscriber<bool> moveInProgress =
        Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.SimpleMove.PathfindInProgress");

    private readonly ICallGateSubscriber<Vector3, bool, bool> moveTo =
        Plugin.PluginInterface.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");

    private readonly ICallGateSubscriber<Vector3, bool, float, bool> moveCloseTo =
        Plugin.PluginInterface.GetIpcSubscriber<Vector3, bool, float, bool>("vnavmesh.SimpleMove.PathfindAndMoveCloseTo");

    private readonly ICallGateSubscriber<bool> pathIsRunning =
        Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");

    private readonly ICallGateSubscriber<object> pathStop =
        Plugin.PluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");

    private readonly ICallGateSubscriber<List<Vector3>, bool, object> pathMoveTo =
        Plugin.PluginInterface.GetIpcSubscriber<List<Vector3>, bool, object>("vnavmesh.Path.MoveTo");

    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPointReachable =
        Plugin.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPointReachable");

    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPoint =
        Plugin.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPoint");

    public bool IsNavmeshReady() => Gate.Call(() => navIsReady.InvokeFunc(), false, "Nav.IsReady");

    public string WaitText() => $"Waiting for the navmesh ({BuildProgressPercent()}%)";

    public bool IsPathfindingInProgress()
        => Gate.Call(() => moveInProgress.InvokeFunc(), false, "SimpleMove.PathfindInProgress");

    public bool IsPathRunning() => Gate.Call(() => pathIsRunning.InvokeFunc(), false, "Path.IsRunning");

    public int BuildProgressPercent()
    {
        var progress = Gate.Call(() => buildProgress.InvokeFunc(), -1f, "Nav.BuildProgress");
        return progress < 0 ? 100 : (int)(progress * 100);
    }

    public bool PathfindAndMoveTo(Vector3 destination, bool fly = false)
    {
        if (!IsNavmeshReady())
        {
            Plugin.Log.Warning("Navmesh is not ready for pathfinding");
            return false;
        }

        return Gate.Call(() => moveTo.InvokeFunc(destination, fly), false, "SimpleMove.PathfindAndMoveTo");
    }

    public bool PathfindAndMoveCloseTo(Vector3 destination, bool fly, float range)
    {
        if (!IsNavmeshReady())
        {
            Plugin.Log.Warning("Navmesh is not ready for pathfinding");
            return false;
        }

        return Gate.Call(() => moveCloseTo.InvokeFunc(destination, fly, range), false,
            "SimpleMove.PathfindAndMoveCloseTo");
    }

    public Vector3? NearestReachablePoint(Vector3 position, float halfExtentXZ, float halfExtentY)
        => Gate.Call(() => nearestPointReachable.InvokeFunc(position, halfExtentXZ, halfExtentY), null,
            "Query.Mesh.NearestPointReachable");

    public Vector3? NearestPoint(Vector3 position, float halfExtentXZ, float halfExtentY)
        => Gate.Call(() => nearestPoint.InvokeFunc(position, halfExtentXZ, halfExtentY), null,
            "Query.Mesh.NearestPoint");

    public Task<List<Vector3>>? Pathfind(Vector3 from, Vector3 to, bool fly)
        => IsNavmeshReady()
            ? Gate.Call<Task<List<Vector3>>?>(() => pathfind.InvokeFunc(from, to, fly), null, "Nav.Pathfind")
            : null;

    public void MoveTo(Vector3 destination, bool fly) => MoveTo(new List<Vector3> { destination }, fly);

    public void MoveTo(List<Vector3> points, bool fly)
        => Gate.Call(() => pathMoveTo.InvokeAction(points, fly), "Path.MoveTo");

    public void StopPathfinding() => Gate.Call(() => pathStop.InvokeAction(), "Path.Stop");
}
