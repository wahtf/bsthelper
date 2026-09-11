using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Conditions;
using BstHelper.Travel.Ipc;

namespace BstHelper.Travel;

public enum ETravel
{
    Idle,
    Teleporting,
    Walking,
    Arrived,
    Failed,
}

public class Traveller
{
    private static readonly TimeSpan ArrivalSettle = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ListWaitLimit = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TeleportLanding = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan HopLanding = TimeSpan.FromSeconds(25);
    private const float LandingReach = 40f;
    private static readonly TimeSpan DescendLimit = TimeSpan.FromSeconds(12);

    private readonly StuckDetector stuck = new();
    private readonly Guid instance = Guid.NewGuid();

    public const float DefaultProbeRadius = 30f;
    private const float PreciseRange = 0.75f;
    private const float SettleReach = 12f;
    private const int MaxSettleAttempts = 3;
    internal const float ProbeHeight = 200f;

    private const float MountDistance = 40f;
    private const float FlightDistance = 100f;
    private const float ZoneTeleportDistance = 400f;
    private const float ZoneTeleportGain = 200f;
    private const float AethernetGain = 40f;
    private const float HopReach = 8f;
    private const float HousingHopReach = 3.5f;
    private const float ShardMatch = 10f;
    private const float WarpJump = 50f;
    private const float PathEndSlack = 3f;
    private static readonly TimeSpan ProbeLimit = TimeSpan.FromSeconds(8);
    private const float CrossingRange = 2f;
    private const float ViaRange = 1.5f;
    private const float WaypointRange = 6f;

    private const float MinimumSprintDistance = 40f;
    public const float CruiseHeight = 25f;
    private const float LedgeInner = 2f;
    private const float LedgeRing = 5f;
    private const float LedgeLevel = 1.5f;
    private static readonly TimeSpan TakeoffGrace = TimeSpan.FromSeconds(5);
    private const float MovedTolerance = 2f;

    private readonly record struct Passage(uint To, uint From, Vector3? Via, Vector3 Cross);

    private static readonly Passage[] Passages =
    {
        new(399, 478, new Vector3(70.25f, 205.56f, 26.72f), new Vector3(148.53f, 207f, 117.84f)),
        new(133, 132, null, new Vector3(11.46f, 1.27f, -15.39f)),
        new(128, 129, new Vector3(0.31f, 20.00f, 16.51f), new Vector3(-7.60f, 21.00f, 26.70f)),
    };

    private uint territory;
    private Vector3 destination;
    private Vector3[] path = System.Array.Empty<Vector3>();
    private int pathAt;
    private Vector3 pathEnd;
    private float pathStop;
    private Vector3? reachable;
    private float stopRange = 3f;
    private float probeRadius = DefaultProbeRadius;
    private bool precise;
    private bool settled;
    private int settleAttempts;
    private string name = "there";
    private bool zoneTeleportConsidered;
    private bool zoneTeleportPending;
    private DateTime zoneTeleportRequestedAt = DateTime.MinValue;
    private GameData.AetheryteInfo? zoneTeleportTo;
    private bool aethernetConsidered;
    private bool aethernetPending;
    private DateTime aethernetRequestedAt = DateTime.MinValue;
    private GameData.AetheryteInfo? hopFrom;
    private GameData.AetheryteInfo? hopTo;
    private bool hopWalking;
    private bool hopExact;
    private Task<List<Vector3>>? reachProbe;
    private DateTime probeStartedAt = DateTime.MinValue;
    private List<GameData.AetheryteInfo>? hopCandidates;
    private int hopCandidate;
    private float? walkLength;
    private bool descending;
    private Vector3? landing;
    private int directRetries;
    private DateTime descendAt = DateTime.MinValue;
    private int stuckRecoveries;
    private bool viaReached;
    private DateTime flightAskedAt = DateTime.MinValue;
    private bool highRouteFailed;
    private bool aimedHigh;
    private Vector3 dispatchedFrom;
    private string status = "Idle";
    private string loggedStatus = string.Empty;

    private DateTime settleUntil = DateTime.MinValue;
    private DateTime teleportRequestedAt = DateTime.MinValue;
    private DateTime listWaitStarted = DateTime.MinValue;
    private DateTime mountRetryAt = DateTime.MinValue;
    private DateTime takeoffRetryAt = DateTime.MinValue;
    private DateTime dismountRetryAt = DateTime.MinValue;
    private int teleportAttempts;
    private int mountAttempts;
    private int takeoffAttempts;
    private int dismountAttempts;
    private bool wantMount;
    private bool wantFlight;
    private bool mountAbandoned;
    private bool flightAbandoned;
    private bool dispatched;
    private uint lastTerritory;

    public bool AllowFlight { get; set; } = true;

    public bool AllowZoneTeleport { get; set; }

    public bool AllowAethernet { get; set; } = true;

    public TimeSpan StuckThreshold { get; set; } = TimeSpan.FromSeconds(20);

