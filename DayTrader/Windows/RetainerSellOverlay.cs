// How to glue a ImGui window to an in-game window adapted from https://github.com/Infiziert90/SubmarineTracker/blob/master/SubmarineTracker/Windows/Overlays/RouteOverlay.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using DayTrader;
using DayTrader.FileHelpers;
using DayTrader.Models;
using DayTrader.Models.Saddlebags;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace Plugin.Windows;

public class RetainerSellOverlay : Window, IDisposable
{
    private readonly Plugin plugin;

    private float width;

    private ItemHistory? dcMarketData;
    private List<ItemHistory> worldMarketData = new();
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
                baseNode = (AtkUnitBase*)itemSearchResultAddonPtr.Address;
            }
            else
            {
                baseNode = (AtkUnitBase*)retainerSellAddonPtr.Address;
            }
            if (baseNode->IsVisible && baseNode->UldManager.LoadedState == AtkLoadState.Loaded)
            {
                Position = new(baseNode->X - width, baseNode->Y);

                var addon = (AddonRetainerSell*)retainerSellAddonPtr.Address;

                if (addon->ItemName != null)
                {
                    var fullItemName = addon->ItemName->NodeText.ToString()[14..^10].Replace("\u0002\u0010\u0001\u0003", "");
                    
                    itemHq = fullItemName.EndsWith(HqSymbol);
                    var newItemName = itemHq ? fullItemName[..^2] : fullItemName;
                    if (itemName != newItemName)
                    {
                        fetchingDataCenterData = false;
                        requestError = false;
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
        var requestSaddlebags = plugin.Configuration.RequestSaddlebags;
        var showSalesGraph = plugin.Configuration.ShowSalesGraph;
        var showPriceHistory = plugin.Configuration.ShowPriceHistory;
        var showSaleDistribution = plugin.Configuration.ShowSaleDistribution;
        var showStackSizeHistory = plugin.Configuration.ShowStackSizeHistory;
        var showSalesPerHour = plugin.Configuration.ShowSalesPerHour;
        var homeWorld = Service.ObjectTable.LocalPlayer?.HomeWorld.Value.Name.ToString()!;
        var dataCenter = Service.ObjectTable.LocalPlayer?.HomeWorld.Value.DataCenter.Value.Name.ToString()!;

        // CONFIGS
        if (ImGui.Checkbox("Get Saddlebags Stats", ref requestSaddlebags))
        {
            plugin.Configuration.RequestSaddlebags = requestSaddlebags;
            plugin.Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Show Sales Graph", ref showSalesGraph))
        {
            plugin.Configuration.ShowSalesGraph = showSalesGraph;
            plugin.Configuration.Save();
        }
        if (plugin.Configuration.RequestSaddlebags)
        {
            ImGui.Indent();
            if (ImGui.Checkbox("Show Price History", ref showPriceHistory))
            {
                plugin.Configuration.ShowPriceHistory = showPriceHistory;
                plugin.Configuration.Save();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("Show Sales Distribution", ref showSaleDistribution))
            {
                plugin.Configuration.ShowSaleDistribution = showSaleDistribution;
                plugin.Configuration.Save();
            }
            if (ImGui.Checkbox("Show Stack Size History", ref showStackSizeHistory))
            {
                plugin.Configuration.ShowStackSizeHistory = showStackSizeHistory;
                plugin.Configuration.Save();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox($"Show {homeWorld} Sales Per Hour", ref showSalesPerHour))
            {
                plugin.Configuration.ShowSalesPerHour = showSalesPerHour;
                plugin.Configuration.Save();
            }
            ImGui.Unindent();
        }

        // CURRENT CHARACTER STATS
        Service.FontManager.H1.Push();
        ImGui.Text($"{itemName}{(itemHq ? $" {HqSymbol}" : "")}");
        Service.FontManager.H1.Pop();
        Service.FontManager.H2.Push();
        ImGuiExtensions.SeparatorText($"{Service.ObjectTable.LocalPlayer?.Name} Stats");
        Service.FontManager.H2.Pop();
        ImGui.Text($"Units sold: {unitsSold}");
        ImGui.Text($"Total earnings: {totalEarnings:N0}{GilSymbol}");
        ImGui.Text($"Earnings/Day: {(totalNumberOfDays == 0 ? 0 : totalEarnings / totalNumberOfDays)}{GilSymbol}");

        // CURRENT CHARACTER SALES CHART
        if (plugin.Configuration.ShowSalesGraph)
        {
            drawSalesGraph();
        }

        // SADDLEBAGS BASE STATS
        if (plugin.Configuration.RequestSaddlebags)
        {
            Service.FontManager.H2.Push();
            ImGuiExtensions.SeparatorText("Data Center");
            Service.FontManager.H2.Pop();
            Service.FontManager.H3.Push();
            ImGui.Text(dataCenter);
            Service.FontManager.H3.Pop();
            if (dcMarketData == null)
            {
                if (requestError)
                {
                    ImGui.Text("Error fetching data");
                }
                else
                {
                    ImGuiExtensions.Spinner(10.0f, 2, ImGuiColors.TankBlue);

                    if (!fetchingDataCenterData)
                    {
                        fetchingDataCenterData = true;
                        Service.PluginLog.Debug("MAKING SADDLEBAGS REQUEST");
                        Task.Run(async () =>
                        {
                            try
                            {
                                this.requestError = false;
                                dcMarketData = await SaddlebagsClient.GetItemInfo(itemId, itemHq, homeWorld, CancellationToken.None);
                            }
                            catch (Exception e)
                            {
                                Service.PluginLog.Error(e.Message);
                                requestError = true;
                            }
                            finally
                            {
                                fetchingDataCenterData = false;
                            }
                        });
                    }
                }
            }
            else
            {
                ImGui.Text($"Median Price Per Unit Sold: {dcMarketData.MedianPPU:n0}{GilSymbol}");
                //if (ImGui.IsItemHovered())
                //{
                //    ImGui.SetTooltip("The median sale price.");
                //}
                ImGui.Text($"Average Price Per Unit Sold: {dcMarketData.AveragePPU:n0}{GilSymbol}");
                ImGui.Text($"Average Purchases Per Day: {dcMarketData.AverageSalesPerDay:n0}");
                //if (ImGui.IsItemHovered())
                //{
                //    ImGui.SetTooltip("The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).\nThis number will tend to be the same for every item, because the number of shown sales is the same and over the same period.\nThis statistic is more useful in historical queries.");
                //}
                ImGui.Text($"Total Purchases Per Week: {dcMarketData.TotalPurchaseAmount:n0}");
                ImGui.Text($"Average Quantity Sold Per Day: {dcMarketData.AverageQuantitySoldPerDay:n0}");
                ImGui.Text($"Total Quantity Sold Per Week: {dcMarketData.TotalQuantitySold:n0}");
            }
        }

        // We set width here because everything below might take a while to fully load
        width = ImGui.GetWindowSize().X;

        // SADDLEBAGS CHARTS
        if (plugin.Configuration.RequestSaddlebags && plugin.Configuration.ShowPriceHistory && dcMarketData != null)
        {
            drawPriceHistoryChart();
        }

        if (plugin.Configuration.RequestSaddlebags && plugin.Configuration.ShowSaleDistribution && dcMarketData != null)
        {
            drawSalesDistribution();
        }

        if (plugin.Configuration.RequestSaddlebags && plugin.Configuration.ShowStackSizeHistory && dcMarketData != null)
        {
            ImGui.Spacing();
            drawStackSizeHistory();
        }

        if (plugin.Configuration.RequestSaddlebags && plugin.Configuration.ShowSalesPerHour && dcMarketData != null)
        {
            drawSalesPerHour();
        }
    }

    private void drawSalesGraph()
    {
        if (plotPoints.GetSize() < 2)
        {
            ImGui.Text($"{plotPoints.GetSize()} plot points found. Minimum of 2 needed to show graph.");
            return;
        }
        if (ImPlot.BeginPlot($"{Service.ObjectTable.LocalPlayer?.Name} Earning/Unit Sold", new Vector2(500, 250), ImPlotFlags.NoMouseText | ImPlotFlags.CanvasOnly))
        {
            float[] xs = plotPoints.GetXs();
            float[] ys = plotPoints.GetYs();
            ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, 0.25f);
            ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Time);
            ImPlot.SetupAxisLimits(ImAxis.Y1, 0, ys.Max());
            ImPlot.SetupAxes("Time", "Price", ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.AutoFit);
            ImPlot.PlotLine($"{itemName} Price Plot Line", ref xs[0], ref ys[0], plotPoints.GetSize(), ImPlotLineFlags.Shaded);
            ImPlot.EndPlot();
        }
    }

    private void drawPriceHistoryChart()
    {
        if (ImPlot.BeginPlot($"Price History###{itemName}", new Vector2(500, 250), ImPlotFlags.NoMouseText))
        {
            string[] xs = dcMarketData!.PriceHistory.Select(i => AbbreviateNumbers(i.PriceRange)).ToArray();
            float[] ys = [.. dcMarketData!.PriceHistory.Select(i => i.SalesAmount)];

            var maxY = ys.Max();
            var labelYTop = -maxY * 0.15f;
            var labelYBottom = -maxY * 0.30f;

            ImPlot.SetupAxes("Price Ranges in Gil", "# of sales", ImPlotAxisFlags.NoTickLabels, 0);
            ImPlot.SetupAxisLimits(ImAxis.X1, -0.5, xs.Length - 0.5, ImPlotCond.Once);
            ImPlot.SetupAxisLimits(ImAxis.Y1, -maxY * 0.42f, maxY * 1.1f, ImPlotCond.Once);
            ImPlot.SetupAxisTicks(ImAxis.X1, 0, xs.Length - 1, xs.Length, xs);

            ImPlot.PlotBars($"{itemName} Price History Plot Bar", ref ys[0], dcMarketData!.PriceHistory.Length);

            for (int x = 0; x < xs.Length; x++)
            {
                var rowY = (x % 2 == 0) ? labelYTop : labelYBottom;
                ImPlot.PlotText(xs[x], x, rowY, new Vector2(0, 0), ImPlotTextFlags.None);
            }
            ImPlot.EndPlot();
        }
    }

    private void drawSalesDistribution()
    {
        if (ImPlot.BeginPlot($"Sales Distribution###{itemName}", new Vector2(500, 250), ImPlotFlags.NoMouseText | ImPlotFlags.CanvasOnly))
        {
            string[] xs = typeof(ServerDistribution).GetProperties().Select(i => i.Name).ToArray();
            float[] ys = dcMarketData!.ServerDistribution.Values().Select(v => (float)v).ToArray();
            for (int x = 0; x < xs.Length; x++)
            {
                ImPlot.PlotText(xs[x], x - 0.25f, -50.0f, new Vector2(0, 0), ImPlotTextFlags.Vertical);
            }
            ImPlot.SetupAxisTicks(ImAxis.X1, 0, xs.Length - 1, xs.Length, xs);
            ImPlot.SetupAxes("Server", "# of sales", ImPlotAxisFlags.AutoFit | ImPlotAxisFlags.NoTickLabels, ImPlotAxisFlags.AutoFit);
            ImPlot.PlotBars($"{itemName} Sales Distribution Plot Bar", ref ys[0], dcMarketData!.ServerDistribution.Values().Length);
            ImPlot.EndPlot();
        }
    }

    private void drawStackSizeHistory()
    {
        if (ImGui.BeginTable($"Stack Size History###{itemName}", 4))
        {
            ImGui.TableSetupColumn("Bundle Size");
            ImGui.TableSetupColumn("# Sales");
            ImGui.TableSetupColumn("% Sales");
            ImGui.TableSetupColumn("% Total");
            ImGui.TableHeadersRow();
            foreach (var stackHistory in dcMarketData!.StackChance)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"{stackHistory.StackSize}");
                ImGui.TableNextColumn();
                ImGui.Text($"{stackHistory.NumberOfSales}");
                ImGui.TableNextColumn();
                ImGui.Text($"{stackHistory.PercentOfSales}");
                ImGui.TableNextColumn();
                ImGui.Text($"{stackHistory.PercentOfSalesPerUnit}");
            }
            ImGui.EndTable();
        }
    }

    private void drawSalesPerHour()
    {
        if (ImPlot.BeginPlot($"Sales per Hour###{itemName}", new Vector2(500, 250), ImPlotFlags.NoMouseText | ImPlotFlags.CanvasOnly))
        {
            float[] xs = dcMarketData!.SalesByHour.Select(i => (float)i.Time).ToArray();
            float[] ys = dcMarketData!.SalesByHour.Select(i => (float)i.SaleAmt).ToArray();
            ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, 0.25f);
            ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Time);
            ImPlot.SetupAxisLimits(ImAxis.Y1, 0.0f, ys.Max());
            ImPlot.SetupAxes("Time", "Sales", ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.AutoFit, ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.AutoFit);
            ImPlot.PlotLine($"{itemName} Sales per Hour Plot Line", ref xs[0], ref ys[0], plotPoints.GetSize(), ImPlotLineFlags.Shaded);
            ImPlot.EndPlot();
        }
    }

    private static string AbbreviateNumbers(string label)
    {
        return Regex.Replace(label, @"\d[\d,]*", match =>
        {
            var digits = match.Value.Replace(",", "");
            if (long.TryParse(digits, out var n))
            {
                if (n >= 1_000_000) return $"{n / 1_000_000.0:0.#}m";
                if (n >= 1_000) return $"{n / 1_000.0:0.#}k";
            }
            return match.Value;
        });
    }
}
