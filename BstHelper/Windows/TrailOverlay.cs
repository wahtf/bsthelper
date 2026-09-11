using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using BstHelper.Bestiary;

namespace BstHelper.Windows;

public class TrailOverlay : Window, IDisposable
{
    private const float EdgeMargin = 48f;

    private static readonly Vector4 Trail = new(1.000f, 0.784f, 0.251f, 0.85f);
    private static readonly Vector4 Shadow = new(0.05f, 0.04f, 0.02f, 0.65f);

    private readonly Plugin plugin;
    private readonly BestiaryWindow bestiary;

    public TrailOverlay(Plugin plugin, BestiaryWindow bestiary)
        : base("##BstHelperTrail",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoDocking)
    {
        this.plugin = plugin;
        this.bestiary = bestiary;

        IsOpen = true;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ForceMainWindow = true;

        Size = Vector2.One;
        SizeCondition = ImGuiCond.Always;
        Position = Vector2.Zero;
        PositionCondition = ImGuiCond.Always;
    }

    public void Dispose() { }

    public override bool DrawConditions()
        => Plugin.Configuration.ShowTrail && Plugin.ClientState.IsLoggedIn && Subject() != null;

    public override void PreDraw() => ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

    public override void PostDraw() => ImGui.PopStyleVar();

    public override void Draw()
    {
        if (Subject() is not { } beast)
            return;

        if (Plugin.ObjectTable.LocalPlayer is not { } me)
            return;

        if (!Plugin.GameGui.WorldToScreen(me.Position, out var from))
            return;

        var draw = ImGui.GetBackgroundDrawList();
        var scale = ImGuiHelpers.GlobalScale;

        foreach (var mob in Mobs.Nearby(beast))
            Line(draw, from, mob.Position, me.Position, beast.Mob, scale);
    }

    private Beast? Subject()
    {
        if (plugin.Trip.State != ETrip.Idle && plugin.Trip.Target is { IsOverworld: true } target)
            return target;

        var selected = bestiary.Selected;
        if (selected == 0)
            return null;

        var beast = BeastTable.ByNumber(selected);
        return beast.IsOverworld ? beast : null;
    }

    private static void Line(ImDrawListPtr draw, Vector2 from, Vector3 world, Vector3 me, string label,
        float scale)
    {
        var onScreen = Plugin.GameGui.WorldToScreen(world, out var to);

        if (!onScreen)
            to = from + (from - to);

        var display = ImGui.GetIO().DisplaySize;
        var margin = EdgeMargin * scale;
        var pinned = PinToScreen(from, to, new Vector2(margin), display - new Vector2(margin));

        var tint = ImGui.ColorConvertFloat4ToU32(Trail);
        var shadow = ImGui.ColorConvertFloat4ToU32(Shadow);

        draw.AddLine(from, pinned, shadow, 4f * scale);
        draw.AddLine(from, pinned, tint, 2f * scale);

        var radius = 5f * scale;
        draw.AddCircleFilled(pinned, radius, tint);
        draw.AddCircle(pinned, radius + 1.5f * scale, shadow, 0, 1.5f * scale);

        var caption = $"{label}  {Distance(me, world):0}y";
        var size = ImGui.CalcTextSize(caption);
        var at = pinned - new Vector2(size.X / 2f, size.Y + radius + 4f * scale);
        draw.AddText(at + new Vector2(1f, 1f), shadow, caption);
        draw.AddText(at, tint, caption);
    }

    private static float Distance(Vector3 me, Vector3 world)
        => new Vector2(world.X - me.X, world.Z - me.Z).Length();

    private static Vector2 PinToScreen(Vector2 from, Vector2 to, Vector2 min, Vector2 max)
    {
        if (to.X >= min.X && to.X <= max.X && to.Y >= min.Y && to.Y <= max.Y)
            return to;

        var direction = to - from;
        var cut = 1f;

        Clip(direction.X, min.X - from.X, max.X - from.X, ref cut);
        Clip(direction.Y, min.Y - from.Y, max.Y - from.Y, ref cut);

        return from + direction * Math.Clamp(cut, 0f, 1f);
    }

    private static void Clip(float delta, float lower, float upper, ref float cut)
    {
        if (Math.Abs(delta) < 0.0001f)
            return;

        var edge = delta < 0f ? lower : upper;
        cut = Math.Min(cut, edge / delta);
    }
}