    public int MaxStuckRecoveries { get; set; } = 1;

    public bool RequireFlight { get; set; }

    public bool LandOnArrival { get; set; } = true;

    public uint MountOverride { get; set; }

    public int PathInterval { get; set; } = 2000;

    public ETravel State { get; private set; } = ETravel.Idle;

    public string Status
    {
        get => status;
        private set
        {
            if (value == status)
                return;

            status = value;
            var shape = Digits.Replace(value, "#");
            if (shape == loggedStatus)
                return;

            loggedStatus = shape;
            Plugin.Log.Information($"[Travel] {value}");
        }
    }

    private static readonly Regex Digits = new("[0-9]+");

    public bool IsMoving => State is ETravel.Teleporting or ETravel.Walking;

    public static bool CanReach(uint territoryId)
    {
        if (Plugin.ClientState.TerritoryType == territoryId || GameData.NearestAetheryte(territoryId, null) != null)
            return true;

        return PassageTo(territoryId) is { } passage && GameData.NearestAetheryte(passage.From, null) != null;
    }

    private static Passage? PassageTo(uint territoryId)
    {
        foreach (var passage in Passages)
        {
            if (passage.To == territoryId)
                return passage;
        }

        return null;
    }

    public void Go(uint territoryId, Vector3 position, float range, string label, float probe = DefaultProbeRadius,
        bool exact = false, IReadOnlyList<Vector3>? waypoints = null)
    {
        territory = territoryId;
        path = waypoints is { Count: > 0 } way ? System.Linq.Enumerable.ToArray(way) : System.Array.Empty<Vector3>();
        pathAt = 0;
        pathEnd = position;
        pathStop = range;
        destination = path.Length > 0 ? path[0] : position;
        reachable = null;
        stopRange = path.Length > 0 ? WaypointRange : range;
        probeRadius = probe;
        precise = exact;
        settled = false;
        settleAttempts = 0;
        name = label;
        zoneTeleportConsidered = false;
        zoneTeleportPending = false;
        aethernetConsidered = false;
        aethernetPending = false;
        aethernetRequestedAt = DateTime.MinValue;
        hopFrom = null;
        hopTo = null;
        hopWalking = false;
        hopExact = false;
        reachProbe = null;
        hopCandidates = null;
        walkLength = null;
        descending = false;
        landing = null;
        descendAt = DateTime.MinValue;
        directRetries = 0;
        stuckRecoveries = 0;
        viaReached = false;
        flightAskedAt = DateTime.MinValue;
        highRouteFailed = false;
        aimedHigh = false;

        settleUntil = DateTime.MinValue;
        teleportRequestedAt = DateTime.MinValue;
        listWaitStarted = DateTime.MinValue;
        mountRetryAt = DateTime.MinValue;
        takeoffRetryAt = DateTime.MinValue;
        dismountRetryAt = DateTime.MinValue;
        teleportAttempts = 0;
        mountAttempts = 0;
        takeoffAttempts = 0;
        dismountAttempts = 0;
        wantMount = false;
        wantFlight = false;
        mountAbandoned = false;
        flightAbandoned = false;
        dispatched = false;
        stuck.Reset(Plugin.ObjectTable[0]?.Position ?? Vector3.Zero);

        State = ETravel.Teleporting;
        Status = $"Heading to {name}";
    }

    public void Stop()
    {
        if (IpcHub.Navmesh.IsPathRunning() || IpcHub.Navmesh.IsPathfindingInProgress())
            IpcHub.Navmesh.StopPathfinding();

        hopWalking = false;
        State = ETravel.Idle;
        Status = "Idle";
    }

    public ETravel Update()
    {
        if (!IsMoving)
            return State;

        if (!Settled())
            return State;

        if (aethernetPending)
        {
            if (!Landed(hopTo, aethernetRequestedAt, HopLanding, "aethernet hop"))
            {
                Status = $"Riding the aethernet to {hopTo?.Name}";
                return State;
            }

            aethernetPending = false;
            dispatched = false;
            reachable = null;
        }

        if (hopWalking)
        {
            WalkToShard();
            return State;
        }

        var here = Plugin.ClientState.TerritoryType;

        if (here != territory)
        {
            if (PassageTo(territory) is { } passage && passage.From == here)
            {
                if (AllowAethernet && !aethernetConsidered && PlanHop(here, passage))
                    return State;

                Cross(passage);
            }
            else
            {
                Teleport();
            }

            return State;
        }

        if (zoneTeleportPending)
        {
            if (!Landed(zoneTeleportTo, zoneTeleportRequestedAt, TeleportLanding, "teleport"))
            {
                Status = $"Teleporting to {zoneTeleportTo?.Name}";
                return State;
            }

            zoneTeleportPending = false;
            dispatched = false;
            reachable = null;
        }

        if (AllowZoneTeleport && !zoneTeleportConsidered && TeleportWithinZone())
            return State;

        if (AllowAethernet && !aethernetConsidered && ProbeReach(here))
            return State;

        if (AllowAethernet && !aethernetConsidered && PlanHop(here, null))
            return State;

        Walk();
        return State;
    }

