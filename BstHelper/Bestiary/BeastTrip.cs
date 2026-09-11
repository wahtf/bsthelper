using System;
using System.Linq;
using System.Numerics;
using BstHelper.Travel;

namespace BstHelper.Bestiary;

public enum ETrip
{
    Idle,
    Travelling,
    Arrived,
    Failed,
}

public sealed class BeastTrip
{
    private const float StopRange = 12f;
    private const float ProbeRadius = 25f;

    private readonly Traveller travel = new();

    public Beast? Target { get; private set; }

    public ETrip State { get; private set; } = ETrip.Idle;

    public string Status { get; private set; } = "Idle";

    public bool IsRunning => State == ETrip.Travelling;

    public static string? Blocker(Beast beast)
    {
        if (!beast.IsOverworld)
            return $"{beast.Mob} is a {beast.DutyRole} in {beast.Duty}.";

        var missing = Dependencies.Missing(Dependencies.Lifestream, Dependencies.Navmesh);
        if (missing.Count > 0)
            return $"{string.Join(" and ", missing)} {(missing.Count == 1 ? "is" : "are")} not loaded.";

        if (!Plugin.ClientState.IsLoggedIn)
            return "Not logged in.";

        GameData.RefreshAetheryteList();
        if (GameData.AetheryteListLoaded && !Traveller.CanReach(beast.Territory))
            return $"No attuned aetheryte in {beast.Zone}; attune there first.";

        return null;
    }

    public bool Start(Beast beast)
    {
        Stop("Starting a new trip");

        if (Blocker(beast) is { } blocker)
        {
            Target = beast;
            State = ETrip.Failed;
            Status = blocker;
            Plugin.ChatGui.PrintError($"[BstHelper] {blocker}");
            return false;
        }

        var destination = beast.World;
        if (destination == Vector3.Zero)
        {
            Target = beast;
            State = ETrip.Failed;
            Status = $"No map data for {beast.Zone}.";
            return false;
        }

        Target = beast;
        travel.AllowZoneTeleport = true;
        travel.Go(beast.Territory, destination, StopRange, $"{beast.Mob} in {beast.Zone}", ProbeRadius);

        State = ETrip.Travelling;
        Status = $"Heading to {beast.Mob} in {beast.Where}";
        Plugin.ChatGui.Print($"[BstHelper] No. {beast.Number} {beast.Name}: {Status}.");
        return true;
    }

    public void Stop(string why)
    {
        if (State == ETrip.Travelling)
            Plugin.Log.Information($"[BstHelper] Trip stopped: {why}");

        travel.Stop();
        State = ETrip.Idle;
        Status = "Idle";
    }

    public void Update()
    {
        if (State != ETrip.Travelling)
            return;

        if (Target is not { } beast)
        {
            Stop("No target");
            return;
        }

        switch (travel.Update())
        {
            case ETravel.Arrived:
                travel.Stop();
                State = ETrip.Arrived;
                Status = Sighted(beast) ? $"Arrived; {beast.Mob} is in sight" : $"Arrived at the {beast.Mob} spot";
                Plugin.ChatGui.Print($"[BstHelper] {Status}.");
                break;

            case ETravel.Failed:
                travel.Stop();
                State = ETrip.Failed;
                Status = $"Could not get there: {travel.Status}";
                Plugin.ChatGui.PrintError($"[BstHelper] {Status}");
                break;

            default:
                Status = travel.Status;
                break;
        }
    }

    private static bool Sighted(Beast beast)
    {
        try
        {
            return Plugin.ObjectTable.Any(x =>
                x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
                string.Equals(x.Name.TextValue, beast.Mob, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
