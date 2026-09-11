using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

namespace BstHelper.Bestiary;

public static class Mobs
{
    private const int Most = 12;

    private static readonly IReadOnlyList<IGameObject> None = Array.Empty<IGameObject>();

    public static IReadOnlyList<IGameObject> Nearby(Beast beast)
    {
        if (!beast.IsOverworld || Plugin.ClientState.TerritoryType != beast.Territory)
            return None;

        if (Plugin.ObjectTable.LocalPlayer is not { } me)
            return None;

        try
        {
            return Plugin.ObjectTable
                .Where(x => x.ObjectKind == ObjectKind.BattleNpc && !x.IsDead && Named(x, beast.Mob))
                .OrderBy(x => Vector3.DistanceSquared(x.Position, me.Position))
                .Take(Most)
                .ToList();
        }
        catch
        {
            return None;
        }
    }

    private static bool Named(IGameObject candidate, string mob)
        => string.Equals(candidate.Name.TextValue, mob, StringComparison.OrdinalIgnoreCase);
}