    private bool PlanHop(uint here, Passage? passage)
    {
        aethernetConsidered = true;

        if (!Dependencies.IsLoaded(Dependencies.Lifestream))
            return Decline("Lifestream is not loaded");

        if (Plugin.ObjectTable[0] is not { } player)
            return false;

        if (GameData.NearestNode(territory, destination) is not { } to)
            return Decline($"no attuned, placed aethernet node in {GameData.GetZoneName(territory)}: {GameData.NodeReport(territory)}");

        if (GameData.NearestNode(here, player.Position) is not { } from)
            return Decline($"no attuned, placed aethernet node in {GameData.GetZoneName(here)}: {GameData.NodeReport(here)}");

        if (from.Group != to.Group)
            return Decline($"{from.Name} (group {from.Group}) and {to.Name} (group {to.Group}) are on different networks");

        if (from.AetheryteId == to.AetheryteId)
            return Decline($"{to.Name} is already the nearest node to both ends");

        if (passage == null)
        {
            var walking = walkLength ?? Yalms.Flat(player.Position, destination);
            var riding = Yalms.Flat(player.Position, from.Position) + Yalms.Flat(to.Position, destination) + AethernetGain;
            if (riding >= walking)
                return Decline($"{walking:F0}y on foot beats {riding:F0}y by aethernet via {from.Name} and {to.Name}");

            Plugin.Log.Information($"[Travel] {name} is {walking:F0}y on foot; riding the aethernet from {from.Name} to {to.Name} instead");
        }
        else
        {
            Plugin.Log.Information($"[Travel] {name} is across the zone line; riding the aethernet from {from.Name} to {to.Name} instead");
        }

        StartHop(from, to, player);
        return true;
    }

    private void StartHop(GameData.AetheryteInfo from, GameData.AetheryteInfo to, Dalamud.Game.ClientState.Objects.Types.IGameObject player)
    {
        hopFrom = from;
        hopTo = to;
        hopWalking = true;
        hopExact = false;
        dispatched = false;
        stuck.Reset(player.Position);
    }

    private bool Decline(string why)
    {
        Plugin.Log.Information($"[Travel] No aethernet for {name}: {why}");
        return false;
    }

    private enum EReach
    {
        Unknown,
        Reaches,
        CutOff,
    }

    private bool ProbeReach(uint here)
    {
        if (!Dependencies.IsLoaded(Dependencies.Lifestream) || Plugin.ObjectTable[0] is not { } player)
            return false;

        if (GameData.NearestNode(here, destination) == null)
            return false;

        if (!IpcHub.Navmesh.IsNavmeshReady())
        {
            Status = IpcHub.Navmesh.WaitText();
            return true;
        }

        if (reachProbe == null)
        {
            var goal = IpcHub.Navmesh.NearestPoint(destination, probeRadius, ProbeHeight) ?? destination;
            var start = player.Position;

            if (hopCandidates != null)
            {
                var shard = hopCandidates[hopCandidate];
                start = IpcHub.Navmesh.NearestPoint(shard.Position, ShardMatch, ProbeHeight) ?? shard.Position;
            }

            reachProbe = IpcHub.Navmesh.Pathfind(start, goal, false);
            probeStartedAt = DateTime.Now;

            if (reachProbe == null)
            {
                hopCandidates = null;
                return false;
            }
        }

        if (!reachProbe.IsCompleted)
        {
            if (DateTime.Now - probeStartedAt < ProbeLimit)
            {
                Status = $"Checking the way to {name}";
                return true;
            }

            Plugin.Log.Warning($"[Travel] The mesh took too long to answer for {name}; planning without it");
            reachProbe = null;
            hopCandidates = null;
            return false;
        }

        var found = reachProbe.IsCompletedSuccessfully ? reachProbe.Result : new List<Vector3>();
        reachProbe = null;
        var verdict = Reaches(found);

        if (hopCandidates == null)
        {
            if (verdict != EReach.CutOff)
            {
                walkLength = verdict == EReach.Reaches ? Length(found) : null;
                return false;
            }

            hopCandidates = GameData.Nodes(here).OrderBy(x => Yalms.Flat(x.Position, destination)).ToList();
            hopCandidate = 0;
            Plugin.Log.Information($"[Travel] The mesh finds no way from here to {name}; asking which aethernet node reaches it");
            return hopCandidates.Count > 0;
        }

        if (verdict == EReach.Reaches)
        {
            var to = hopCandidates[hopCandidate];
            hopCandidates = null;
            return ForceHop(here, to, player);
        }

        hopCandidate++;
        if (hopCandidate < hopCandidates.Count)
            return true;

        hopCandidates = null;
        return Decline($"no aethernet node in {GameData.GetZoneName(here)} reaches {name} on the mesh");
    }

