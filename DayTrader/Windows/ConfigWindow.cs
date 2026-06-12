using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Plugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base(
        "A Wonderful Configuration Window",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(420, 80);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var autoCycle = this.Configuration.AutoCycleSaleHistoryEnabled;
        if (ImGui.Checkbox("Auto-fetch sale history during AutoRetainer cycles", ref autoCycle))
        {
            this.Configuration.AutoCycleSaleHistoryEnabled = autoCycle;
            this.Configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Requires AutoRetainer. Whenever AR processes a retainer (single-character or multi-mode), DayTrader opens that retainer's sale history so its CSV gets refreshed without manual clicks.");
    }
}
