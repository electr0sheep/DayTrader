using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DayTrader.Interop;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace DayTrader;

// Drives an automatic per-retainer sale-history capture during an AutoRetainer
// run (single-character or multi-mode — the post-process hooks fire either way,
// after AR finishes its own chores for that retainer: ventures, entrust, gil
// withdraw, vendor sells). AR leaves the SelectString menu open and hands us
// the per-retainer window; we click "View Sale History", wait for opcode 185
// to land in the existing OnReceivePacketDetour, close the history addon, and
// hand control back to AR via FinishRetainerPostProcess.
//
// No-op if the user hasn't enabled the toggle. If AR isn't installed the IPC
// subscribers attach harmlessly to channels nothing publishes on — events
// simply never fire.
internal sealed class AutoCycleSaleHistory : IDisposable
{
    private const string SelectStringAddon = "SelectString";
    private const string SaleHistoryAddon = "RetainerHistory";

    // Matched case-insensitively against entry text. "View sale history." is
    // the current English entry; substring-match to survive minor copy changes.
    private const string SaleHistoryEntryNeedle = "Sale History";

    // SelectString AtkValues layout (verified against a live retainer menu):
    // [0..1] unset, [2] prompt string, [3] entry count (Int), [4..6] flags,
    // [7..7+count) entry strings. Callback wants the 0-based entry index, i.e.
    // (atkValueIndex - EntriesStartIndex).
    private const int EntryCountAtkValueIndex = 3;
    private const int EntriesStartAtkValueIndex = 7;

    private static readonly TimeSpan PacketWaitTimeout = TimeSpan.FromSeconds(15);

    private readonly Plugin.Plugin plugin;
    private readonly AutoRetainerIpc ipc;

    private TaskCompletionSource<bool>? packetTcs;
    private int active; // 0 = idle, 1 = a postprocess flow is in progress

    public AutoCycleSaleHistory(Plugin.Plugin plugin)
    {
        this.plugin = plugin;
        this.ipc = new AutoRetainerIpc(Service.PluginInterface, Service.PluginInterface.InternalName);
        this.ipc.OnRetainerPostprocessStep += HandlePostprocessStep;
        this.ipc.OnRetainerReadyToPostprocess += HandleReadyToPostprocess;
    }

    public void Dispose()
    {
        this.ipc.OnRetainerPostprocessStep -= HandlePostprocessStep;
        this.ipc.OnRetainerReadyToPostprocess -= HandleReadyToPostprocess;
        this.ipc.Dispose();
        packetTcs?.TrySetCanceled();
    }

    // Called from Plugin.OnReceivePacketDetour the moment opcode 185 arrives.
    // Safe to call from any thread — TrySetResult is thread-safe and the
    // awaiting code re-enters the framework thread before touching UI.
    public void NotifySaleHistoryReceived()
        => packetTcs?.TrySetResult(true);

    private void HandlePostprocessStep(string retainerName)
    {
        if (!plugin.Configuration.AutoCycleSaleHistoryEnabled) return;
        try { ipc.RequestRetainerPostprocess(); }
        catch (Exception e) { Service.PluginLog.Warning($"[DayTrader/AutoCycle] RequestRetainerPostprocess failed: {e.Message}"); }
    }

    private void HandleReadyToPostprocess(string pluginName, string retainerName)
    {
        if (pluginName != ipc.PluginName) return;
        if (!plugin.Configuration.AutoCycleSaleHistoryEnabled) { FinishSafely(); return; }
        if (Interlocked.Exchange(ref active, 1) == 1) { FinishSafely(); return; }

        _ = Task.Run(() => RunFlowAsync(retainerName));
    }

    private async Task RunFlowAsync(string retainerName)
    {
        try
        {
            packetTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var triggered = await Service.Framework.RunOnFrameworkThread(() => TryTriggerSaleHistory(retainerName));
            if (!triggered) return;

            var completed = await Task.WhenAny(packetTcs.Task, Task.Delay(PacketWaitTimeout));
            if (completed != packetTcs.Task)
                Service.PluginLog.Warning($"[DayTrader/AutoCycle] {retainerName}: timed out waiting for sale-history packet.");

            await Service.Framework.RunOnFrameworkThread(CloseSaleHistoryAddon);
        }
        catch (Exception e)
        {
            Service.PluginLog.Error($"[DayTrader/AutoCycle] flow failed for {retainerName}: {e}");
        }
        finally
        {
            packetTcs = null;
            FinishSafely();
            Interlocked.Exchange(ref active, 0);
        }
    }

    private static unsafe bool TryTriggerSaleHistory(string retainerName)
    {
        var addonPtr = Service.GameGui.GetAddonByName(SelectStringAddon);
        if (addonPtr == nint.Zero)
        {
            Service.PluginLog.Warning($"[DayTrader/AutoCycle] {retainerName}: SelectString addon not present; skipping.");
            return false;
        }
        var addon = (AtkUnitBase*)addonPtr.Address;
        if (addon == null || !addon->IsVisible)
        {
            Service.PluginLog.Warning($"[DayTrader/AutoCycle] {retainerName}: SelectString addon found but not visible; skipping.");
            return false;
        }

        var index = FindEntryIndex(addon, SaleHistoryEntryNeedle);
        if (index < 0)
        {
            Service.PluginLog.Warning($"[DayTrader/AutoCycle] {retainerName}: 'Sale History' entry not found in SelectString; skipping.");
            return false;
        }

        var values = stackalloc AtkValue[1];
        values[0] = new AtkValue { Type = AtkValueType.Int, Int = index };
        addon->FireCallback(1, values, true);
        Service.PluginLog.Info($"[DayTrader/AutoCycle] {retainerName}: selected SelectString entry #{index} (\"Sale History\").");
        return true;
    }

    private static unsafe int FindEntryIndex(AtkUnitBase* addon, string needle)
    {
        if (addon->AtkValuesCount <= EntryCountAtkValueIndex) return -1;
        var count = addon->AtkValues[EntryCountAtkValueIndex].Int;
        for (var i = 0; i < count; i++)
        {
            var atkIdx = EntriesStartAtkValueIndex + i;
            if (atkIdx >= addon->AtkValuesCount) break;
            var v = addon->AtkValues[atkIdx];
            var ptr = (byte*)v.String.Value;
            if (ptr == null) continue;
            var text = Marshal.PtrToStringUTF8((nint)ptr);
            if (text != null && text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static unsafe void CloseSaleHistoryAddon()
    {
        var addonPtr = Service.GameGui.GetAddonByName(SaleHistoryAddon);
        if (addonPtr == nint.Zero) return;
        var addon = (AtkUnitBase*)addonPtr.Address;
        if (addon == null) return;
        addon->Close(true);
    }

    private void FinishSafely()
    {
        try { ipc.FinishRetainerPostProcess(); }
        catch (Exception e) { Service.PluginLog.Warning($"[DayTrader/AutoCycle] FinishRetainerPostProcess failed: {e.Message}"); }
    }
}