    private bool ForceHop(uint here, GameData.AetheryteInfo to, Dalamud.Game.ClientState.Objects.Types.IGameObject player)
    {
        aethernetConsidered = true;

        if (GameData.NearestNode(here, player.Position) is not { } from)
            return Decline($"no attuned, placed aethernet node in {GameData.GetZoneName(here)}: {GameData.NodeReport(here)}");

        if (from.AetheryteId == to.AetheryteId)
            return Decline($"{to.Name} reaches {name} on the mesh and is already the nearest node here");

        Plugin.Log.Information($"[Travel] {name} is cut off from here on the mesh; riding the aethernet from {from.Name} to {to.Name}");
        StartHop(from, to, player);
        return true;
    }

    private static EReach Reaches(List<Vector3> found)
    {
        if (found.Count < 2)
            return EReach.Unknown;

        for (var i = 1; i < found.Count; i++)
        {
            if (Yalms.Solid(found[i - 1], found[i]) > WarpJump)
                return EReach.CutOff;
        }

        return Yalms.Solid(found[^2], found[^1]) <= PathEndSlack ? EReach.Reaches : EReach.CutOff;
    }

    private static float Length(List<Vector3> found)
    {
        var total = 0f;
        for (var i = 1; i < found.Count; i++)
            total += Yalms.Solid(found[i - 1], found[i]);

        return total;
    }

    private void WalkToShard()
    {
        State = ETravel.Walking;

        if (Plugin.ObjectTable[0] is not { } player || hopFrom is not { } from || hopTo is not { } to)
        {
            hopWalking = false;
            return;
        }

        var node = NodeObject(from);
        var at = node?.Position ?? new Vector3(from.Position.X, player.Position.Y, from.Position.Z);
        var reach = GameData.IsHousingShard(from.AetheryteId) ? HousingHopReach : HopReach;

        if (node != null && !hopExact)
        {
            hopExact = true;
            dispatched = false;
        }

        var away = Yalms.Flat(player.Position, at);

        if (away <= reach)
        {
            hopWalking = false;

            if (IpcHub.Navmesh.IsPathRunning() || IpcHub.Navmesh.IsPathfindingInProgress())
                IpcHub.Navmesh.StopPathfinding();

            dispatched = false;
            reachable = null;

            var accepted = GameData.IsHousingShard(to.AetheryteId)
                ? IpcHub.Lifestream.HousingAethernetTeleport(to.AetheryteId)
                : IpcHub.Lifestream.AethernetTeleport(to.AetheryteId);

            if (!accepted)
            {
                Plugin.Log.Information($"[Travel] Lifestream declined the aethernet hop to {to.Name}; walking to {name}");
                return;
            }

            Plugin.Log.Information($"[Travel] Riding the aethernet from {from.Name} to {to.Name} for {name}");
            State = ETravel.Teleporting;
            Status = $"Riding the aethernet to {to.Name}";
            aethernetPending = true;
            aethernetRequestedAt = DateTime.Now;
            return;
        }

        Status = $"Walking to the aethernet at {from.Name}";

        if (stuck.IsStuck(player.Position, StuckThreshold))
        {
            GiveUpHop($"Stuck on the way to the aethernet at {from.Name}");
            return;
        }

        if (dispatched && (IpcHub.Navmesh.IsPathRunning() || IpcHub.Navmesh.IsPathfindingInProgress()))
            return;

        if (!IpcHub.Navmesh.IsNavmeshReady())
        {
            Status = IpcHub.Navmesh.WaitText();
            dispatched = false;
            stuck.Reset(player.Position);
            return;
        }

        if (dispatched)
        {
            GiveUpHop($"The mesh stopped {away:F0}y short of the aethernet at {from.Name}");
            return;
        }

        if (!Throttle.Ready($"wahtools.travel.{instance}.shard", PathInterval))
            return;

        dispatched = true;
        stuck.Reset(player.Position);
        ConsiderSprint(away);

        if (!IpcHub.Navmesh.PathfindAndMoveCloseTo(at, false, reach / 2f))
            GiveUpHop($"Navmesh refused to path to the aethernet at {from.Name}");
    }

    private void GiveUpHop(string why)
    {
        Plugin.Log.Warning($"[Travel] {why}; walking to {name} instead");
        hopWalking = false;
        dispatched = false;
        stuck.Reset(Plugin.ObjectTable[0]?.Position ?? Vector3.Zero);

        if (IpcHub.Navmesh.IsPathRunning() || IpcHub.Navmesh.IsPathfindingInProgress())
            IpcHub.Navmesh.StopPathfinding();
    }

