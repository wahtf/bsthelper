using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using BstHelper.Bestiary;
using BstHelper.Travel;

namespace BstHelper.Windows;

public class BestiaryWindow : Window, IDisposable
{
    private const int Columns = 5;
    private const int PerPage = 25;
    private const int Pages = 2;

    private static readonly Vector4 Parchment = new(0.847f, 0.788f, 0.667f, 1f);
    private static readonly Vector4 Slot = new(0.906f, 0.871f, 0.776f, 1f);
    private static readonly Vector4 SlotEmpty = new(0.804f, 0.765f, 0.678f, 1f);
    private static readonly Vector4 Edge = new(0.596f, 0.510f, 0.353f, 1f);
    private static readonly Vector4 Ink = new(0.243f, 0.180f, 0.110f, 1f);
    private static readonly Vector4 InkFaint = new(0.435f, 0.373f, 0.286f, 1f);
    private static readonly Vector4 Catchable = new(1.000f, 0.784f, 0.251f, 1f);
    private static readonly Vector4 Locked = new(0.549f, 0.353f, 0.318f, 1f);
    private static readonly Vector4 Amber = new(0.545f, 0.400f, 0.000f, 1f);

    private readonly Plugin plugin;

    private int page;
    private uint selected;

    public uint Selected => selected;

