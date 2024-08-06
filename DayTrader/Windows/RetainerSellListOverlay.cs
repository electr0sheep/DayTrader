using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using DayTrader;
using ImPlotNET;

namespace Plugin.Windows;
internal unsafe class RetainerSellListOverlay : Window, IDisposable
{
    private float height;
    private readonly Plugin Plugin;
    public RetainerSellListOverlay(Plugin plugin) : base("DayTrader RetainerSellList Overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        RespectCloseHotkey = false;
        IsOpen = true;

        Plugin = plugin;
    }

    public override bool DrawConditions()
    {
        if (Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedSummoningBell])
        {
            var addonPtr = Service.GameGui.GetAddonByName("RetainerSellList");
            if (addonPtr == nint.Zero)
            {
                return false;
            }
            var baseNode = (AtkUnitBase*)addonPtr;
            if (baseNode->IsVisible && baseNode->UldManager.LoadedState == AtkLoadState.Loaded)
            {
                Position = new(baseNode->X, baseNode->Y - height);
                return true;
            }
        }
        return false;
    }

    public override void Draw()
    {
        var enabled = Plugin.Configuration.Enabled;
        var requestRegion = Plugin.Configuration.RequestRegion;
        var requestDataCenter = Plugin.Configuration.RequestDataCenter;
        var requestWorlds = Plugin.Configuration.RequestWorlds;

        if (ImGui.Checkbox("Enable DayTrader", ref enabled))
        {
            Plugin.Configuration.Enabled = enabled;
            Plugin.Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Request Region", ref requestRegion))
        {
            Plugin.Configuration.RequestRegion = requestRegion;
            Plugin.Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Request Data Center", ref requestDataCenter))
        {
            Plugin.Configuration.RequestDataCenter = requestDataCenter;
            Plugin.Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Request Worlds", ref requestWorlds))
        {
            Plugin.Configuration.RequestWorlds = requestWorlds;
            Plugin.Configuration.Save();
        }
        height = ImGui.GetWindowSize().Y;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