    private static Dalamud.Game.ClientState.Objects.Types.IGameObject? NodeObject(GameData.AetheryteInfo node)
    {
        var housing = GameData.IsHousingShard(node.AetheryteId);
        Dalamud.Game.ClientState.Objects.Types.IGameObject? nearest = null;
        var closest = ShardMatch;

        foreach (var thing in Plugin.ObjectTable)
        {
            if (thing.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Aetheryte)
                continue;

            if (!housing)
            {
                if (thing.BaseId == node.AetheryteId)
                    return thing;

                continue;
            }

            var away = Yalms.Flat(thing.Position, node.Position);
            if (away >= closest)
                continue;

            closest = away;
            nearest = thing;
        }

        return nearest;
    }

    private bool TeleportWithinZone()
    {
        zoneTeleportConsidered = true;

        if (Plugin.ObjectTable[0] is not { } player)
            return false;

        var remaining = Yalms.Flat(player.Position, destination);
        if (remaining < ZoneTeleportDistance)
            return false;

        var aetheryte = GameData.NearestAetheryte(territory, destination);
        if (aetheryte == null || !aetheryte.HasPosition)
            return false;

        if (Yalms.Flat(aetheryte.Position, destination) + ZoneTeleportGain > remaining)
            return false;

        if (!IpcHub.Lifestream.Teleport(aetheryte.AetheryteId, aetheryte.SubIndex))
            return false;

        Plugin.Log.Information($"[Travel] {name} is {remaining:F0}y away; teleporting to {aetheryte.Name} first");
        State = ETravel.Teleporting;
        Status = $"Teleporting to {aetheryte.Name}";
        zoneTeleportPending = true;
        zoneTeleportTo = aetheryte;
        zoneTeleportRequestedAt = DateTime.Now;
        return true;
    }

    private static bool Landed(GameData.AetheryteInfo? at, DateTime since, TimeSpan limit, string what)
    {
        if (Plugin.ObjectTable[0] is not { } player)
            return false;

        if (at is { HasPosition: true } && Yalms.Flat(player.Position, at.Position) <= LandingReach)
            return true;

        if (Plugin.Condition[ConditionFlag.Casting] || IpcHub.Lifestream.IsBusy || DateTime.Now - since < limit)
            return false;

        Plugin.Log.Warning($"[Travel] The {what} to {at?.Name} did not land within {limit.TotalSeconds:F0}s; carrying on from here");
        return true;
    }

    private void Cross(Passage passage)
    {
        State = ETravel.Walking;

        var player = Plugin.ObjectTable[0];
        if (player == null)
            return;

        var zone = GameData.GetZoneName(passage.To);
        Status = $"Crossing into {zone}";

        var toVia = passage.Via != null && !viaReached;
        var point = toVia ? passage.Via!.Value : passage.Cross;

        if (Yalms.Flat(player.Position, point) <= (toVia ? ViaRange : CrossingRange))
        {
            if (toVia)
            {
                viaReached = true;
                dispatched = false;
            }

            return;
        }

        if (stuck.IsStuck(player.Position, StuckThreshold))
        {
            Fail($"Stuck crossing into {zone}");
            return;
        }

        if (dispatched && (IpcHub.Navmesh.IsPathRunning() || IpcHub.Navmesh.IsPathfindingInProgress()))
            return;

        if (!IpcHub.Navmesh.IsNavmeshReady())
        {
            Status = IpcHub.Navmesh.WaitText();
            dispatched = false;
            stuck.Reset(player.Position);
            return;
        }

        if (!Throttle.Ready($"wahtools.travel.{instance}.cross", PathInterval))
            return;

        dispatched = true;
        stuck.Reset(player.Position);

        if (!IpcHub.Navmesh.PathfindAndMoveTo(point, false))
        {
            dispatched = false;
            Plugin.Log.Warning($"[Travel] Navmesh refused to path to the crossing into {zone} at {point}");
        }
    }

    private bool Settled()
    {
        var here = Plugin.ClientState.TerritoryType;

        if (here != lastTerritory)
        {
            if (lastTerritory != 0)
                Plugin.Log.Information($"[Travel] Now in {GameData.GetZoneName(here)} ({here}), settling before moving on to {name}");

            lastTerritory = here;
            settleUntil = DateTime.Now.Add(ArrivalSettle);
            dispatched = false;
        }

        if (PlayerActions.Zoning || Plugin.ObjectTable[0] == null)
        {
            settleUntil = DateTime.Now.Add(ArrivalSettle);
            dispatched = false;
            Status = "Loading";
            return false;
        }

        return DateTime.Now >= settleUntil;
    }

