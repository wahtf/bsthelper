using System;
using System.Collections.Generic;
using System.Linq;

namespace BstHelper.Bestiary;

public static class BeastTable
{
    private const uint CentralShroud = 148;
    private const uint NorthShroud = 154;
    private const uint MiddleLaNoscea = 134;
    private const uint LowerLaNoscea = 135;
    private const uint EasternLaNoscea = 137;
    private const uint WesternLaNoscea = 138;
    private const uint OuterLaNoscea = 180;
    private const uint WesternThanalan = 140;
    private const uint CentralThanalan = 141;
    private const uint SouthernThanalan = 146;
    private const uint MorDhona = 156;

    public const int Total = 50;

    public static readonly IReadOnlyList<Beast> All = new[]
    {
        Instanced(1, "Cu Sith", "-", 1, "the Beastmaster unlock quest", "quest reward",
            "Given when you take up the job; nothing to hunt."),
        Field(2, "Squirrel", "ground squirrel", 2, CentralShroud, "Central Shroud", 64.64f, 4.93f, -218.82f),
        Field(3, "Lamb", "lost lamb", 3, MiddleLaNoscea, "Middle La Noscea", 74.95f, 46.88f, 124.84f),
        Field(4, "Pugil", "pugil", 4, MiddleLaNoscea, "Middle La Noscea", 68.19f, 42.42f, 32.76f),
        Field(5, "Opo-opo", "opo-opo", 9, NorthShroud, "North Shroud", 282.48f, -10.54f, 77.51f),
        Field(6, "Dodo", "wild dodo", 7, LowerLaNoscea, "Lower La Noscea", 329.09f, 48.38f, -18.37f),
        Field(7, "Coblyn", "rusty coblyn", 8, WesternThanalan, "Western Thanalan", 24.95f, 52.48f, 274.47f),
        Field(8, "Diremite", "diremite", 10, CentralShroud, "Central Shroud", -96.53f, 3.68f, -159.78f),
        Field(9, "Megalocrab", "megalocrab", 13, MiddleLaNoscea, "Middle La Noscea", -324.79f, 18.46f, -374.63f),
        Field(10, "Wespe", "huge hornet", 1, CentralThanalan, "Central Thanalan", -74.91f, 0.75f, 219.74f),
        Field(11, "Vulture", "nesting buzzard", 6, WesternThanalan, "Western Thanalan", -37.67f, 51.44f, 240.60f),
        Field(12, "Mandragora", "tiny mandragora", 7, MiddleLaNoscea, "Middle La Noscea", 47.83f, 63.77f, -193.16f),
        Field(13, "Geshunpest", "geshunpest", 14, CentralShroud, "Central Shroud", -122.83f, -30.23f, 334.24f),
        Field(14, "Puk", "puk hatchling", 8, MiddleLaNoscea, "Middle La Noscea", -145.08f, 46.38f, -153.61f),
        Field(15, "Crab", "thickshell", 13, WesternThanalan, "Western Thanalan", -271.60f, 15.46f, -238.53f),
        Field(16, "Mantis", "killer mantis", 16, WesternLaNoscea, "Western La Noscea", 14.69f, -22.92f, 83.94f),
        Instanced(17, "Slime", "Ichorous Ire", 17, "Copperbell Mines", "second boss"),
        Instanced(18, "Dullahan", "Doctore", 20, "Halatali", "boss"),
        Field(19, "Bat", "cave bat", 7, LowerLaNoscea, "Lower La Noscea", 275.95f, 71.15f, -209.62f),
        Field(20, "Flying Trap", "roselet", 10, CentralShroud, "Central Shroud", 58.61f, -17.29f, 179.34f),
        Field(21, "Ziz", "Rothlyt pelican", 16, WesternLaNoscea, "Western La Noscea", 110.53f, -17.09f, 59.45f),
        Field(22, "Sabotender", "cactuar", 4, WesternThanalan, "Western Thanalan", 262.02f, 52.86f, 148.49f),
        Field(23, "Golem", "sandstone golem", 29, SouthernThanalan, "Southern Thanalan", 15.04f, 3.07f, -453.10f),
        Field(24, "Apkallu", "apkallu", 30, EasternLaNoscea, "Eastern La Noscea", 354.88f, 27.54f, 709.51f),
        Field(25, "Adamantoise", "giant tortoise", 12, CentralThanalan, "Central Thanalan", -34.46f, -2.57f, -128.37f),
        Field(26, "Buffalo", "wounded aurochs", 8, MiddleLaNoscea, "Middle La Noscea", -181.49f, 42.51f, -207.21f),
        Field(27, "Uragnite", "scaphite", 14, WesternThanalan, "Western Thanalan", -234.92f, 15.88f, -302.41f),
        Field(28, "Worm", "sandworm", 32, SouthernThanalan, "Southern Thanalan", 51.59f, 27.27f, 535.91f),
        Field(29, "Spriggan", "spriggan graverobber", 7, CentralThanalan, "Central Thanalan", -190.91f, -28.36f, 80.19f),
        Field(30, "Goobbue", "mossless goobbue", 17, LowerLaNoscea, "Lower La Noscea", 198.75f, 59.04f, 281.03f),
        Field(31, "Gigantoad", "rivertoad", 4, LowerLaNoscea, "Lower La Noscea", 146.34f, 36.52f, 108.74f),
        Field(32, "Colibri", "colibri", 33, EasternLaNoscea, "Eastern La Noscea", 376.64f, 34.00f, 161.93f),
        Field(33, "Coeurl", "coeurl", 34, OuterLaNoscea, "Outer La Noscea", -343.34f, 62.05f, -333.52f),
        Field(34, "Raptor", "anole", 9, CentralShroud, "Central Shroud", 464.15f, 20.49f, -61.85f),
        Field(35, "Drake", "sundrake", 32, SouthernThanalan, "Southern Thanalan", 117.93f, 9.34f, 784.46f),
        Field(36, "Treant", "treant sapling", 12, CentralShroud, "Central Shroud", 180.57f, -0.92f, -149.15f),
        Instanced(37, "Antling", "Myrmidon Princess", 38, "Cutter's Cry", "first boss"),
        Instanced(38, "Chimera", "Chimera", 38, "Cutter's Cry", "final boss"),
        Field(39, "Morbol", "halitostroper", 31, CentralShroud, "Central Shroud", -359.02f, 54.64f, 3.43f),
        Field(40, "Ghost", "bogy", 7, MiddleLaNoscea, "Middle La Noscea", -57.03f, 34.96f, -80.25f,
            "Underground: the route ends at the cave mouth, the bogies are inside it."),
        Field(41, "Salamander", "black eft", 6, CentralShroud, "Central Shroud", 233.24f, -5.64f, -176.65f),
        Field(42, "Cobra", "lake cobra", 45, MorDhona, "Mor Dhona", 221.04f, -20.85f, -453.94f),
        Instanced(43, "Hydra", "Hydra", 50, "A Relic Reborn: the Hydra", "trial boss"),
        Instanced(44, "Damselfly", "gadfly", 50, "the Lost City of Amdapor", "trash mob",
            "Gadflies come in several packs; the first is soon after you enter."),
        Instanced(45, "Rotting Goobbue", "Decaying Gourmand", 50, "the Lost City of Amdapor", "first boss"),
        Instanced(46, "Zu", "Zu", 50, "Pharos Sirius", "second boss",
            "Also reported from the level 50 FATE \"Anzu Trois\" in the Sea of Clouds."),
        Instanced(47, "Ice Golem", "Wandil", 50, "Snowcloak", "first boss",
            "Also reported from the level 51 FATE \"In Command\" in Coerthas Western Highlands."),
        Instanced(48, "Karlabos", "Karlabos", 50, "Sastasha (Hard)", "first boss"),
        Instanced(49, "Rafflesia", "Rafflesia", 50, "the Second Coil of Bahamut - Turn 1", "raid boss"),
        Instanced(50, "Behemoth", "King Behemoth", 50, "the Labyrinth of the Ancients", "third raid boss"),
    };

    private static Beast Field(uint number, string name, string mob, int level, uint territory, string zone,
        float x, float y, float z, string note = "")
        => new(number, name, mob, level, territory, zone, x, y, z, Note: note);

    private static Beast Instanced(uint number, string name, string mob, int level, string duty, string role,
        string note = "")
        => new(number, name, mob, level, 0, string.Empty, 0, 0, 0, duty, role, note);

    public static Beast ByNumber(uint number) => All.First(x => x.Number == number);

    public static Beast? Find(string query)
    {
        query = query.Trim();
        if (query.Length == 0)
            return null;

        var digits = new string(query.Where(char.IsDigit).ToArray());
        if (digits.Length == query.Trim(' ', '#', '.', 'N', 'n', 'o', 'O').Length &&
            uint.TryParse(digits, out var number))
        {
            return All.FirstOrDefault(x => x.Number == number);
        }

        return All.FirstOrDefault(x => string.Equals(x.Name, query, StringComparison.OrdinalIgnoreCase))
               ?? All.FirstOrDefault(x => string.Equals(x.Mob, query, StringComparison.OrdinalIgnoreCase))
               ?? All.FirstOrDefault(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
               ?? All.FirstOrDefault(x => x.Mob.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
