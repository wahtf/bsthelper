using System.Numerics;

namespace BstHelper.Bestiary;

public sealed record Landmark(string Name, uint Territory, string Zone, Vector3 World);

public static class Landmarks
{
    public static readonly Landmark Crucible =
        new("the Crucible", 148, "Central Shroud", new Vector3(26.91f, -6.00f, 60.96f));
}