    private void Teleport()
    {
        State = ETravel.Teleporting;

        var busy = IpcHub.Lifestream.IsBusy;
        var casting = Plugin.Condition[ConditionFlag.Casting];
        var cooling = DateTime.Now < teleportRequestedAt.Add(TravelLimits.TeleportRetryInterval);

        if (busy || casting || cooling)
        {
            if (Throttle.Ready("wahtools.travel.teleportheld", 30000))
                Plugin.Log.Information($"[Travel] Holding the teleport to {name}: lifestream busy {busy}, " +
                                       $"casting {casting}, cooling {cooling}");

            return;
        }

        GameData.RefreshAetheryteList();

        if (!GameData.AetheryteListLoaded)
        {
            if (listWaitStarted == DateTime.MinValue)
                listWaitStarted = DateTime.Now;
            else if (DateTime.Now - listWaitStarted > ListWaitLimit)
                Fail("The teleport list never loaded");

            Status = "Waiting for the teleport list";
            return;
        }

        listWaitStarted = DateTime.MinValue;

        var aetheryte = GameData.NearestAetheryte(territory, destination);

        if (aetheryte == null && PassageTo(territory) is { } passage)
            aetheryte = GameData.NearestAetheryte(passage.From, passage.Cross);

        if (aetheryte == null)
        {
            Plugin.Log.Warning($"[Travel] {GameData.GetZoneName(territory)} ({territory}): {GameData.AttunementReport(territory)}");
            Fail($"No attuned aetheryte in {GameData.GetZoneName(territory)}");
            return;
        }

        if (teleportAttempts >= TravelLimits.MaxTeleportAttempts)
        {
            Fail($"Could not teleport to {aetheryte.Name} after {TravelLimits.MaxTeleportAttempts} attempts");
            return;
        }

        teleportAttempts++;
        teleportRequestedAt = DateTime.Now;
        Status = $"Teleporting to {aetheryte.Name}";
        Plugin.Log.Information($"[Travel] Teleport {teleportAttempts}/{TravelLimits.MaxTeleportAttempts} to {aetheryte.Name} " +
                               $"for {name} in {GameData.GetZoneName(territory)}");

        if (!IpcHub.Lifestream.Teleport(aetheryte.AetheryteId, aetheryte.SubIndex))
            Plugin.Log.Warning($"[Travel] Lifestream declined to teleport to {aetheryte.Name}, " +
                               $"attempt {teleportAttempts}/{TravelLimits.MaxTeleportAttempts}");
    }

    private void Walk()
    {
        State = ETravel.Walking;
        teleportAttempts = 0;

        var player = Plugin.ObjectTable[0]!;
        var target = reachable ?? destination;
        var flat = RequireFlight
            ? Yalms.Solid(player.Position, target)
            : Yalms.Flat(player.Position, target);

        if (descending || flat <= stopRange)
        {
            if (pathAt < path.Length)
            {
                var straight = pathAt > 0 && IpcHub.Navmesh.IsPathRunning();
                if (!straight)
                {
                    if (IpcHub.Navmesh.IsPathRunning() && !descending)
                        IpcHub.Navmesh.StopPathfinding();

                    dispatched = false;
                }

                directRetries = 0;
                NextWaypoint(player.Position);

                if (pathAt >= path.Length)
                {
                    if (IpcHub.Navmesh.IsPathRunning())
                        IpcHub.Navmesh.StopPathfinding();

                    dispatched = false;
                }

                Status = $"Following the marked way to {name}, {pathAt}/{path.Length}";
                Plugin.Log.Information($"[Travel] Waypoint {pathAt}/{path.Length} for {name}, heading for {destination}");
                return;
            }

            if (IpcHub.Navmesh.IsPathRunning() && !descending && !(precise && !settled))
                IpcHub.Navmesh.StopPathfinding();

            dispatched = false;

            if (Land())
                return;

            if (flat > stopRange)
                return;

            if (Settle(player.Position))
                return;

            State = ETravel.Arrived;
            Status = $"At {name}";
            return;
        }

        if (stuck.IsStuck(player.Position, StuckThreshold))
        {
            if (stuckRecoveries >= MaxStuckRecoveries)
            {
                Fail($"Stuck on the way to {name}");
                return;
            }

            stuckRecoveries++;
            reachable = null;
            dispatched = false;
            stuck.Reset(player.Position);
            IpcHub.Navmesh.StopPathfinding();
            Plugin.Log.Warning($"[Travel] Stuck on the way to {name}; asking the mesh for a fresh route");
            return;
        }

        var following = path.Length > 0 && pathAt > 0 && pathAt < path.Length;

        if (!following && DriveMount(flat))
            return;

        ConsiderSprint(flat);

        Status = Plugin.Condition[ConditionFlag.InFlight] ? $"Flying to {name}"
            : wantFlight && Plugin.Condition[ConditionFlag.Mounted] ? $"Taking off for {name}"
            : $"Walking to {name}";

        if (dispatched && (IpcHub.Navmesh.IsPathRunning() || IpcHub.Navmesh.IsPathfindingInProgress()))
            return;

        if (!IpcHub.Navmesh.IsNavmeshReady())
        {
            Status = IpcHub.Navmesh.WaitText();
            dispatched = false;
            stuck.Reset(player.Position);
            return;
        }

        if (dispatched)
        {
            dispatched = false;

            if (following)
            {
                if (++directRetries > 3)
                {
                    Plugin.Log.Warning($"[Travel] Marker {pathAt + 1}/{path.Length} for {name} at {destination} cannot be reached straight; skipping it");
                    directRetries = 0;
                    NextWaypoint(player.Position);
                }

                return;
            }

            if (aimedHigh && !highRouteFailed && Yalms.Flat(player.Position, dispatchedFrom) < MovedTolerance)
            {
                highRouteFailed = true;
                Plugin.Log.Information($"[Travel] No flying route to a point above {name}; aiming at the ground instead");
            }
            else if (reachable == null)
            {
                if (!Probe())
                    return;
            }
            else if (flat <= stopRange * 4f)
            {
                State = ETravel.Arrived;
                Status = $"As close to {name} as the ground goes";
                return;
            }
            else
            {
                Fail($"Could not get closer than {flat:F0}y to {name}");
                return;
            }
        }

        if (IpcHub.Navmesh.IsPathfindingInProgress())
        {
            Status = "Waiting for the mesh to answer an earlier request";
            stuck.Reset(player.Position);
            return;
        }

        if (!Throttle.Ready($"wahtools.travel.{instance}.{territory}", PathInterval))
            return;

        dispatched = true;
        stuck.Reset(player.Position);

        if (following)
        {
            var legs = new List<Vector3>(path[pathAt..]);
            var low = Plugin.Condition[ConditionFlag.InFlight];
            Plugin.Log.Information($"[Travel] Marker to marker for {name}, {legs.Count} to go{(low ? ", flying low along them" : "")}");
            IpcHub.Navmesh.MoveTo(legs, low);
            return;
        }

        var flying = Plugin.Condition[ConditionFlag.InFlight] || (wantFlight && Plugin.Condition[ConditionFlag.Mounted]);
        if (flying && !Plugin.Condition[ConditionFlag.InFlight])
            flightAskedAt = DateTime.Now;
        var goal = reachable ?? destination;
        aimedHigh = flying && !RequireFlight && !highRouteFailed && path.Length == 0;
        if (aimedHigh)
            goal.Y += CruiseHeight;
        dispatchedFrom = player.Position;
        Plugin.Log.Information($"[Travel] Asking the mesh for a {(flying ? "flying" : "ground")} route to {name} " +
                               $"at {goal}, {flat:F0}y away");

        if (IpcHub.Navmesh.PathfindAndMoveCloseTo(goal, flying, stopRange / 2f))
            return;

        dispatched = false;

        if (reachable != null)
            Plugin.Log.Warning($"[Travel] Navmesh refused to path to {name} at {reachable.Value}");
        else
            Probe();
    }

