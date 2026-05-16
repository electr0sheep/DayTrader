using Dalamud.Interface.Windowing;
using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using DayTrader;
using Dalamud.Bindings.ImGui;

namespace Plugin.Windows;
internal unsafe class RetainerSellListOverlay : Window, IDisposable
{
    private float height;
    private readonly Plugin plugin;
    public RetainerSellListOverlay(Plugin plugin) : base("DayTrader RetainerSellList Overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        RespectCloseHotkey = false;
        IsOpen = true;

        this.plugin = plugin;
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
            var baseNode = (AtkUnitBase*)addonPtr.Address;
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
        var enabled = plugin.Configuration.Enabled;

        if (ImGui.Checkbox("Enable DayTrader", ref enabled))
        {
            plugin.Configuration.Enabled = enabled;
            plugin.Configuration.Save();
            plugin.RetainerSellListColumn.Refresh();
        }
        height = ImGui.GetWindowSize().Y;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