    public BestiaryWindow(Plugin plugin)
        : base("BST Helper  (/bst)###BstHelperBestiary",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.plugin = plugin;
        page = Math.Clamp(Plugin.Configuration.Page, 0, Pages - 1);
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Parchment);
        ImGui.PushStyleColor(ImGuiCol.TitleBg, Edge);
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, Edge);
        ImGui.PushStyleColor(ImGuiCol.Text, Ink);
        ImGui.PushStyleColor(ImGuiCol.Border, Edge);
        ImGui.PushStyleColor(ImGuiCol.Button, Slot);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.949f, 0.910f, 0.796f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Catchable);
        ImGui.PushStyleColor(ImGuiCol.CheckMark, Ink);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, SlotEmpty);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Slot);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Slot);
        ImGui.PushStyleColor(ImGuiCol.Separator, Edge);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.804f, 0.745f, 0.627f, 1f));
    }

    public override void PostDraw() => ImGui.PopStyleColor(14);

    public override void Draw()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var tile = 54f * scale;
        var gap = 6f * scale;
        var caption = ImGui.GetTextLineHeight() + 4f * scale;
        var width = Columns * tile + (Columns - 1) * gap;

        DrawPageBar(scale, width);
        ImGuiHelpers.ScaledDummy(2f);
        DrawGrid(scale, tile, gap, caption);
        ImGuiHelpers.ScaledDummy(2f);
        DrawBottom(scale, width);
    }

    private void DrawBottom(float scale, float width)
    {
        ImGui.Separator();
        ImGui.Spacing();

        var left = ImGui.GetCursorPosX();

        DrawTally(left, width);

        var missing = Dependencies.Missing(Dependencies.Lifestream, Dependencies.Navmesh);
        if (missing.Count > 0)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Locked))
            using (Wrapped(left, width))
                ImGui.TextUnformatted($"{string.Join(" and ", missing)} {(missing.Count == 1 ? "is" : "are")} "
                                      + "not running; nothing can travel.");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (selected == 0)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, InkFaint))
                ImGui.TextUnformatted("Pick a slot.");

            return;
        }

        DrawSelected(BeastTable.ByNumber(selected), scale, left, width);
    }

    private void DrawPageBar(float scale, float width)
    {
        var left = ImGui.GetCursorPosX();

        for (var index = 0; index < Pages; index++)
        {
            if (index > 0)
                ImGui.SameLine();

            var active = index == page;
            using var colour = ImRaii.PushColor(ImGuiCol.Button, active ? Catchable : SlotEmpty)
                .Push(ImGuiCol.ButtonHovered, active ? Catchable : Slot);

            if (ImGui.Button($"{index + 1}##page{index}", new Vector2(26 * scale, 22 * scale)))
            {
                page = index;
                Plugin.Configuration.Page = page;
                Plugin.Configuration.Save();
            }
        }

        const string lineLabel = "Line";
        const string catchLabel = "Catchable only";

        var frame = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X;
        var lineBox = ImGui.CalcTextSize(lineLabel).X + frame;
        var catchBox = ImGui.CalcTextSize(catchLabel).X + frame;

        var trail = Plugin.Configuration.ShowTrail;
        ImGui.SameLine(left + width - catchBox - lineBox - ImGui.GetStyle().ItemSpacing.X);
        if (ImGui.Checkbox(lineLabel, ref trail))
        {
            Plugin.Configuration.ShowTrail = trail;
            Plugin.Configuration.Save();
        }

        var onlyCatchable = Plugin.Configuration.OnlyShowCatchable;
        ImGui.SameLine();
        if (ImGui.Checkbox(catchLabel, ref onlyCatchable))
        {
            Plugin.Configuration.OnlyShowCatchable = onlyCatchable;
            Plugin.Configuration.Save();
        }
    }

    private void DrawGrid(float scale, float tile, float gap, float caption)
    {
        using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(gap, gap));

        var first = page * PerPage;
        for (var offset = 0; offset < PerPage; offset++)
        {
            if (offset % Columns != 0)
                ImGui.SameLine(0, gap);

            DrawSlot(BeastTable.All[first + offset], tile, caption, scale);
        }
    }

    private void DrawSlot(Beast beast, float tile, float caption, float scale)
    {
        var captured = CaptureState.IsCaptured(beast.Number);
        var catchable = CaptureState.IsCatchableNow(beast);
        var dimmed = Plugin.Configuration.OnlyShowCatchable && !catchable;

        using var group = ImRaii.Group();

        var origin = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton($"##slot{beast.Number}", new Vector2(tile, tile));
        if (ImGui.IsItemClicked())
            selected = selected == beast.Number ? 0u : beast.Number;

        var draw = ImGui.GetWindowDrawList();
        var corner = origin + new Vector2(tile, tile);
        var rounding = 4f * scale;
        var alpha = dimmed ? 0.35f : 1f;

        draw.AddRectFilled(origin, corner, Tint(captured ? Slot : SlotEmpty, alpha), rounding);

        var texture = captured
            ? Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(beast.Icon)).GetWrapOrDefault()
            : null;
        if (texture != null)
        {
            var inset = 4f * scale;
            draw.AddImage(texture.Handle, origin + new Vector2(inset, inset), corner - new Vector2(inset, inset),
                Vector2.Zero, Vector2.One, Tint(Vector4.One, alpha));
        }
        else
        {
            DrawGlyph(draw, origin, tile, captured ? beast.Name[..1] : "?", Tint(InkFaint, alpha));
        }

        if (catchable)
        {
            draw.AddRect(origin - Vector2.One, corner + Vector2.One, Tint(Catchable, 1f), rounding,
                ImDrawFlags.None, 3f * scale);
        }
        else if (!captured && CaptureState.Ready && beast.Level > CaptureState.PlayerLevel)
        {
            draw.AddRect(origin, corner, Tint(Locked, alpha * 0.8f), rounding, ImDrawFlags.None, 1f * scale);
        }
        else
        {
            draw.AddRect(origin, corner, Tint(Edge, alpha), rounding, ImDrawFlags.None, 1f * scale);
        }

        if (selected == beast.Number)
            draw.AddRect(origin - new Vector2(3f, 3f), corner + new Vector2(3f, 3f), Tint(Ink, 1f), rounding,
                ImDrawFlags.None, 2f * scale);

        var labelWidth = ImGui.CalcTextSize(beast.Label).X;
        draw.AddText(origin + new Vector2(MathF.Round((tile - labelWidth) / 2f), tile + 2f * scale),
            Tint(captured ? Ink : InkFaint, alpha), beast.Label);

        ImGui.SetCursorScreenPos(origin + new Vector2(0f, tile));
        ImGui.Dummy(new Vector2(tile, caption));
    }

    private static void DrawTally(float left, float width)
    {
        var captured = CaptureState.Ready ? CaptureState.CapturedCount : 0;
        var missing = BeastTable.All.Count(CaptureState.IsCatchableNow);

        ImGui.TextUnformatted("Beasts Captured");

        var count = CaptureState.Ready ? $"{captured}/{BeastTable.Total}" : $"-/{BeastTable.Total}";
        ImGui.SameLine(left + width - ImGui.CalcTextSize(count).X);
        ImGui.TextUnformatted(count);

        using (ImRaii.PushColor(ImGuiCol.Text, missing > 0 ? Amber : InkFaint))
        using (Wrapped(left, width))
        {
            ImGui.TextUnformatted(!CaptureState.Ready
                ? "Log in and open the bestiary once so the game sends the list."
                : captured == BeastTable.Total
                    ? "All fifty captured."
                    : missing == 0
                        ? "Nothing capturable right now."
                        : $"{missing} missing {(missing == 1 ? "is" : "are")} capturable.");
        }
    }

    private void DrawSelected(Beast beast, float scale, float left, float width)
    {
        ImGui.TextUnformatted($"No. {beast.Number}  {beast.Name}");

        using (ImRaii.PushColor(ImGuiCol.Text, InkFaint))
        using (Wrapped(left, width))
        {
            ImGui.TextUnformatted(beast.Summary);

            if (beast.Note.Length > 0)
                ImGui.TextUnformatted(beast.Note);
        }

        if (!beast.IsOverworld)
            return;

        ImGui.Spacing();

        var trip = plugin.Trip;
        var mine = trip.Target?.Number == beast.Number;
        string? blocker = null;

        if (mine && trip.IsRunning)
        {
            if (ImGui.Button("Stop", new Vector2(90 * scale, 0)))
                trip.Stop("Asked to stop");
        }
        else
        {
            blocker = BeastTrip.Blocker(beast);
            using (ImRaii.Disabled(blocker != null))
            {
                if (ImGui.Button("Travel", new Vector2(90 * scale, 0)))
                    trip.Start(beast);
            }
        }

        if (mine && trip.State != ETrip.Idle)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, trip.State == ETrip.Failed ? Locked : InkFaint))
            using (Wrapped(left, width))
                ImGui.TextUnformatted(trip.Status);
        }
        else if (blocker != null)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, InkFaint))
            using (Wrapped(left, width))
                ImGui.TextUnformatted(blocker);
        }
        else if (Plugin.ClientState.TerritoryType == beast.Territory)
        {
            var seen = Mobs.Nearby(beast).Count;
            using (ImRaii.PushColor(ImGuiCol.Text, seen > 0 ? Amber : InkFaint))
                ImGui.TextUnformatted(seen > 0
                    ? $"{seen} in range."
                    : "None in range right now.");
        }
    }

    private static IDisposable Wrapped(float left, float width)
    {
        ImGui.PushTextWrapPos(left + width);
        return new WrapScope();
    }

    private sealed class WrapScope : IDisposable
    {
        public void Dispose() => ImGui.PopTextWrapPos();
    }

    private static void DrawGlyph(ImDrawListPtr draw, Vector2 origin, float tile, string glyph, uint colour)
    {
        var font = ImGui.GetFont();
        var size = ImGui.GetFontSize() * 1.9f;
        var measured = ImGui.CalcTextSize(glyph) * (size / ImGui.GetFontSize());
        draw.AddText(font, size, origin + (new Vector2(tile, tile) - measured) / 2f, colour, glyph);
    }

    private static uint Tint(Vector4 colour, float alpha)
        => ImGui.ColorConvertFloat4ToU32(Tint4(colour, alpha));

    private static Vector4 Tint4(Vector4 colour, float alpha) => colour with { W = colour.W * alpha };
}
