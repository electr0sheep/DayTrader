using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Utility;
using DayTrader;
using DayTrader.Addons;
using DayTrader.FileHelpers;
using DayTrader.Interop;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Plugin.Windows;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Plugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Day Trader";
        private const string CommandName = "/pdt";

        // Opcodes and packet offsets change on essentially every game patch. VerifiedGameVersion
        // records the ffxivgame.ver string this build was last confirmed against; bump it together
        // with RetainerSaleHistoryOpcode each time you re-verify after a patch. When the running
        // game version differs, the opcode below is almost certainly stale and DayTrader will record
        // wrong or no sale history until it's updated, so we warn at startup. Leave it empty to
        // suppress the warning — the detected version is always written to the plugin log so you can
        // copy the correct value here.
        private const ushort RetainerSaleHistoryOpcode = 874;
        private const string VerifiedGameVersion = "2026.05.25.0000.0000";


        private bool opcodeNotificationShown = false;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("DayTrader");

        private ConfigWindow ConfigWindow { get; init; }
        private RetainerSellOverlay MainWindow { get; init; }
        private HelpWindow HelpWindow { get; init; }
        private RetainerSellListOverlay RetainerSellListOverlay { get; init; }
        private DashboardWindow Dashboard { get; init; }
        internal RetainerSellListColumn RetainerSellListColumn { get; init; }
        internal ListingTimestampStore ListingTimestampStore { get; init; }
        private AutoCycleSaleHistory AutoCycleSaleHistory { get; init; }
        private readonly Hook<PacketDispatcher.Delegates.OnReceivePacket> onReceivePacketHook;

        private unsafe delegate void MoveToRetainerMarketDelegate(
            InventoryManager* inventoryManager,
            InventoryType srcInv,
            ushort srcSlot,
            InventoryType dstInv,
            ushort dstSlot,
            uint quantity,
            uint unitPrice);

        private unsafe delegate void SetRetainerMarketPriceDelegate(
            InventoryManager* inventoryManager,
            short slot,
            uint price);

        private unsafe delegate int MoveFromRetainerMarketToPlayerInventoryDelegate(
            InventoryManager* inventoryManager,
            InventoryType srcInv,
            ushort srcSlot,
            uint quantity);

        private unsafe delegate int MoveFromRetainerMarketToRetainerInventoryDelegate(
            InventoryManager* inventoryManager,
            InventoryType srcInv,
            ushort srcSlot,
            uint quantity);

        private readonly Hook<MoveToRetainerMarketDelegate> moveToRetainerMarketHook;
        private readonly Hook<SetRetainerMarketPriceDelegate> setRetainerMarketPriceHook;
        private readonly Hook<MoveFromRetainerMarketToPlayerInventoryDelegate> moveFromRetainerMarketToPlayerInventoryHook;
        private readonly Hook<MoveFromRetainerMarketToRetainerInventoryDelegate> moveFromRetainerMarketToRetainerInventoryHook;

        public unsafe Plugin(IDalamudPluginInterface PluginInterface)
        {
            PluginInterface.Create<Service>();

            // Warn early — before the patch-sensitive hooks below are installed — if the game has
            // been updated since this build was last verified.
            CheckGameVersion();

            Service.FontManager = new();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new RetainerSellOverlay(this);
            HelpWindow = new HelpWindow(this);
            RetainerSellListOverlay = new(this);
            Dashboard = new DashboardWindow(this);
            ListingTimestampStore = new ListingTimestampStore();
            ListingTimestampStore.Load();
            RetainerSellListColumn = new RetainerSellListColumn(this);
            AutoCycleSaleHistory = new AutoCycleSaleHistory(this);

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

            // Hooks on the InventoryManager methods that emit the outgoing market packets:
            // - MoveToRetainerMarket fires when a brand-new listing is placed (item moved from
            //   player/retainer inventory into the RetainerMarket inventory at a chosen slot).
            // - SetRetainerMarketPrice fires when an existing listing's price/qty is adjusted.
            // - MoveFromRetainerMarketTo{Player,Retainer}Inventory fire when the seller pulls
            //   a listing back. No sale can ever match a withdrawn entry, so an empty post-call
            //   slot means we drop the entry; a still-occupied slot is a partial withdrawal and
            //   the entry's CreatedAt stays put.
            // Hooking here gives us slot + price directly, no opcode/struct guessing needed.
            var moveToMarketAddr = (nint)InventoryManager.Addresses.MoveToRetainerMarket.Value;
            this.moveToRetainerMarketHook = Service.GameInteropProvider
                .HookFromAddress<MoveToRetainerMarketDelegate>(moveToMarketAddr, MoveToRetainerMarketDetour);
            this.moveToRetainerMarketHook.Enable();

            var setPriceAddr = (nint)InventoryManager.Addresses.SetRetainerMarketPrice.Value;
            this.setRetainerMarketPriceHook = Service.GameInteropProvider
                .HookFromAddress<SetRetainerMarketPriceDelegate>(setPriceAddr, SetRetainerMarketPriceDetour);
            this.setRetainerMarketPriceHook.Enable();

            var withdrawToPlayerAddr = (nint)InventoryManager.Addresses.MoveFromRetainerMarketToPlayerInventory.Value;
            this.moveFromRetainerMarketToPlayerInventoryHook = Service.GameInteropProvider
                .HookFromAddress<MoveFromRetainerMarketToPlayerInventoryDelegate>(withdrawToPlayerAddr, MoveFromRetainerMarketToPlayerInventoryDetour);
            this.moveFromRetainerMarketToPlayerInventoryHook.Enable();

            var withdrawToRetainerAddr = (nint)InventoryManager.Addresses.MoveFromRetainerMarketToRetainerInventory.Value;
            this.moveFromRetainerMarketToRetainerInventoryHook = Service.GameInteropProvider
                .HookFromAddress<MoveFromRetainerMarketToRetainerInventoryDelegate>(withdrawToRetainerAddr, MoveFromRetainerMarketToRetainerInventoryDetour);
            this.moveFromRetainerMarketToRetainerInventoryHook.Enable();
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();
            Dashboard.Dispose();
            RetainerSellListColumn.Dispose();
            AutoCycleSaleHistory.Dispose();

            Service.CommandManager.RemoveHandler(CommandName);
            this.onReceivePacketHook.Dispose();
            this.moveToRetainerMarketHook.Dispose();
            this.setRetainerMarketPriceHook.Dispose();
            this.moveFromRetainerMarketToPlayerInventoryHook.Dispose();
            this.moveFromRetainerMarketToRetainerInventoryHook.Dispose();
        }

        private unsafe void MoveToRetainerMarketDetour(
            InventoryManager* inventoryManager,
            InventoryType srcInv,
            ushort srcSlot,
            InventoryType dstInv,
            ushort dstSlot,
            uint quantity,
            uint unitPrice)
        {
            this.moveToRetainerMarketHook.Original(
                inventoryManager, srcInv, srcSlot, dstInv, dstSlot, quantity, unitPrice);

            try
            {
                if (dstInv == InventoryType.RetainerMarket)
                {
                    var (retainerId, retainerName) = GetActiveRetainer();
                    var srcContainer = inventoryManager->GetInventoryContainer(srcInv);
                    uint itemId = 0;
                    if (srcContainer != null)
                    {
                        var srcItem = srcContainer->GetInventorySlot(srcSlot);
                        if (srcItem != null) itemId = srcItem->ItemId;
                    }
                    Service.PluginLog.Info(
                        $"[DayTrader/Record/New] retainer={retainerName}({retainerId}) dstSlot={dstSlot} itemId={itemId} qty={quantity} unitPrice={unitPrice}");
                    ListingTimestampStore.RecordNewListing(
                        retainerId, retainerName, (short)dstSlot, itemId, unitPrice);
                }
            }
            catch (Exception e)
            {
                Service.PluginLog.Error($"[DayTrader] MoveToRetainerMarket detour failed: {e}");
            }
        }

        private unsafe void SetRetainerMarketPriceDetour(
            InventoryManager* inventoryManager,
            short slot,
            uint price)
        {
            this.setRetainerMarketPriceHook.Original(inventoryManager, slot, price);

            try
            {
                var (retainerId, retainerName) = GetActiveRetainer();
                var marketContainer = inventoryManager->GetInventoryContainer(InventoryType.RetainerMarket);
                uint itemId = 0;
                if (marketContainer != null)
                {
                    var item = marketContainer->GetInventorySlot(slot);
                    if (item != null) itemId = item->ItemId;
                }
                Service.PluginLog.Info(
                    $"[DayTrader/Record/Adjust] retainer={retainerName}({retainerId}) slot={slot} itemId={itemId} newPrice={price}");
                ListingTimestampStore.RecordPriceUpdate(
                    retainerId, retainerName, slot, itemId, price);
            }
            catch (Exception e)
            {
                Service.PluginLog.Error($"[DayTrader] SetRetainerMarketPrice detour failed: {e}");
            }
        }

        private unsafe int MoveFromRetainerMarketToPlayerInventoryDetour(
            InventoryManager* inventoryManager,
            InventoryType srcInv,
            ushort srcSlot,
            uint quantity)
        {
            var result = this.moveFromRetainerMarketToPlayerInventoryHook.Original(
                inventoryManager, srcInv, srcSlot, quantity);
            HandleWithdrawal(inventoryManager, srcSlot, "ToPlayer");
            return result;
        }

        private unsafe int MoveFromRetainerMarketToRetainerInventoryDetour(
            InventoryManager* inventoryManager,
            InventoryType srcInv,
            ushort srcSlot,
            uint quantity)
        {
            var result = this.moveFromRetainerMarketToRetainerInventoryHook.Original(
                inventoryManager, srcInv, srcSlot, quantity);
            HandleWithdrawal(inventoryManager, srcSlot, "ToRetainer");
            return result;
        }

        // Inspect the market slot after the move completed. If it's empty, the seller
        // pulled the whole stack and the listing entry should be dropped. If items remain,
        // it was a partial withdrawal — the listing is still live at the original CreatedAt.
        private unsafe void HandleWithdrawal(InventoryManager* inventoryManager, ushort slot, string destination)
        {
            try
            {
                var marketContainer = inventoryManager->GetInventoryContainer(InventoryType.RetainerMarket);
                bool slotEmpty = true;
                if (marketContainer != null)
                {
                    var item = marketContainer->GetInventorySlot(slot);
                    if (item != null && item->ItemId != 0 && item->Quantity > 0)
                    {
                        slotEmpty = false;
                    }
                }
                var (retainerId, retainerName) = GetActiveRetainer();
                if (slotEmpty)
                {
                    Service.PluginLog.Info(
                        $"[DayTrader/Record/Withdraw] retainer={retainerName}({retainerId}) slot={slot} dest={destination} fully-withdrawn");
                    ListingTimestampStore.RemoveListing(retainerId, (short)slot);
                }
                else
                {
                    Service.PluginLog.Info(
                        $"[DayTrader/Record/Withdraw] retainer={retainerName}({retainerId}) slot={slot} dest={destination} partial — entry preserved");
                }
            }
            catch (Exception e)
            {
                Service.PluginLog.Error($"[DayTrader] HandleWithdrawal failed: {e}");
            }
        }

        private static unsafe (ulong RetainerId, string? RetainerName) GetActiveRetainer()
        {
            var manager = RetainerManager.Instance();
            if (manager == null) return (0, null);
            var active = manager->GetActiveRetainer();
            if (active == null) return (manager->LastSelectedRetainerId, null);
            return (active->RetainerId, active->NameString);
        }

        // Diagnostic: returns AgentRetainer's base pointer so you can pin it in
        // Cheat Engine while exploring the (untyped) sell-list row layout.
        internal static unsafe nint GetAgentRetainerAddress()
        {
            var agentModule = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentModule.Instance();
            if (agentModule == null) return nint.Zero;
            var agent = agentModule->GetAgentByInternalId(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentId.Retainer);
            return (nint)agent;
        }

        // The sale-history opcode and the inventory hooks are pinned to a specific game build, so a
        // patch almost always invalidates them. Compare the running client version against the one
        // this build was verified on and warn the user if the game has been updated.
        private static unsafe void CheckGameVersion()
        {
            try
            {
                var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
                var current = framework != null ? framework->GameVersionString : null;

                if (string.IsNullOrEmpty(current))
                {
                    Service.PluginLog.Warning("[DayTrader] Could not read the current game version; skipping patch check.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(VerifiedGameVersion))
                {
                    Service.PluginLog.Info(
                        $"[DayTrader] Running on game version {current}. No verified baseline is pinned; set VerifiedGameVersion to this value to enable patch warnings.");
                    return;
                }

                if (current == VerifiedGameVersion)
                {
                    Service.PluginLog.Info($"[DayTrader] Game version {current} matches the verified baseline.");
                    return;
                }

                Service.PluginLog.Warning(
                    $"[DayTrader] Game version changed: verified against {VerifiedGameVersion} but currently running {current}. " +
                    "A patch has likely changed the sale-history opcode and inventory hooks, so DayTrader may record wrong or no data until it is updated.");

                Service.NotificationManager.AddNotification(new()
                {
                    Title = "DayTrader may be out of date",
                    Content =
                        $"The game was patched ({VerifiedGameVersion} -> {current}). DayTrader's market hooks are " +
                        "probably stale, so sale history may be wrong or missing until the plugin is updated.",
                    Type = Dalamud.Interface.ImGuiNotification.NotificationType.Warning
                });
            }
            catch (Exception e)
            {
                Service.PluginLog.Error($"[DayTrader] Game version check failed: {e}");
            }
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
                    AutoCycleSaleHistory.NotifySaleHistoryReceived();
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

        public void ToggleConfigUI()
        {
            ConfigWindow.Toggle();
        }
    }
}
