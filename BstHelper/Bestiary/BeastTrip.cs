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

    private uint territory;
    private string label = "";

    public Beast? Target { get; private set; }

    public Landmark? Spot { get; private set; }

    public ETrip State { get; private set; } = ETrip.Idle;

    public string Status { get; private set; } = "Idle";

    public bool IsRunning => State == ETrip.Travelling;

    public static string? Blocker(Beast beast)
    {
        if (!beast.IsOverworld)
            return $"{beast.Mob} is a {beast.DutyRole} in {beast.Duty}.";

        return Blocker(beast.Territory, beast.Zone);
    }

    public static string? Blocker(Landmark spot) => Blocker(spot.Territory, spot.Zone);

    private static string? Blocker(uint territoryId, string zone)
    {
        var missing = Dependencies.Missing(Dependencies.Lifestream, Dependencies.Navmesh);
        if (missing.Count > 0)
            return $"{string.Join(" and ", missing)} {(missing.Count == 1 ? "is" : "are")} not loaded.";

        if (!Plugin.ClientState.IsLoggedIn)
            return "Not logged in.";

        GameData.RefreshAetheryteList();
        if (GameData.AetheryteListLoaded && !Traveller.CanReach(territoryId))
            return $"No attuned aetheryte in {zone}; attune there first.";

        return null;
    }

    public bool Start(Beast beast)
    {
        Stop("Starting a new trip");

        Target = beast;
        Spot = null;
        territory = beast.Territory;
        label = $"the {beast.Mob} spot";

        if (Blocker(beast) is { } blocker)
        {
            State = ETrip.Failed;
            Status = blocker;
            Plugin.ChatGui.PrintError($"[BstHelper] {blocker}");
            return false;
        }

        var destination = beast.World;
        if (destination == Vector3.Zero)
        {
            State = ETrip.Failed;
            Status = $"No coordinates for {beast.Name}.";
            return false;
        }

        travel.AllowZoneTeleport = true;
        travel.Go(beast.Territory, destination, StopRange, $"{beast.Mob} in {beast.Zone}", ProbeRadius);

        State = ETrip.Travelling;
        Status = $"Heading to {beast.Mob} in {beast.Where}";
        Plugin.ChatGui.Print($"[BstHelper] No. {beast.Number} {beast.Name}: {Status}.");
        return true;
    }

    public bool Start(Landmark spot)
    {
        Stop("Starting a new trip");

        Target = null;
        Spot = spot;
        territory = spot.Territory;
        label = spot.Name;

        if (Blocker(spot) is { } blocker)
        {
            State = ETrip.Failed;
            Status = blocker;
            Plugin.ChatGui.PrintError($"[BstHelper] {blocker}");
            return false;
        }

        travel.AllowZoneTeleport = true;
        travel.Go(spot.Territory, spot.World, StopRange, $"{spot.Name} in {spot.Zone}", ProbeRadius);

        State = ETrip.Travelling;
        Status = $"Heading to {spot.Name} in {spot.Zone}";
        Plugin.ChatGui.Print($"[BstHelper] {Status}.");
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
        if (State is ETrip.Arrived or ETrip.Failed)
        {
            if ((Target == null && Spot == null) || !Plugin.ClientState.IsLoggedIn ||
                (territory != 0 && Plugin.ClientState.TerritoryType != territory))
            {
                Stop("Left the zone");
            }

            return;
        }

        if (State != ETrip.Travelling)
            return;

        if (Target == null && Spot == null)
        {
            Stop("No target");
            return;
        }

        switch (travel.Update())
        {
            case ETravel.Arrived:
                travel.Stop();
                State = ETrip.Arrived;
                Status = Target is { } beast && Sighted(beast)
                    ? $"Arrived; {beast.Mob} is in sight"
                    : $"Arrived at {label}";
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
