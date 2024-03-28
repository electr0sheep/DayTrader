using System;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using DayTrader;
using ImGuiNET;

namespace Plugin.Windows;

internal class HelpWindow : Window, IDisposable
{
    public HelpWindow(Plugin plugin) : base("Day Trader Help")
    {
        Size = new Vector2(0, 0);
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
        SizeCondition = ImGuiCond.Always;
    }

    public void Dispose() { }

    public override void Draw()
    {
        Service.FontManager.H2.Push();
        DayTrader.ImGuiExtensions.SeparatorText("Glossary of Terms");
        Service.FontManager.H2.Pop();
        ImGui.NewLine();
        ImGuiComponents.HelpMarker(@"The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
This statistic is more useful in historical queries.");
        ImGui.SameLine();
        ImGui.Text("Sale Velocity");
    }
}
