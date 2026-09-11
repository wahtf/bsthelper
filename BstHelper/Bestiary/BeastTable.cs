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
        Field(2, "Squirrel", "ground squirrel", 2, CentralShroud, "Central Shroud", 22, 17),
        Field(3, "Lamb", "lost lamb", 3, MiddleLaNoscea, "Middle La Noscea", 23, 24),
        Field(4, "Pugil", "pugil", 4, MiddleLaNoscea, "Middle La Noscea", 23, 22),
        Field(5, "Opo-opo", "opo-opo", 9, NorthShroud, "North Shroud", 27, 23),
        Field(6, "Dodo", "wild dodo", 7, LowerLaNoscea, "Lower La Noscea", 27, 21),
        Field(7, "Coblyn", "rusty coblyn", 8, WesternThanalan, "Western Thanalan", 22, 27),
        Field(8, "Diremite", "diremite", 10, CentralShroud, "Central Shroud", 19, 18),
        Field(9, "Megalocrab", "megalocrab", 13, MiddleLaNoscea, "Middle La Noscea", 15, 14),
        Field(10, "Wespe", "huge hornet", 1, CentralThanalan, "Central Thanalan", 20, 26),
        Field(11, "Vulture", "nesting buzzard", 6, WesternThanalan, "Western Thanalan", 21, 25),
        Field(12, "Mandragora", "tiny mandragora", 7, MiddleLaNoscea, "Middle La Noscea", 22, 17),
        Field(13, "Geshunpest", "geshunpest", 14, CentralShroud, "Central Shroud", 19, 28),
        Field(14, "Puk", "puk hatchling", 8, MiddleLaNoscea, "Middle La Noscea", 19, 19),
        Field(15, "Crab", "thickshell", 13, WesternThanalan, "Western Thanalan", 15, 17),
        Field(16, "Mantis", "killer mantis", 16, WesternLaNoscea, "Western La Noscea", 21, 22),
        Instanced(17, "Slime", "Ichorous Ire", 17, "Copperbell Mines", "second boss"),
        Instanced(18, "Dullahan", "Doctore", 20, "Halatali", "boss"),
        Field(19, "Bat", "cave bat", 7, LowerLaNoscea, "Lower La Noscea", 26, 15),
        Field(20, "Flying Trap", "roselet", 10, CentralShroud, "Central Shroud", 22, 25),
        Field(21, "Ziz", "Rothlyt pelican", 16, WesternLaNoscea, "Western La Noscea", 24, 22),
        Field(22, "Sabotender", "cactuar", 4, WesternThanalan, "Western Thanalan", 26, 23),
        Field(23, "Golem", "sandstone golem", 29, SouthernThanalan, "Southern Thanalan", 32, 12),
        Field(24, "Apkallu", "apkallu", 30, EasternLaNoscea, "Eastern La Noscea", 28, 35),
        Field(25, "Adamantoise", "giant tortoise", 12, CentralThanalan, "Central Thanalan", 19, 26),
        Field(26, "Buffalo", "wounded aurochs", 8, MiddleLaNoscea, "Middle La Noscea", 18, 17),
        Field(27, "Uragnite", "scaphite", 14, WesternThanalan, "Western Thanalan", 17, 14),
        Field(28, "Worm", "sandworm", 32, SouthernThanalan, "Southern Thanalan", 23, 32),
        Field(29, "Spriggan", "spriggan graverobber", 7, CentralThanalan, "Central Thanalan", 17, 23),
        Field(30, "Goobbue", "mossless goobbue", 17, LowerLaNoscea, "Lower La Noscea", 28, 19),
        Field(31, "Gigantoad", "rivertoad", 4, LowerLaNoscea, "Lower La Noscea", 24, 22),
        Field(32, "Colibri", "colibri", 33, EasternLaNoscea, "Eastern La Noscea", 29, 24),
        Field(33, "Coeurl", "coeurl", 34, OuterLaNoscea, "Outer La Noscea", 14, 14),
        Field(34, "Raptor", "anole", 9, CentralShroud, "Central Shroud", 31, 20),
        Field(35, "Drake", "sundrake", 32, SouthernThanalan, "Southern Thanalan", 25, 38),
        Field(36, "Treant", "treant sapling", 12, CentralShroud, "Central Shroud", 24, 18),
        Instanced(37, "Antling", "Myrmidon Princess", 38, "Cutter's Cry", "first boss"),
        Instanced(38, "Chimera", "Chimera", 38, "Cutter's Cry", "final boss"),
        Field(39, "Morbol", "halitostroper", 31, CentralShroud, "Central Shroud", 15, 21),
        Field(40, "Ghost", "bogy", 7, MiddleLaNoscea, "Middle La Noscea", 20, 18,
            "Underground: the route ends above the cave, the bogies are inside it."),
        Field(41, "Salamander", "black eft", 6, CentralShroud, "Central Shroud", 26, 18),
        Field(42, "Cobra", "lake cobra", 45, MorDhona, "Mor Dhona", 26, 13),
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
        float mapX, float mapY, string note = "")
        => new(number, name, mob, level, territory, zone, mapX, mapY, Note: note);

    private static Beast Instanced(uint number, string name, string mob, int level, string duty, string role,
        string note = "")
        => new(number, name, mob, level, 0, string.Empty, 0, 0, duty, role, note);

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
