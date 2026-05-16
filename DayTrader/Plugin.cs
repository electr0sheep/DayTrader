using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Plugin.Windows;
using DayTrader;
using Dalamud.Utility;
using Dalamud.Hooking;
using DayTrader.FileHelpers;
using System.Threading.Tasks;
using DayTrader.Interop;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace Plugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Day Trader";
        private const string CommandName = "/pdt";

        private const ushort RetainerSaleHistoryOpcode = 185;

        private bool opcodeNotificationShown = false;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("DayTrader");

        private ConfigWindow ConfigWindow { get; init; }
        private RetainerSellOverlay MainWindow { get; init; }
        private HelpWindow HelpWindow { get; init; }
        private RetainerSellListOverlay RetainerSellListOverlay { get; init; }
        private DashboardWindow Dashboard { get; init; }
        private readonly Hook<PacketDispatcher.Delegates.OnReceivePacket> onReceivePacketHook;

        public unsafe Plugin(IDalamudPluginInterface PluginInterface)
        {
            PluginInterface.Create<Service>();

            Service.FontManager = new();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new RetainerSellOverlay(this);
            HelpWindow = new HelpWindow(this);
            RetainerSellListOverlay = new(this);
            Dashboard = new DashboardWindow(this);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(HelpWindow);
            WindowSystem.AddWindow(RetainerSellListOverlay);
            WindowSystem.AddWindow(Dashboard);

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the Day Trader sale-history dashboard. Subcommands: 'config', 'help'."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            var onReceivePacketAddr = (nint)PacketDispatcher.StaticVirtualTablePointer->OnReceivePacket;
            this.onReceivePacketHook = Service.GameInteropProvider
                .HookFromAddress<PacketDispatcher.Delegates.OnReceivePacket>(onReceivePacketAddr, OnReceivePacketDetour);
            this.onReceivePacketHook.Enable();
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();
            Dashboard.Dispose();

            Service.CommandManager.RemoveHandler(CommandName);
            this.onReceivePacketHook.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            var argv = args.Split(' ');
            if (string.IsNullOrEmpty(argv[0]))
            {
                Dashboard.IsOpen = true;
                return;
            }
            if (argv[0] == "config")
            {
                ConfigWindow.IsOpen = true;
                return;
            }
            if (argv[0] == "help")
            {
                HelpWindow.IsOpen = true;
                return;
            }
        }

        private unsafe void OnReceivePacketDetour(PacketDispatcher* dispatcher, uint targetId, IntPtr dataPtr)
        {
            // dataPtr arrives 0x10 into the packet header; back up to reach the start.
            dataPtr -= 0x10;
            try
            {
                var opCode = (ushort)Marshal.ReadInt16(dataPtr, 0x12);
                if (opCode == RetainerSaleHistoryOpcode)
                {
                    List<DayTrader.Models.SaleHistoryItem> items = [];
                    var saleHistory = (SaleHistory*)(dataPtr + 0x20);
                    foreach (var item in saleHistory->ItemList())
                    {
                        // copies items to list because the memory will be reused, and I want to process the CSV writing async
                        items.Add(new DayTrader.Models.SaleHistoryItem
                        {
                            BuyerName = item.BuyerName(),
                            ItemId = item.ItemId,
                            PricePerUnitSold = item.PricePerUnitSold(),
                            Quantity = item.Quantity,
                            SaleDate = item.SaleDate,
                            TotalPrice = item.SalePrice
                        });
                    }
                    if (!opcodeNotificationShown)
                    {
                        Service.NotificationManager.AddNotification(new()
                        {
                            Title = "DayTrader",
                            Content = "Opcode is correct.",
                            Type = Dalamud.Interface.ImGuiNotification.NotificationType.Success
                        });
                        opcodeNotificationShown = true;
                    }
                    Task.Run(() =>
                    {
                        Writers.WriteItemsToCsv(items);
                    });
                }
            }
            catch (Exception e)
            {
                if (!opcodeNotificationShown)
                {
                    Service.NotificationManager.AddNotification(new()
                    {
                        Title = "DayTrader",
                        Content = $"Opcode is incorrect.\n{e.Message}",

                        Type = Dalamud.Interface.ImGuiNotification.NotificationType.Error
                    });
                    opcodeNotificationShown = true;
                }
            }
            this.onReceivePacketHook.Original(dispatcher, targetId, dataPtr + 0x10);
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