    private bool Land()
    {
        wantMount = false;
        wantFlight = false;

        if (!LandOnArrival || (!Plugin.Condition[ConditionFlag.Mounted] && !Plugin.Condition[ConditionFlag.InFlight]))
        {
            descending = false;
            landing = null;
            return false;
        }

        if (Plugin.Condition[ConditionFlag.InFlight])
        {
            if (!descending)
            {
                descending = true;
                descendAt = DateTime.Now;
                var onto = reachable ?? destination;
                landing = Terrain.LevelSpot(onto, LedgeInner, LedgeRing, LedgeLevel,
                    spot => IpcHub.Navmesh.NearestPoint(spot, 1.5f, 2f) != null);

                if (landing is { } back)
                {
                    Plugin.Log.Information($"[Travel] {name} sits at an edge; coming down {Yalms.Flat(back, onto):F0}y back from it at {back}");
                    onto = back;
                }

                IpcHub.Navmesh.PathfindAndMoveTo(onto, true);
                Plugin.Log.Information($"[Travel] Coming down onto {name} before dismounting");
                Status = $"Coming down onto {name}";
                return true;
            }

            if ((IpcHub.Navmesh.IsPathRunning() || IpcHub.Navmesh.IsPathfindingInProgress()) &&
                DateTime.Now - descendAt < DescendLimit)
            {
                Status = $"Coming down onto {name}";
                return true;
            }
        }

        Status = Plugin.Condition[ConditionFlag.InFlight] ? $"Landing at {name}" : "Dismounting";

        if (dismountAttempts >= TravelLimits.MaxDismountAttempts)
        {
            Fail($"Could not get off the mount at {name}");
            return true;
        }

        if (PlayerActions.IsBusyWithAnimation() || DateTime.Now < dismountRetryAt)
            return true;

        dismountRetryAt = DateTime.Now.Add(TravelLimits.MountRetryInterval);
        dismountAttempts++;
        PlayerActions.Dismount();
        return true;
    }

