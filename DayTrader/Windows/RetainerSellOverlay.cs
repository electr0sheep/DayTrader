// How to glue a ImGui window to an in-game window adapted from https://github.com/Infiziert90/SubmarineTracker/blob/master/SubmarineTracker/Windows/Overlays/RouteOverlay.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using DayTrader;
using DayTrader.Models;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;

namespace Plugin.Windows;

public class RetainerSellOverlay : Window, IDisposable
{
    private readonly Plugin Plugin;

    private float width;

    private CurrentlyShownView? dcMarketData;
    private List<CurrentlyShownView> universalisData = new();
    private string? itemName;
    private uint itemId;
    private bool itemHq;
    private bool fetchingData = false;

    private const char HqSymbol = '';

    public RetainerSellOverlay(Plugin plugin) : base("DayTrader RetainerSell Overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        //Size = new Vector2(0, 0);
        //Flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize;
        //Flags |= ImGuiWindowFlags.NoScrollbar;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        IsOpen = true;

        Plugin = plugin;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    public unsafe override bool DrawConditions()
    {
        if (!Plugin.Configuration.Enabled)
        {
            return false;
        }

        if (Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedSummoningBell])
        {
            var addonPtr = Service.GameGui.GetAddonByName("RetainerSell");
            if (addonPtr == nint.Zero)
            {
                return false;
            }
            var baseNode = (AtkUnitBase*)addonPtr;
            if (baseNode->IsVisible && baseNode->UldManager.LoadedState == AtkLoadState.Loaded)
            {
                Position = new(baseNode->X - width, baseNode->Y);
                return true;
            }
        }
        return false;
    }

    public unsafe override void PreOpenCheck()
    {
        try
        {
            var addonPtr = Service.GameGui.GetAddonByName("RetainerSell");
            if (addonPtr == nint.Zero)
            {
                return;
            }

            var baseNode = (AtkUnitBase*)addonPtr;
            var addon = (AddonRetainerSell*)addonPtr;
            if (addon->ItemName != null)
            {
                var fullItemName = addon->ItemName->NodeText.ToString()[14..^10];
                itemHq = fullItemName.EndsWith(HqSymbol);
                var newItemName = itemHq ? fullItemName[..^2] : fullItemName;
                if (itemName != newItemName)
                {
                    itemName = newItemName;
                    itemId = Service.DataManager.GetExcelSheet<Item>()!.Where((i) => i.Name == itemName).First().RowId;
                    dcMarketData = null;
                    universalisData = new();
                    fetchingData = false;
                }

                //Size = new Vector2(Size!.Value.X * ImGuiHelpers.GlobalScaleSafe, baseNode->RootNode->Height * baseNode->Scale);
                //Size = new Vector2(300.0f, baseNode->RootNode->Height * baseNode->Scale);
                //Position = new Vector2(baseNode->X - (Size!.Value.X * ImGuiHelpers.GlobalScale), baseNode->Y);
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex.Message);
            Service.PluginLog.Error(ex.StackTrace!);
        }
    }

