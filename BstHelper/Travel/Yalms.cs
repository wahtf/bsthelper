using System.Numerics;

namespace BstHelper.Travel;

public static class Yalms
{
    public static float Flat(Vector3 from, Vector3 to)
        => new Vector2(from.X - to.X, from.Z - to.Z).Length();

    public static float Solid(Vector3 from, Vector3 to) => Vector3.Distance(from, to);
}
