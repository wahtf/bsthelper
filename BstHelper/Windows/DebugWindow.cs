using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using BstHelper.Bestiary;
using BstHelper.Travel;

namespace BstHelper.Windows;

public class DebugWindow : Window, IDisposable
{
    public DebugWindow()
        : base("BST Debug###BstHelperDebug", ImGuiWindowFlags.AlwaysAutoResize)
    {
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (Plugin.ObjectTable.LocalPlayer is not { } me)
        {
            ImGui.TextUnformatted("Not in the world.");
            return;
        }

        var at = me.Position;
        var territory = Plugin.ClientState.TerritoryType;
        var map = MapCoords.ToMap(territory, at);

        ImGui.TextUnformatted($"{GameData.GetZoneName(territory)}  ({territory})");
        ImGui.TextUnformatted($"map  {map.X:0.0}, {map.Y:0.0}");
        ImGui.Separator();

        var world = $"{at.X:0.00}, {at.Y:0.00}, {at.Z:0.00}";
        var vector = $"new Vector3({at.X:0.00}f, {at.Y:0.00}f, {at.Z:0.00}f)";

        ImGui.TextUnformatted($"X  {at.X,10:0.00}");
        ImGui.TextUnformatted($"Y  {at.Y,10:0.00}");
        ImGui.TextUnformatted($"Z  {at.Z,10:0.00}");
        ImGui.Separator();

        ImGui.TextUnformatted(world);
        ImGui.SameLine();
        if (ImGui.Button("Copy##world"))
            ImGui.SetClipboardText(world);

        ImGui.TextUnformatted(vector);
        ImGui.SameLine();
        if (ImGui.Button("Copy##vector"))
            ImGui.SetClipboardText(vector);
    }
}