    public override void Draw()
    {
        Service.FontManager.H1.Push();
        ImGui.Text($"{itemName}{(itemHq ? $" {HqSymbol}" : "")}");
        Service.FontManager.H1.Pop();

        if (dcMarketData == null)
        {
            var dataCenter = Service.ClientState.LocalPlayer?.HomeWorld.GameData?.DataCenter?.Value;
            if (dataCenter == null)
            {
                return;
            }
            var worlds = Service.DataManager.GetExcelSheet<World>()!.Where((i) => i.DataCenter.Value?.RowId == dataCenter.RowId);
            if (!fetchingData)
            {
                fetchingData = true;
                Task.Run(async () =>
                {
                    if (Plugin.Configuration.RequestDataCenter)
                    {
                        dcMarketData = await UniversalisClient.GetItemInfo(itemId, dataCenter.Name, 10, CancellationToken.None);
                    }
                    if (Plugin.Configuration.RequestWorlds)
                    {
                        foreach (var world in worlds)
                        {
                            if (!world.IsPublic)
                            {
                                continue;
                            }
                            var marketData = await UniversalisClient.GetItemInfo(itemId, world.Name.RawString, 10, CancellationToken.None);
                            if (marketData.WorldID == Service.ClientState.LocalPlayer.HomeWorld.Id)
                            {
                                universalisData.Insert(0, marketData);
                                continue;
                            }
                            Service.PluginLog.Debug(world.Name.RawString);
                            Service.PluginLog.Debug(marketData.ToString());
                            universalisData.Add(marketData);
                        }
                    }
                });
            }
        }
        //if (ImGui.BeginChild("Glossary", ImGui.CalcTextSize("TEST"), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize))
        //{
        //    ImGui.Text("TEST");
        //    ImGui.CalcTextSize()
        //}
        //ImGui.EndChild();
        if (Plugin.Configuration.RequestRegion)
        {
            Service.FontManager.H2.Push();
            DayTrader.ImGuiExtensions.SeparatorText("Region");
            Service.FontManager.H2.Pop();
            ImGui.Text("TODO");
        }
        if (Plugin.Configuration.RequestDataCenter)
        {
            Service.FontManager.H2.Push();
            DayTrader.ImGuiExtensions.SeparatorText("Data Center");
            Service.FontManager.H2.Pop();
            Service.FontManager.H3.Push();
            ImGui.Text(Service.ClientState.LocalPlayer?.HomeWorld.GameData?.DataCenter?.Value?.Name!);
            Service.FontManager.H3.Pop();
            if (dcMarketData == null)
            {
                DayTrader.ImGuiExtensions.Spinner("DCSpinner", 10.0f, 2, ImGuiColors.TankBlue);
            }
            else
            {
                if (itemHq)
                {
                    ImGui.Text($"Sale Velocity: {dcMarketData.HqSaleVelocity}");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).\nThis number will tend to be the same for every item, because the number of shown sales is the same and over the same period.\nThis statistic is more useful in historical queries.");
                    }
                    ImGui.Text($"Current Average Price: {dcMarketData.AveragePriceHq}");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean.");
                    }
                }
                else
                {
                    ImGui.Text($"Sale Velocity: {dcMarketData.NqSaleVelocity}");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).\nThis number will tend to be the same for every item, because the number of shown sales is the same and over the same period.\nThis statistic is more useful in historical queries.");
                    }
                    ImGui.Text($"Current Average Price: {dcMarketData.AveragePriceNq}");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean.");
                    }
                }
                ImGui.Text($"Units for sale: {dcMarketData.UnitsForSale}");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("The number of items(not listings) up for sale.");
                }
                ImGui.Text($"Units sold: {dcMarketData.UnitsSold}");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("The number of items(not sale entries) sold over the retrieved sales.");
                }
            }
        }
        if (Plugin.Configuration.RequestWorlds)
        {
            Service.FontManager.H2.Push();
            DayTrader.ImGuiExtensions.SeparatorText("Worlds");
            Service.FontManager.H2.Pop();

            foreach (var world in universalisData)
            {
                Service.FontManager.H3.Push();
                DayTrader.ImGuiExtensions.SeparatorText(world.WorldName);
                Service.FontManager.H3.Pop();
                if (itemHq)
                {
                    ImGui.Text($"World Sale Velocity: {world.HqSaleVelocity}");
                    ImGui.Text($"Current Average Price: {world.AveragePriceHq}");
                }
                else
                {
                    ImGui.Text($"Sale Velocity: {world.NqSaleVelocity}");
                    ImGui.Text($"Current Average Price: {world.AveragePriceNq}");
                }
                ImGui.Text($"Units for sale: {world.UnitsForSale}");
                ImGui.Text($"Units sold: {world.UnitsSold}");
            }
        }
        width = ImGui.GetWindowSize().X;
    }
}