    private bool DriveMount(float remaining)
    {
        if (Plugin.Condition[ConditionFlag.InCombat])
            return false;

        var territory = Plugin.ClientState.TerritoryType;
        var flightWanted = AllowFlight && !flightAbandoned && GameData.CanFly(territory) &&
                           (RequireFlight || remaining >= FlightDistance);

        if (!wantMount && !Plugin.Condition[ConditionFlag.Mounted] &&
            (mountAbandoned || !GameData.CanMount(territory) || (remaining < MountDistance && !flightWanted)))
            return false;

        wantMount = true;

        if (!Plugin.Condition[ConditionFlag.Mounted])
        {
            if (mountAttempts >= TravelLimits.MaxMountAttempts)
            {
                Plugin.Log.Warning($"[Travel] Mount did not take after {mountAttempts} tries; walking to {name}");
                mountAbandoned = true;
                wantMount = false;
                wantFlight = false;
                return false;
            }

            if (PlayerActions.IsBusyWithAnimation() || DateTime.Now < mountRetryAt)
                return false;

            mountRetryAt = DateTime.Now.Add(TravelLimits.MountRetryInterval);
            mountAttempts++;
            PlayerActions.Mount(MountOverride);
            return false;
        }

        mountAttempts = 0;

        if (wantFlight && !flightWanted && (!AllowFlight || flightAbandoned || !GameData.CanFly(territory)))
        {
            Plugin.Log.Information($"[Travel] {GameData.GetZoneName(territory)} does not allow flight; riding to {name}");
            wantFlight = false;
            takeoffAttempts = 0;
            dispatched = false;
            return false;
        }

        if (!wantFlight && !flightWanted)
            return false;

        if (!wantFlight)
        {
            if (Plugin.Condition[ConditionFlag.Mounting] || Plugin.Condition[ConditionFlag.Mounting71])
                return false;

            wantFlight = true;
            dispatched = false;
            flightAskedAt = DateTime.Now;
            IpcHub.Navmesh.StopPathfinding();
            Plugin.Log.Information($"[Travel] Mounted; asking the mesh for a flying route to {name} and leaving the takeoff to it");
            return true;
        }

        if (Plugin.Condition[ConditionFlag.InFlight])
        {
            takeoffAttempts = 0;
            return false;
        }

        if (DateTime.Now < flightAskedAt.Add(TakeoffGrace) || !dispatched)
            return false;

        if (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.Casting] ||
            Plugin.Condition[ConditionFlag.Mounting] || Plugin.Condition[ConditionFlag.Mounting71])
            return false;

        if (DateTime.Now < takeoffRetryAt)
            return false;

        if (takeoffAttempts >= TravelLimits.MaxTakeoffAttempts)
        {
            Plugin.Log.Warning($"[Travel] Could not get airborne after {takeoffAttempts} tries; riding to {name}");
            flightAbandoned = true;
            wantFlight = false;
            dispatched = false;
            IpcHub.Navmesh.StopPathfinding();
            return false;
        }

        takeoffRetryAt = DateTime.Now.Add(TravelLimits.TakeoffRetryInterval);
        takeoffAttempts++;
        PlayerActions.TakeOff();
        return false;
    }

    private void ConsiderSprint(float remaining)
    {
        if (wantMount)
            return;

        if (PlayerActions.TrySprint(remaining, $"wahtools.travel.{instance}.sprint", MinimumSprintDistance))
            Plugin.Log.Information($"[Travel] Sprinting with {remaining:F0}y to {name}");
    }

    private bool Settle(Vector3 at)
    {
        if (!precise || settled)
            return false;

        var away = Yalms.Flat(at, destination);

        if (away <= PreciseRange || away > SettleReach || settleAttempts >= MaxSettleAttempts)
        {
            settled = true;
            return false;
        }

        if (IpcHub.Navmesh.IsPathRunning())
        {
            Status = $"Settling onto {name}";
            return true;
        }

        settleAttempts++;
        IpcHub.Navmesh.MoveTo(destination, false);
        Plugin.Log.Information($"[Travel] {away:F1}y off {name}; walking the last of it straight");
        Status = $"Settling onto {name}";
        return true;
    }

    private bool OnPath => pathAt < path.Length;

    private void NextWaypoint(Vector3 from)
    {
        pathAt++;
        destination = pathAt < path.Length ? path[pathAt] : pathEnd;
        stopRange = pathAt < path.Length ? WaypointRange : pathStop;
        reachable = null;
        settled = false;
        settleAttempts = 0;
        stuck.Reset(from);
    }

    private bool Probe()
    {
        if (OnPath)
        {
            Plugin.Log.Warning($"[Travel] Waypoint {pathAt + 1}/{path.Length} for {name} at {destination} " +
                               "is out of the mesh's reach; skipping it");
            NextWaypoint(Plugin.ObjectTable[0]?.Position ?? destination);
            return true;
        }

        reachable = IpcHub.Navmesh.NearestReachablePoint(destination, probeRadius, ProbeHeight);

        if (reachable == null)
        {
            Fail($"Nothing reachable within {probeRadius:F0}y of {name}");
            return false;
        }

        Plugin.Log.Information($"[Travel] {name} cannot be stood on; heading for {reachable.Value} instead");
        return true;
    }

    private void Fail(string reason)
    {
        Plugin.Log.Warning($"[Travel] {reason}");
        State = ETravel.Failed;
        Status = reason;
    }
}
