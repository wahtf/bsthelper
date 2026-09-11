using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Map = Lumina.Excel.Sheets.Map;

namespace BstHelper.Travel;

public static class GameData
{
    private const int MapMarkerAetheryteType = 3;
    private const int MapMarkerShardType = 4;

    private static readonly Dictionary<uint, AetheryteInfo> Aetherytes = new();

    private static readonly Dictionary<uint, AetheryteInfo> AethernetShards = new();
    private static readonly Dictionary<uint, AetheryteInfo> HousingShards = new();
    private const uint HousingGroupBase = 1_000_000;

    private static readonly Dictionary<uint, uint> AetherCurrentSets = new();

    private static readonly Dictionary<uint, string> TerritoryNames = new();
    private static readonly HashSet<uint> MountableTerritories = new();

    private static bool loaded;

    public static string GetZoneName(uint territoryId)
    {
        if (territoryId == 0)
            return "-";

        EnsureLoaded();

        if (TerritoryNames.TryGetValue(territoryId, out var known))
            return known;

        if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var row))
        {
            var name = row.PlaceName.Value.Name.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                return TerritoryNames[territoryId] = name;
        }

        return "Unknown";
    }

    public static bool CanMount(uint territoryId)
    {
        EnsureLoaded();
        return MountableTerritories.Contains(territoryId);
    }

    public static unsafe bool CanFly(uint territoryId)
    {
        EnsureLoaded();
        if (!AetherCurrentSets.TryGetValue(territoryId, out var compFlgSet))
            return false;

        var playerState = PlayerState.Instance();
        return playerState != null && playerState->IsAetherCurrentZoneComplete(compFlgSet);
    }



    public static unsafe bool Attuned(uint aetheryteId)
    {
        if (HousingShards.ContainsKey(aetheryteId))
            return true;

        var state = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
        return state != null && state->IsAetheryteUnlocked(aetheryteId);
    }

    public static bool IsHousingShard(uint aetheryteId)
    {
        EnsureLoaded();
        return HousingShards.ContainsKey(aetheryteId);
    }


    public static bool AetheryteListLoaded => Plugin.AetheryteList.Any();

    public static unsafe void RefreshAetheryteList()
    {
        if (AetheryteListLoaded || !Plugin.ClientState.IsLoggedIn ||
            Plugin.ObjectTable.LocalPlayer == null ||
            Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51])
            return;

        if (!Throttle.Ready("wahtools.telepo.refresh", 2000))
            return;

        var telepo = Telepo.Instance();
        if (telepo == null)
            return;

        telepo->UpdateAetheryteList();
    }

    public static AetheryteInfo? NearestAetheryte(uint territoryId, Vector3? near)
    {
        EnsureLoaded();

        var candidates = Aetherytes.Values
            .Where(x => x.TerritoryId == territoryId && Attuned(x.AetheryteId))
            .ToList();

        if (candidates.Count == 0)
            return Residential(territoryId);

        if (candidates.Count <= 1 || near == null)
            return candidates.FirstOrDefault();

        return candidates
            .OrderByDescending(x => x.HasPosition)
            .ThenBy(x => Yalms.Flat(x.Position, near.Value))
            .First();
    }

    private static AetheryteInfo? Residential(uint territoryId)
    {
        RefreshAetheryteList();

        var entry = Plugin.AetheryteList
            .Where(x => x.TerritoryId == territoryId && !x.IsSharedHouse && !x.IsApartment)
            .OrderBy(x => x.Plot)
            .ThenBy(x => x.SubIndex)
            .FirstOrDefault();

        if (entry == null)
            return null;

        return new AetheryteInfo(entry.AetheryteId, territoryId, Vector3.Zero, GetZoneName(territoryId), false, 0,
            entry.SubIndex);
    }

    public static AetheryteInfo? NearestNode(uint territoryId, Vector3 near)
        => Nodes(territoryId).OrderBy(x => Yalms.Flat(x.Position, near)).FirstOrDefault();

    public static List<AetheryteInfo> Nodes(uint territoryId)
    {
        EnsureLoaded();

        return Aetherytes.Values.Concat(AethernetShards.Values).Concat(HousingShards.Values)
            .Where(x => x.TerritoryId == territoryId && x.HasPosition && Attuned(x.AetheryteId))
            .ToList();
    }

    public static string NodeReport(uint territoryId)
    {
        EnsureLoaded();

        var nodes = Aetherytes.Values.Concat(AethernetShards.Values).Concat(HousingShards.Values)
            .Where(x => x.TerritoryId == territoryId)
            .Select(x => $"{x.Name} {(x.HasPosition ? "placed" : "unplaced")} {(Attuned(x.AetheryteId) ? "attuned" : "not attuned")}")
            .ToList();

        return nodes.Count == 0 ? "the sheet lists no node there" : string.Join(", ", nodes);
    }

    public static string AttunementReport(uint territoryId)
    {
        EnsureLoaded();

        var listed = Aetherytes.Values
            .Where(x => x.TerritoryId == territoryId)
            .Select(x => $"{x.Name} {(Attuned(x.AetheryteId) ? "attuned" : "not attuned")}")
            .ToList();

        return (listed.Count == 0 ? "the sheet places no aetheryte there" : string.Join(", ", listed)) +
               $"; the teleport list holds {Plugin.AetheryteList.Count()} entries";
    }

    private static void EnsureLoaded()
    {
        if (loaded)
            return;

        loaded = true;

        try
        {
            LoadAetherytes();
            LoadTerritories();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to build sheet lookups; teleport and flight checks will be degraded");
        }
    }

    private static void LoadAetherytes()
    {
        var mapsByMarkerRange = Plugin.DataManager.GetExcelSheet<Map>()
            .Where(x => x.RowId > 0 && x.MapMarkerRange > 0)
            .GroupBy(x => (uint)x.MapMarkerRange)
            .ToDictionary(x => x.Key, x => x.First());

        var markerPositions = new Dictionary<(int Type, uint Key, uint Territory), Vector2>();
        foreach (var subrows in Plugin.DataManager.GetSubrowExcelSheet<MapMarker>())
        {
            if (!mapsByMarkerRange.TryGetValue(subrows.RowId, out var map))
                continue;

            foreach (var marker in subrows)
            {
                var type = (int)marker.DataType;
                if (type is not (MapMarkerAetheryteType or MapMarkerShardType) || marker.DataKey.RowId == 0)
                    continue;

                var scale = map.SizeFactor / 100f;
                var flat = new Vector2(((marker.X - 1024f) / scale) - map.OffsetX, ((marker.Y - 1024f) / scale) - map.OffsetY);
                markerPositions[(type, marker.DataKey.RowId, map.TerritoryType.RowId)] = flat;
                markerPositions.TryAdd((type, marker.DataKey.RowId, 0u), flat);
            }
        }

        foreach (var aetheryte in Plugin.DataManager.GetExcelSheet<Aetheryte>().Where(x => x.RowId > 0))
        {
            if (aetheryte.Territory.RowId == 0)
                continue;

            var type = aetheryte.IsAetheryte ? MapMarkerAetheryteType : MapMarkerShardType;
            var key = aetheryte.IsAetheryte ? aetheryte.RowId : aetheryte.AethernetName.RowId;
            var placed = markerPositions.TryGetValue((type, key, aetheryte.Territory.RowId), out var flat) ||
                         markerPositions.TryGetValue((type, key, 0u), out flat);
            var name = aetheryte.IsAetheryte
                ? aetheryte.PlaceName.ValueNullable?.Name.ExtractText()
                : aetheryte.AethernetName.ValueNullable?.Name.ExtractText();

            var info = new AetheryteInfo(
                aetheryte.RowId,
                aetheryte.Territory.RowId,
                new Vector3(flat.X, 0f, flat.Y),
                name ?? $"#{aetheryte.RowId}",
                placed,
                (uint)aetheryte.AethernetGroup);

            if (aetheryte.IsAetheryte)
                Aetherytes[aetheryte.RowId] = info;
            else
                AethernetShards[aetheryte.RowId] = info;
        }

        foreach (var shard in Plugin.DataManager.GetExcelSheet<HousingAethernet>().Where(x => x.RowId > 0))
        {
            var territory = shard.TerritoryType.RowId;
            if (territory == 0)
                continue;

            var placed = markerPositions.TryGetValue((MapMarkerShardType, shard.PlaceName.RowId, territory), out var flat) ||
                         markerPositions.TryGetValue((MapMarkerShardType, shard.PlaceName.RowId, 0u), out flat);

            HousingShards[shard.RowId] = new AetheryteInfo(
                shard.RowId,
                territory,
                new Vector3(flat.X, 0f, flat.Y),
                shard.PlaceName.ValueNullable?.Name.ExtractText() ?? $"#{shard.RowId}",
                placed,
                HousingGroupBase + territory);
        }

        Plugin.Log.Information($"[Travel] {Aetherytes.Count} aetherytes, {Aetherytes.Values.Count(x => !x.HasPosition)} unplaced; " +
                               $"{AethernetShards.Count} shards, {AethernetShards.Values.Count(x => !x.HasPosition)} unplaced; " +
                               $"{HousingShards.Count} housing shards, {HousingShards.Values.Count(x => !x.HasPosition)} unplaced");
    }

    private static void LoadTerritories()
    {
        foreach (var territory in Plugin.DataManager.GetExcelSheet<TerritoryType>().Where(x => x.RowId > 0))
        {
            if (territory.AetherCurrentCompFlgSet.RowId > 0)
                AetherCurrentSets[territory.RowId] = territory.AetherCurrentCompFlgSet.RowId;

            if (territory.Mount)
                MountableTerritories.Add(territory.RowId);

            var name = territory.PlaceName.ValueNullable?.Name.ExtractText();
            if (!string.IsNullOrEmpty(name))
                TerritoryNames[territory.RowId] = name;
        }
    }

    public sealed record AetheryteInfo(
        uint AetheryteId,
        uint TerritoryId,
        Vector3 Position,
        string Name,
        bool HasPosition,
        uint Group,
        byte SubIndex = 0);
}
