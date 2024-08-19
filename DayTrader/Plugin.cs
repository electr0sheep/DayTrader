using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Plugin.Windows;
using DayTrader;
using System.Linq;
using System.Diagnostics.Contracts;
using Dalamud.Utility;
using ImGuiNET;
using System.Reflection.Emit;
using Dalamud.Game.Network;
using DayTrader.Models;
using System;
using Lumina.Excel.GeneratedSheets2;
using Dalamud.Game.Text.SeStringHandling;
using System.Text;

namespace Plugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Day Trader";
        private const string CommandName = "/pdt";

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("DayTrader");

        private ConfigWindow ConfigWindow { get; init; }
        private RetainerSellOverlay MainWindow { get; init; }
        private HelpWindow HelpWindow { get; init; }
        private RetainerSellListOverlay RetainerSellListOverlay { get; init; }

        public Plugin(IDalamudPluginInterface PluginInterface)
        {
            PluginInterface.Create<Service>();

            Service.FontManager = new();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new RetainerSellOverlay(this);
            HelpWindow = new HelpWindow(this);
            RetainerSellListOverlay = new(this);
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(HelpWindow);
            WindowSystem.AddWindow(RetainerSellListOverlay);

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Displays Day Trader config window"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Service.GameNetwork.NetworkMessage += OnNetworkMessage;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            
            Service.CommandManager.RemoveHandler(CommandName);
            Service.GameNetwork.NetworkMessage -= OnNetworkMessage;
        }

        private void OnCommand(string command, string args)
        {
            var argv = args.Split(' ');
            if (argv[0].IsNullOrEmpty())
            {
                ConfigWindow.IsOpen = true;
            }
            if (argv[0] == "help")
            {
                HelpWindow.IsOpen = true;
            }
        }

        private unsafe void OnNetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (direction == NetworkMessageDirection.ZoneDown && opCode == 803)
            {
                Service.PluginLog.Debug($"{dataPtr:X}");
                SaleHistory* saleHistory = (SaleHistory*)dataPtr;
                foreach (SaleHistoryItem item in saleHistory->ItemList())
                {
                    var itemName = Service.DataManager.GetExcelSheet<Item>()!.GetRow(item.ItemId)!.Name;
                    Service.PluginLog.Debug($"{itemName}: {item.SalePrice}: {item.Date}: {item.BuyerName()}");
                }
            }
            return;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
