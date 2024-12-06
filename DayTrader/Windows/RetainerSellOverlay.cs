// How to glue a ImGui window to an in-game window adapted from https://github.com/Infiziert90/SubmarineTracker/blob/master/SubmarineTracker/Windows/Overlays/RouteOverlay.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using DayTrader;
using DayTrader.FileHelpers;
using DayTrader.Models;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImPlotNET;
using Lumina.Excel.Sheets;

namespace Plugin.Windows;

public class RetainerSellOverlay : Window, IDisposable
{
    private readonly Plugin plugin;

    private float width;

    private CurrentlyShownView? dcMarketData;
    private List<CurrentlyShownView> worldMarketData = new();
    private string? itemName;
    private uint itemId;
    private bool itemHq;
    private bool fetchingDataCenterData = false;
    private bool requestError = false;

    private const char HqSymbol = '';
    private const char GilSymbol = '';

    private PlotPoints plotPoints = new();
    private float totalEarnings = 0.0f;
    private uint unitsSold = 0;
    private DateTime earliestSale = DateTime.MaxValue;
    private DateTime latestSale = DateTime.MinValue;
    private int totalNumberOfDays = 0;

    public RetainerSellOverlay(Plugin plugin) : base("DayTrader RetainerSell Overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        //plotPoints.Add(DateTimeOffset.UtcNow, 1000f);
        //plotPoints.Add(DateTimeOffset.UtcNow.AddDays(2), 2000f);
        //plotPoints.Add(DateTimeOffset.UtcNow.AddDays(1), 3000f);
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        IsOpen = true;

        this.plugin = plugin;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    public unsafe override bool DrawConditions()
    {
        if (!plugin.Configuration.Enabled)
        {
            return false;
        }

        if (Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedSummoningBell])
        {
            var retainerSellAddonPtr = Service.GameGui.GetAddonByName("RetainerSell");
            var itemSearchResultAddonPtr = Service.GameGui.GetAddonByName("ItemSearchResult");
            AtkUnitBase* baseNode = null;
            if (retainerSellAddonPtr == nint.Zero)
            {
                return false;
            }
            if (itemSearchResultAddonPtr != nint.Zero)
            {
                baseNode = (AtkUnitBase*)itemSearchResultAddonPtr;
            }
            else
            {
                baseNode = (AtkUnitBase*)retainerSellAddonPtr;
            }
            if (baseNode->IsVisible && baseNode->UldManager.LoadedState == AtkLoadState.Loaded)
            {
                Position = new(baseNode->X - width, baseNode->Y);

                var addon = (AddonRetainerSell*)retainerSellAddonPtr;

                if (addon->ItemName != null)
                {
                    var fullItemName = addon->ItemName->NodeText.ToString()[14..^10].Replace("\u0002\u0010\u0001\u0003", "");
                    
                    itemHq = fullItemName.EndsWith(HqSymbol);
                    var newItemName = itemHq ? fullItemName[..^2] : fullItemName;
                    if (itemName != newItemName)
                    {
                        fetchingDataCenterData = false;
                        itemName = newItemName;
                        itemId = Service.DataManager.GetExcelSheet<Item>()!.Where((i) => i.Name == itemName).First().RowId;
                        dcMarketData = null;
                        worldMarketData = [];
                        plotPoints = new();
                        totalNumberOfDays = 0;
                        totalEarnings = 0;
                        unitsSold = 0;
                        foreach (var item in Readers.ReadItemsFromCsv())
                        {
                            if (item.ItemId != itemId)
                            {
                                continue;
                            }
                            if (item.SaleDateTime() < earliestSale)
                            {
                                earliestSale = item.SaleDateTime();
                            }
                            if (item.SaleDateTime() > latestSale)
                            {
                                latestSale = item.SaleDateTime();
                            }
                            totalNumberOfDays = (latestSale - earliestSale).Days;
                            totalEarnings += item.TotalPrice;
                            unitsSold += item.Quantity;
                            plotPoints.Add(item.SaleDateTime(), item.PricePerUnitSold);
                            //Service.PluginLog.Debug($"({item},{})");
                        }
                    }
                }

                return true;
            }
        }
        return false;
    }

