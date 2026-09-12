using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Lumina.Excel.Sheets;
using Map = Lumina.Excel.Sheets.Map;

namespace BstHelper.Bestiary;

public static class MapCoords
{
    private static readonly Dictionary<uint, Map> MapsByTerritory = new();
    private static bool loaded;

    private static Map? For(uint territory)
    {
        if (!loaded)
        {
            foreach (var map in Plugin.DataManager.GetExcelSheet<Map>().Where(x => x.RowId > 0))
            {
                var owner = map.TerritoryType.RowId;
                if (owner != 0)
                    MapsByTerritory.TryAdd(owner, map);
            }

            loaded = true;
        }

        return MapsByTerritory.TryGetValue(territory, out var found) ? found : null;
    }

    public static Vector2 ToMap(uint territory, Vector3 world)
    {
        if (For(territory) is not { } map)
            return Vector2.Zero;

        var scale = map.SizeFactor / 100f;
        return new Vector2(Coord(world.X, scale, map.OffsetX), Coord(world.Z, scale, map.OffsetY));
    }

    private static float Coord(float world, float scale, float offset)
        => 1f + (41f * (((world + offset) * scale) + 1024f) / (2048f * scale));
}
