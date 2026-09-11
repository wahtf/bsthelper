using System;
using System.Numerics;

namespace BstHelper.Bestiary;

public sealed record Beast(
    uint Number,
    string Name,
    string Mob,
    int Level,
    uint Territory,
    string Zone,
    float MapX,
    float MapY,
    string Duty = "",
    string DutyRole = "",
    string Note = "")
{
    private const uint IconBase = 242000;

    public bool IsOverworld => Territory != 0;

    public string Where => IsOverworld ? $"{Zone} ({MapX:0.#}, {MapY:0.#})" : Duty;

    public string Summary
    {
        get
        {
            if (Mob == "-")
                return Where;

            var where = IsOverworld || DutyRole.Length == 0 ? Where : $"{Where}, {DutyRole}";
            return $"{Mob} · Lv {Level} · {where}";
        }
    }

    public Vector3 World => MapCoords.ToWorld(Territory, MapX, MapY);

    public string Label => $"No. {Number}";

    public uint Icon => IconBase + Number;
}