    public override void Draw()
    {
        var requestRegion = plugin.Configuration.RequestRegion;
        var requestDataCenter = plugin.Configuration.RequestDataCenter;
        var requestWorlds = plugin.Configuration.RequestWorlds;
        var showGraph = plugin.Configuration.ShowGraph;

        if (ImGui.Checkbox("Request Region", ref requestRegion))
        {
            plugin.Configuration.RequestRegion = requestRegion;
            plugin.Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Request Data Center", ref requestDataCenter))
        {
            plugin.Configuration.RequestDataCenter = requestDataCenter;
            plugin.Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Request Worlds", ref requestWorlds))
        {
            plugin.Configuration.RequestWorlds = requestWorlds;
            plugin.Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Show Graph", ref showGraph))
        {
            plugin.Configuration.ShowGraph = showGraph;
            plugin.Configuration.Save();
        }
        Service.FontManager.H1.Push();
        ImGui.Text($"{itemName}{(itemHq ? $" {HqSymbol}" : "")}");
        Service.FontManager.H1.Pop();
        ImGui.Text($"Units sold: {unitsSold}");
        ImGui.Text($"Total earnings: {GilSymbol}{totalEarnings:N0}");
        ImGui.Text($"Earnings/Day: {GilSymbol}{(totalNumberOfDays == 0 ? 0 : totalEarnings / totalNumberOfDays)}");
        //ImGui.Text($"Span: {totalNumberOfDays}");

        //if (ImGui.BeginChild("Glossary", ImGui.CalcTextSize("TEST"), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize))
        //{
        //    ImGui.Text("TEST");
        //    ImGui.CalcTextSize()
        //}
        //ImGui.EndChild();
        if (plugin.Configuration.RequestRegion)
        {
            Service.FontManager.H2.Push();
            DayTrader.ImGuiExtensions.SeparatorText("Region");
            Service.FontManager.H2.Pop();
            ImGui.Text("TODO");
        }
        if (plugin.Configuration.RequestDataCenter)
        {
            var dataCenter = Service.ClientState.LocalPlayer?.HomeWorld.Value.DataCenter.Value.Name.ToString()!;
            Service.FontManager.H2.Push();
            DayTrader.ImGuiExtensions.SeparatorText("Data Center");
            Service.FontManager.H2.Pop();
            Service.FontManager.H3.Push();
            ImGui.Text(dataCenter);
            Service.FontManager.H3.Pop();
            if (dcMarketData == null)
            {
                ImGuiExtensions.Spinner("DCSpinner", 10.0f, 2, ImGuiColors.TankBlue);

                if (!fetchingDataCenterData)
                {
                    fetchingDataCenterData = true;
                    Service.PluginLog.Debug("MAKING UNIVERSALIS REQUEST");
                    Task.Run(async () =>
                    {
                        try
                        {
                            this.requestError = false;
                            dcMarketData = await UniversalisClient.GetItemInfo(itemId, dataCenter, 10, CancellationToken.None);
                        } catch (System.Net.Http.HttpRequestException e)
                        {
                            Service.PluginLog.Error(e.Message);
                            requestError = true;
                        }
                        fetchingDataCenterData = false;
                    });
                }
            }
            else
            {
                if (requestError)
                {
                    ImGui.Text("Error fetching data");
                    return;
                }
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
        //if (Plugin.Configuration.RequestWorlds)
        //{
        //    Service.FontManager.H2.Push();
        //    DayTrader.ImGuiExtensions.SeparatorText("Worlds");
        //    Service.FontManager.H2.Pop();

        //    if (worldMarketData.Count == 0)
        //    {
        //        var worlds = Service.DataManager.GetExcelSheet<World>()!.Where((i) => i.DataCenter.Value?.RowId == dataCenter!.RowId);
        //        foreach (var world in worlds)
        //        {
        //            if (!world.IsPublic)
        //            {
        //                continue;
        //            }
        //            var marketData = await UniversalisClient.GetItemInfo(itemId, world.Name.RawString, 10, CancellationToken.None);
        //            if (marketData.WorldID == Service.ClientState.LocalPlayer!.HomeWorld.Id)
        //            {
        //                universalisData.Insert(0, marketData);
        //                continue;
        //            }
        //            Service.PluginLog.Debug(world.Name.RawString);
        //            Service.PluginLog.Debug(marketData.ToString());
        //            universalisData.Add(marketData);
        //        }
        //    }


        //    foreach (var world in worldMarketData)
        //    {
        //        Service.FontManager.H3.Push();
        //        DayTrader.ImGuiExtensions.SeparatorText(world.WorldName);
        //        Service.FontManager.H3.Pop();
        //        if (itemHq)
        //        {
        //            ImGui.Text($"World Sale Velocity: {world.HqSaleVelocity}");
        //            ImGui.Text($"Current Average Price: {world.AveragePriceHq}");
        //        }
        //        else
        //        {
        //            ImGui.Text($"Sale Velocity: {world.NqSaleVelocity}");
        //            ImGui.Text($"Current Average Price: {world.AveragePriceNq}");
        //        }
        //        ImGui.Text($"Units for sale: {world.UnitsForSale}");
        //        ImGui.Text($"Units sold: {world.UnitsSold}");
        //    }
        //}
        width = ImGui.GetWindowSize().X;

        if (plugin.Configuration.ShowGraph)
        {
            drawGraph();
        }
    }

    private void drawGraph()
    {
        //Service.PluginLog.Debug($"{plotPoints.GetSize()}");
        if (plotPoints.GetSize() < 2)
        {
            ImGui.Text($"{plotPoints.GetSize()} plot points found. Minimum of 2 needed to show graph.");
            return;
        }
        if (ImPlot.BeginPlot($"{itemName} Price", new Vector2(500, 500), ImPlotFlags.NoTitle))
        {
            float[] xs = plotPoints.GetXs();
            float[] ys = plotPoints.GetYs();
            ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, 0.25f);
            ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Time);
            //ImPlot.SetupAxes("Time", "Price", ImPlotAxisFlags.AutoFit | ImPlotAxisFlags.NoLabel, ImPlotAxisFlags.AutoFit | ImPlotAxisFlags.NoLabel);
            ImPlot.SetupAxisLimits(ImAxis.Y1, 0, ys.Max());
            ImPlot.SetupAxes("Time", "Price", ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.NoLabel);
            //ImPlot.SetupAxesLimits(0, double.MaxValue, 0, double.MaxValue);
            ImPlot.PlotLine("", ref xs[0],  ref ys[0], plotPoints.GetSize(), ImPlotLineFlags.Shaded);
            ImPlot.EndPlot();
        }
    }
}
