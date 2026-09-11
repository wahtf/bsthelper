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

    public static Vector3 ToWorld(uint territory, float mapX, float mapY)
    {
        if (For(territory) is not { } map)
            return Vector3.Zero;

        var scale = map.SizeFactor / 100f;
        return new Vector3(Axis(mapX, scale, map.OffsetX), 0f, Axis(mapY, scale, map.OffsetY));
    }

    private static float Axis(float mapCoord, float scale, float offset)
        => ((((mapCoord - 1f) * scale / 41f) * 2048f) - 1024f) / scale - offset;
}
