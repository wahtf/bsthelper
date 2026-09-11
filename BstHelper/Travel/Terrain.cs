using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;

namespace BstHelper.Travel;

public static unsafe class Terrain
{
    private static readonly Vector3 Down = new(0f, -1f, 0f);

    private const int Around = 16;
    private const float Lift = 3f;
    private const float DropLook = 40f;

    public static bool Ready => Framework.Instance() != null && Framework.Instance()->BGCollisionModule != null;

    public static float? GroundBelow(Vector3 from, float reach = 2000f)
    {
        if (!Ready)
            return null;

        try
        {
            return BGCollisionModule.RaycastMaterialFilter(from, Down, out var hit, reach)
                ? hit.Point.Y
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static Vector3? LevelSpot(Vector3 point, float inner, float outer, float sameLevel, Func<Vector3, bool> walkable)
    {
        if (!Ready)
            return null;

        if (Ring(point, inner, sameLevel, walkable, out _).Count == Around)
            return null;

        var level = Ring(point, outer, sameLevel, walkable, out var drop);
        if (level.Count == 0 || drop == Vector2.Zero)
            return null;

        var away = Vector2.Normalize(-drop);
        Vector3? best = null;
        var bestDot = 0f;

        foreach (var (at, dir) in level)
        {
            var dot = Vector2.Dot(dir, away);
            if (dot <= bestDot)
                continue;

            bestDot = dot;
            best = at;
        }

        return best;
    }

    private static System.Collections.Generic.List<(Vector3 At, Vector2 Dir)> Ring(Vector3 point, float radius, float sameLevel,
        Func<Vector3, bool> walkable, out Vector2 drop)
    {
        var level = new System.Collections.Generic.List<(Vector3 At, Vector2 Dir)>();
        drop = Vector2.Zero;

        for (var step = 0; step < Around; step++)
        {
            var angle = MathF.Tau * step / Around;
            var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var column = new Vector3(point.X + (dir.X * radius), point.Y + Lift, point.Z + (dir.Y * radius));
            var ground = GroundBelow(column, Lift + sameLevel + DropLook);

            if (ground is { } y && MathF.Abs(y - point.Y) <= sameLevel && walkable(column with { Y = y }))
                level.Add((column with { Y = y }, dir));
            else
                drop += dir;
        }

        return level;
    }
}
