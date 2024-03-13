// How to glue a ImGui window to an in-game window adapted from https://github.com/Infiziert90/SubmarineTracker/blob/master/SubmarineTracker/Windows/Overlays/RouteOverlay.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using DayTrader;
using DayTrader.Models;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;
using OtterGuiInternal.Enums;

namespace Plugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private CurrentlyShownView? dcMarketData;
    private List<CurrentlyShownView> worldMarketData;
    private string? itemName;
    private uint itemId;
    private bool itemHq;
    private bool fetchingData = false;

    private const char HqSymbol = '';

    public MainWindow(Plugin plugin) : base("Market Data Window")
    {
        Size = new Vector2(0, 0);
        Flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize;
        //Flags |= ImGuiWindowFlags.NoScrollbar;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ForceMainWindow = true;

        Plugin = plugin;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    public override unsafe void PreOpenCheck()
    {
        IsOpen = false;
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
                    worldMarketData = new();
                    fetchingData = false;
                }

                //Size = new Vector2(Size!.Value.X * ImGuiHelpers.GlobalScaleSafe, baseNode->RootNode->Height * baseNode->Scale);
                Size = new Vector2(baseNode->RootNode->Width * baseNode->Scale, baseNode->RootNode->Height * baseNode->Scale);
                Position = new Vector2(baseNode->X - (Size!.Value.X * ImGuiHelpers.GlobalScale), baseNode->Y);
                
                IsOpen = true;
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
                    dcMarketData = await UniversalisClient.GetItemInfo(itemId, dataCenter.Name, 10, CancellationToken.None);
                    foreach (var world in worlds)
                    {
                        if (!world.IsPublic)
                        {
                            continue;
                        }
                        var marketData = await UniversalisClient.GetItemInfo(itemId, world.Name.RawString, 10, CancellationToken.None);
                        if (marketData.WorldID == Service.ClientState.LocalPlayer.HomeWorld.Id)
                        {
                            worldMarketData.Insert(0, marketData);
                            continue;
                        }
                        Service.PluginLog.Debug(world.Name.RawString);
                        Service.PluginLog.Debug(marketData.ToString());
                        worldMarketData.Add(marketData);
                    }
                });
            }
            ImGui.Text("Loading...");
        }
        else
        {
            Service.FontManager.H1.Push();
            ImGui.Text($"{itemName}{(itemHq ? $" {HqSymbol}" : "")}");
            Service.FontManager.H1.Pop();
            Service.FontManager.H2.Push();
            DayTrader.ImGuiExtensions.SeparatorText("Region");
            Service.FontManager.H2.Pop();
            ImGui.Text("TODO");
            Service.FontManager.H2.Push();
            DayTrader.ImGuiExtensions.SeparatorText("Data Center");
            Service.FontManager.H2.Pop();
            Service.FontManager.H3.Push();
            ImGui.Text(Service.ClientState.LocalPlayer?.HomeWorld.GameData?.DataCenter?.Value?.Name!);
            Service.FontManager.H3.Pop();
            if (itemHq)
            {
                ImGui.Text($"Sale Velocity: {dcMarketData.HqSaleVelocity}");
                ImGui.Text($"Current Average Price: {dcMarketData.AveragePriceHq}");
            }
            else
            {
                ImGui.Text($"Sale Velocity: {dcMarketData.NqSaleVelocity}");
                ImGui.Text($"Current Average Price: {dcMarketData.AveragePriceNq}");
            }
            ImGui.Text($"Units for sale: {dcMarketData.UnitsForSale}");
            ImGui.Text($"Units sold: {dcMarketData.UnitsSold}");

            Service.FontManager.H2.Push();
            DayTrader.ImGuiExtensions.SeparatorText("Worlds");
            Service.FontManager.H2.Pop();

            foreach (var world in worldMarketData)
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
    }
}
