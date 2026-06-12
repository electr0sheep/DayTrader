using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Plugin;

namespace DayTrader.Addons;

// Adds an "Age" column to the RetainerSellList addon. Header text is attached
// inside the column-header host (#5); per-row age text is attached inside each
// row component (children of the list #11), so it rides with the row when the
// list scrolls or rebinds. Age data comes from ListingTimestampStore, populated
// by the InventoryManager hooks in Plugin.cs.
internal unsafe class RetainerSellListColumn : IDisposable
{
    private const string AddonName = "RetainerSellList";

    // Known addon node IDs (provided by user; confirmed against the tree dump).
    private const uint ListCountNodeId = 19;
    private const uint XButtonNodeId = 7;
    private const uint HeaderHostNodeId = 5;
    private const uint ListNodeId = 11;
    private const uint MainBodyNodeId = 20;
    private const uint ScrollbarNodeId = 2;
    private const ushort RowComponentType = 1011;

    // Top-level nodes that span ~addon width and need to widen with us.
    private static readonly uint[] WidenWindowComponentNodeIds = { 2, 8, 9, 10, 11, 12 };
    private static readonly uint[] WidenTopLevelIds = { 2, 5, 10, 11, 12, 13, 14, 20 };
    // Top-level nodes anchored toward the right edge that need to shift X.
    private const uint TitleBarNodeId = 2;

    // Our injected node IDs — high range to avoid colliding with game IDs.
    private const uint HeaderNodeId = 0xDA7A0001;
    private const uint RowNodeIdBase = 0xDA7A0100;
    private const int MaxRows = 20;

    private const short ColumnX = 570;
    private const short RowYInRow = 5;
    private const ushort ColumnWidth = 80;

    private readonly Plugin.Plugin plugin;
    private AtkTextNode* headerNode;
    private readonly AtkTextNode*[] rowNodes = new AtkTextNode*[MaxRows];
    private readonly AtkComponentNode*[] rowOwners = new AtkComponentNode*[MaxRows];
    private bool columnApplied;
    private bool diagDumped;
    private long lastPeriodicRefreshUnix;
    private const long PeriodicRefreshIntervalSeconds = 60;

    public RetainerSellListColumn(Plugin.Plugin plugin)
    {
        this.plugin = plugin;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, AddonName, OnSetup);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, AddonName, OnRefresh);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, AddonName, OnUpdate);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, AddonName, OnFinalize);
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, AddonName, OnSetup);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, AddonName, OnRefresh);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, AddonName, OnUpdate);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, AddonName, OnFinalize);
    }

    // Called by the overlay's Enable checkbox handler. Looks at the current
    // addon (if open) and the toggle state, and applies or removes the column
    // so the change is visible immediately without needing to reopen the addon.
    public void Refresh()
    {
        var addonPtr = Service.GameGui.GetAddonByName(AddonName);
        if (addonPtr == nint.Zero) return;
        var addon = (AtkUnitBase*)addonPtr.Address;
        if (addon == null) return;

        if (plugin.Configuration.Enabled) ApplyColumn(addon);
        else RemoveColumn(addon);
    }

    private void OnSetup(AddonEvent type, AddonArgs args)
    {
        if (!plugin.Configuration.Enabled) return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || addon->RootNode == null) return;

        ApplyColumn(addon);
    }

    private void OnFinalize(AddonEvent type, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        RemoveColumn(addon);
    }

    // Fires after the addon refreshes its data — i.e., when the user lists a new item,
    // adjusts a price, or sells an item. AtkValues are already up to date at this point,
    // so we just re-resolve and rewrite each existing row's age text in place.
    private void OnRefresh(AddonEvent type, AddonArgs args)
    {
        if (!plugin.Configuration.Enabled) return;
        if (!columnApplied) return;
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;
        UpdateAgeTexts(addon);
        lastPeriodicRefreshUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    // Fires every UI frame. Cheap throttle: only re-resolve age texts once a minute,
    // so "2m" ticks over to "3m" without the user having to touch the addon.
    private void OnUpdate(AddonEvent type, AddonArgs args)
    {
        if (!plugin.Configuration.Enabled) return;
        if (!columnApplied) return;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - lastPeriodicRefreshUnix < PeriodicRefreshIntervalSeconds) return;
        lastPeriodicRefreshUnix = now;
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;
        UpdateAgeTexts(addon);
    }

    private void UpdateAgeTexts(AtkUnitBase* addon)
    {
        var rowSlots = BuildRowSlotMap(addon);
        var activeRetainerId = GetActiveRetainerId();
        for (var rowIdx = 0; rowIdx < MaxRows; rowIdx++)
        {
            if (rowNodes[rowIdx] == null) continue;
            var ageText = ResolveAgeText(activeRetainerId, rowSlots, rowIdx);
            rowNodes[rowIdx]->SetText(ageText);
        }
    }

    private void ApplyColumn(AtkUnitBase* addon)
    {
        if (columnApplied) return;
        if (addon == null || addon->RootNode == null) return;

        if (!diagDumped)
        {
            diagDumped = true;
            LogChildren("root", addon->RootNode);
            var hh = addon->GetNodeById(HeaderHostNodeId);
            if (hh != null) LogChildren("#5", hh);
            var body = addon->GetNodeById(MainBodyNodeId);
            if (body != null) LogChildren("#20", body);
            LogAtkValues(addon);
        }

        WidenAddonLayout(addon, (short)ColumnWidth);

        var headerHost = addon->GetNodeById(HeaderHostNodeId);
        if (headerHost != null)
        {
            headerNode = MakeTextNode("Age", HeaderNodeId, ColumnX, 0, ColumnWidth, 16, AlignmentType.Center, 12, new ByteColor { R = 0xD1, G = 0xD1, B = 0xD1, A = 0xFF });
            if (headerNode != null)
            {
                AppendToSiblingTail(headerHost, (AtkResNode*)headerNode);
                //AppendToNodeList(&addon->UldManager, (AtkResNode*)headerNode);
                addon->UldManager.UpdateDrawNodeList();
            }
        }

        var rowSlots = BuildRowSlotMap(addon);
        var activeRetainerId = GetActiveRetainerId();
        LogDiagnostics(activeRetainerId, rowSlots);

        var listNode = addon->GetNodeById(ListNodeId);
        if (listNode != null && (ushort)listNode->Type >= 1000)
        {
            var listComp = ((AtkComponentNode*)listNode)->Component;
            if (listComp != null)
            {
                var rowIdx = 0;
                for (var i = 0; i < listComp->UldManager.NodeListCount && rowIdx < MaxRows; i++)
                {
                    var entry = listComp->UldManager.NodeList[i];
                    if (entry == null || (ushort)entry->Type != RowComponentType) continue;

                    var rowCompNode = (AtkComponentNode*)entry;
                    var rowComp = rowCompNode->Component;
                    if (rowComp == null || rowComp->UldManager.RootNode == null) continue;

                    var row = rowComp->GetNodeById(4);
                    if (row == null) continue;

                    // Row columns have no gap between them — anchor our column
                    // flush to #4's right edge (which WidenAddonLayout already
                    // grew by ColumnWidth). Right-aligned text inside the box
                    // produces the visual breathing room.
                    var rowColumnX = (short)(row->Width - ColumnWidth);

                    LogRowTexts(rowIdx, rowSlots, row);
                    var ageText = ResolveAgeText(activeRetainerId, rowSlots, rowIdx);
                    var ageNode = MakeTextNode(ageText, RowNodeIdBase + (uint)rowIdx, rowColumnX, RowYInRow, ColumnWidth, 23, AlignmentType.Right, 14, new ByteColor { R = 0xD1, G = 0xD1, B = 0xD1, A = 0xFF });
                    if (ageNode == null) continue;

                    AppendToSiblingTail(row, (AtkResNode*)ageNode);
                    rowComp->UldManager.UpdateDrawNodeList();

                    rowNodes[rowIdx] = ageNode;
                    rowOwners[rowIdx] = rowCompNode;
                    rowIdx++;
                }
            }
        }

        columnApplied = true;
        lastPeriodicRefreshUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Service.PluginLog.Info("[AgeCol] column applied");
    }

    private void RemoveColumn(AtkUnitBase* addon)
    {
        if (!columnApplied) return;

        if (headerNode != null)
        {
            if (addon != null)
            {
                var host = addon->GetNodeById(HeaderHostNodeId);
                if (host != null)
                {
                    UnlinkFromSiblings(host, (AtkResNode*)headerNode);
                    RemoveFromNodeList(&addon->UldManager, (AtkResNode*)headerNode);
                    addon->UldManager.UpdateDrawNodeList();
                }
            }
            headerNode->AtkResNode.Destroy(false);
            IMemorySpace.Free(headerNode, (ulong)sizeof(AtkTextNode));
            headerNode = null;
        }

        for (var i = 0; i < MaxRows; i++)
        {
            if (rowNodes[i] != null)
            {
                if (rowOwners[i] != null && rowOwners[i]->Component != null && rowOwners[i]->Component->UldManager.RootNode != null)
                {
                    var comp = rowOwners[i]->Component;
                    UnlinkFromSiblings(comp->UldManager.RootNode, (AtkResNode*)rowNodes[i]);
                    RemoveFromNodeList(&comp->UldManager, (AtkResNode*)rowNodes[i]);
                    comp->UldManager.UpdateDrawNodeList();
                }
                rowNodes[i]->AtkResNode.Destroy(false);
                IMemorySpace.Free(rowNodes[i], (ulong)sizeof(AtkTextNode));
                rowNodes[i] = null;
            }
            rowOwners[i] = null;
        }

        if (addon != null) WidenAddonLayout(addon, (short)-ColumnWidth);

        columnApplied = false;
        Service.PluginLog.Info("[AgeCol] column removed");
    }

    private static AtkTextNode* MakeTextNode(string text, uint nodeId, short x, short y, ushort w, ushort h, AlignmentType alignmentType, byte fontSize, ByteColor textColor)
    {
        var node = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
        if (node == null) return null;
        IMemorySpace.Memset(node, 0, (ulong)sizeof(AtkTextNode));
        node->Ctor();

        node->AtkResNode.Type = NodeType.Text;
        node->AtkResNode.NodeId = nodeId;
        node->AtkResNode.SetPositionShort(x, y);
        node->AtkResNode.SetWidth(w);
        node->AtkResNode.SetHeight(h);
        node->AtkResNode.NodeFlags =
            NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents;

        node->AtkResNode.Color.A = 0xFF;
        node->AtkResNode.AddRed = 0;
        node->AtkResNode.AddGreen = 0;
        node->AtkResNode.AddBlue = 0;
        node->AtkResNode.MultiplyRed = 100;
        node->AtkResNode.MultiplyGreen = 100;
        node->AtkResNode.MultiplyBlue = 100;
        node->AtkResNode.ScaleX = 1f;
        node->AtkResNode.ScaleY = 1f;

        node->LineSpacing = 12;
        node->AlignmentFontType = (byte)alignmentType;
        node->FontSize = fontSize;
        node->TextFlags = TextFlags.Edge;
        node->TextColor = textColor;
        node->EdgeColor = new ByteColor { R = 0, G = 0, B = 0, A = 0xFF };

        node->SetText(text);
        return node;
    }

    private static void AppendToSiblingTail(AtkResNode* parent, AtkResNode* node)
    {
        node->ParentNode = parent;
        var firstChild = parent->ChildNode;
        if (firstChild == null)
        {
            parent->ChildNode = node;
        }
        else
        {
            var tail = firstChild;
            while (tail->PrevSiblingNode != null) tail = tail->PrevSiblingNode;
            tail->PrevSiblingNode = node;
            node->NextSiblingNode = tail;
        }
    }

    private static void AppendToNodeList(AtkUldManager* uld, AtkResNode* node)
    {
        var oldCount = uld->NodeListCount;
        var newList = (AtkResNode**)IMemorySpace.GetUISpace()->Malloc((ulong)(sizeof(nint) * (oldCount + 1)), 8);
        for (var i = 0; i < oldCount; i++) newList[i] = uld->NodeList[i];
        newList[oldCount] = node;
        uld->NodeList = newList;
        uld->NodeListCount = (ushort)(oldCount + 1);
    }

    private static void UnlinkFromSiblings(AtkResNode* parent, AtkResNode* node)
    {
        if (parent->ChildNode == node)
            parent->ChildNode = node->PrevSiblingNode;
        if (node->PrevSiblingNode != null)
            node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;
        if (node->NextSiblingNode != null)
            node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;
    }

    // Builds the displayed-row -> RetainerMarket-slot mapping by reading the addon's
    // AtkValues. The addon's row 0 slot lives at AtkValues[15], row 1 at [28], row 2 at
    // [41], etc. — i.e., index = 15 + rowIdx*13. This is the authoritative display-order
    // mapping pushed by the agent; container iteration order is a different ordering and
    // can't be used positionally.
    private const int AtkValueSlotBaseIndex = 15;
    private const int AtkValueSlotStride = 13;

    private static List<short> BuildRowSlotMap(AtkUnitBase* addon)
    {
        var slots = new List<short>(MaxRows);
        if (addon == null || addon->AtkValues == null) return slots;

        for (var row = 0; row < MaxRows; row++)
        {
            var idx = AtkValueSlotBaseIndex + row * AtkValueSlotStride;
            if (idx >= addon->AtkValuesCount) break;
            var v = &addon->AtkValues[idx];
            int raw;
            switch (v->Type)
            {
                case AtkValueType.Int:
                    raw = v->Int;
                    break;
                case AtkValueType.UInt:
                    raw = (int)v->UInt;
                    break;
                default:
                    return slots; // hit a non-numeric value — end of populated rows
            }
            if (raw < 0 || raw > 19) return slots; // sentinel / unused row
            slots.Add((short)raw);
        }
        return slots;
    }

    private static ulong GetActiveRetainerId()
    {
        var manager = RetainerManager.Instance();
        if (manager == null) return 0;
        var active = manager->GetActiveRetainer();
        return active != null ? active->RetainerId : manager->LastSelectedRetainerId;
    }

    private string ResolveAgeText(ulong retainerId, List<short> rowSlots, int rowIdx)
    {
        if (retainerId == 0 || rowIdx >= rowSlots.Count)
        {
            Service.PluginLog.Info($"[AgeCol/Resolve] row={rowIdx} -> no slot (retainerId={retainerId}, rowSlots.Count={rowSlots.Count})");
            return "—";
        }
        var slot = rowSlots[rowIdx];
        var entry = plugin.ListingTimestampStore.Get(retainerId, slot);
        if (entry == null)
        {
            Service.PluginLog.Info($"[AgeCol/Resolve] row={rowIdx} -> slot={slot}, NO STORE ENTRY for retainer={retainerId}");
            return "—";
        }
        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - entry.CreatedAt;
        Service.PluginLog.Info(
            $"[AgeCol/Resolve] row={rowIdx} -> slot={slot}, entry: itemId={entry.ItemId} price={entry.Price} modifiedAt={entry.ModifiedAt} ageSec={age}");
        return FormatAge(age);
    }

    // Dumps the active retainer, the container's filled slots (iterIdx, item->Slot, itemId, qty),
    // and our store entries for the active retainer. Lets us compare what we recorded vs the
    // container's actual state at display time.
    private void LogDiagnostics(ulong activeRetainerId, List<short> rowSlots)
    {
        Service.PluginLog.Info($"[AgeCol/Diag] active retainer={activeRetainerId}");
        var agentAddr = Plugin.Plugin.GetAgentRetainerAddress();
        Service.PluginLog.Info($"[AgeCol/Diag] AgentRetainer base = 0x{agentAddr.ToInt64():X}  (size 0x68D0)");

        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            Service.PluginLog.Info("[AgeCol/Diag] InventoryManager is null");
            return;
        }
        var container = inventoryManager->GetInventoryContainer(InventoryType.RetainerMarket);
        if (container == null)
        {
            Service.PluginLog.Info("[AgeCol/Diag] RetainerMarket container is null");
            return;
        }
        Service.PluginLog.Info($"[AgeCol/Diag] container size={container->Size}");
        for (short s = 0; s < (short)container->Size; s++)
        {
            var item = container->GetInventorySlot(s);
            if (item == null) continue;
            if (item->ItemId == 0) continue;
            Service.PluginLog.Info(
                $"[AgeCol/Diag] container iter={s} item->Slot={item->Slot} itemId={item->ItemId} qty={item->Quantity}");
        }

        Service.PluginLog.Info($"[AgeCol/Diag] rowSlots (in display order)=[{string.Join(",", rowSlots)}]");

        foreach (var slot in rowSlots)
        {
            var entry = plugin.ListingTimestampStore.Get(activeRetainerId, slot);
            if (entry == null)
            {
                Service.PluginLog.Info($"[AgeCol/Diag] store: slot={slot} -> (none)");
            }
            else
            {
                Service.PluginLog.Info(
                    $"[AgeCol/Diag] store: slot={slot} -> itemId={entry.ItemId} price={entry.Price} createdAt={entry.CreatedAt} modifiedAt={entry.ModifiedAt}");
            }
        }
    }

    // Dumps every descendant text node of a row's content host, so we can see what
    // the addon actually shows in this row position and compare against the slot
    // we mapped it to.
    private static void LogRowTexts(int rowIdx, List<short> rowSlots, AtkResNode* rowContent)
    {
        var mappedSlot = rowIdx < rowSlots.Count ? rowSlots[rowIdx].ToString() : "(none)";
        var texts = new List<string>();
        CollectRowTexts(rowContent, texts);
        Service.PluginLog.Info(
            $"[AgeCol/Row] row={rowIdx} mappedSlot={mappedSlot} texts=[{string.Join(" | ", texts)}]");
    }

    private static void CollectRowTexts(AtkResNode* node, List<string> result)
    {
        if (node == null) return;
        if (node->Type == NodeType.Text)
        {
            var raw = ((AtkTextNode*)node)->NodeText.ToString();
            // Strip control chars / non-printables for readability.
            var cleaned = new string(System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Where(raw, c => c >= 0x20 && c < 0x7F)));
            if (cleaned.Length > 0) result.Add($"x={node->X:F0}:'{cleaned}'");
        }
        var child = node->ChildNode;
        while (child != null)
        {
            CollectRowTexts(child, result);
            child = child->PrevSiblingNode;
        }
    }

    private static string FormatAge(long seconds)
    {
        if (seconds < 60) return "<1m";
        if (seconds < 3600) return $"{seconds / 60}m";
        if (seconds < 86400) return $"{seconds / 3600}h";
        var days = seconds / 86400;
        var hours = (seconds % 86400) / 3600;
        return hours > 0 ? $"{days}d {hours}h" : $"{days}d";
    }

    private static void LogChildren(string label, AtkResNode* parent)
    {
        Service.PluginLog.Info($"[Diag] {label} pos=({parent->X:F0},{parent->Y:F0}) size={parent->Width}x{parent->Height}");
        var child = parent->ChildNode;
        while (child != null)
        {
            if (child->NodeId != HeaderNodeId && !(child->NodeId >= RowNodeIdBase && child->NodeId < RowNodeIdBase + MaxRows))
            {
                Service.PluginLog.Info(
                    $"[Diag]   #{child->NodeId} type={child->Type} " +
                    $"pos=({child->X:F0},{child->Y:F0}) size={child->Width}x{child->Height}");
            }
            child = child->PrevSiblingNode;
        }
    }

    // Dumps every AtkValue in the addon to the log. AtkValues is the standardized
    // data channel from the agent to the addon — for list-style addons like this one,
    // each row's fields (item id, qty, price, slot, etc.) live here in display order.
    // We use this to find the offset/stride for reading slot per row.
    private static void LogAtkValues(AtkUnitBase* addon)
    {
        if (addon == null || addon->AtkValues == null)
        {
            Service.PluginLog.Info("[Diag/AtkValues] no AtkValues");
            return;
        }
        var count = addon->AtkValuesCount;
        Service.PluginLog.Info($"[Diag/AtkValues] count={count}");
        for (var i = 0; i < count; i++)
        {
            var v = &addon->AtkValues[i];
            var typeStr = v->Type.ToString();
            string valueStr;
            switch (v->Type)
            {
                case AtkValueType.Int:
                    valueStr = v->Int.ToString();
                    break;
                case AtkValueType.UInt:
                    valueStr = v->UInt.ToString();
                    break;
                case AtkValueType.Bool:
                    valueStr = v->Byte.ToString();
                    break;
                case AtkValueType.String:
                case AtkValueType.String8:
                case AtkValueType.ManagedString:
                    valueStr = v->String.HasValue
                        ? System.Runtime.InteropServices.Marshal.PtrToStringUTF8((nint)v->String.Value) ?? "(null)"
                        : "(null)";
                    if (valueStr.Length > 60) valueStr = valueStr[..60] + "...";
                    break;
                default:
                    valueStr = $"raw=0x{v->Int:X}";
                    break;
            }
            Service.PluginLog.Info($"[Diag/AtkValues] [{i,3}] {typeStr,-20} = {valueStr}");
        }
    }

    private static void RemoveFromNodeList(AtkUldManager* uld, AtkResNode* node)
    {
        var count = uld->NodeListCount;
        for (var i = 0; i < count; i++)
        {
            if (uld->NodeList[i] == node)
            {
                for (var j = i; j < count - 1; j++)
                    uld->NodeList[j] = uld->NodeList[j + 1];
                uld->NodeListCount--;
                return;
            }
        }
    }

    // Widens or shrinks the addon by `delta` so our column has somewhere to
    // live. Pass a negative delta to undo a previous widening.
    private static void WidenAddonLayout(AtkUnitBase* addon, short delta)
    {
        if (addon->RootNode != null)
            addon->RootNode->SetWidth((ushort)(addon->RootNode->Width + delta));

        foreach (var id in WidenTopLevelIds)
        {
            var n = addon->GetNodeById(id);
            if (n != null) n->SetWidth((ushort)(n->Width + delta));
        }

        var windowComponentNode = (AtkComponentWindow*)addon->GetComponentByNodeId(20);

        foreach (var id in WidenWindowComponentNodeIds)
        {
            var n = windowComponentNode->GetNodeById(id);
            if (n != null) n->SetWidth((ushort)(n->Width + delta));
        }

        // Shift the title-bar / close-button area so it stays at the right edge.
        // Walk root's direct children (not GetNodeById) to disambiguate from the
        // scrollbar inside the list, which also has NodeId 2.
        var xNode = windowComponentNode->GetNodeById(XButtonNodeId);
        xNode->SetXShort((short)(xNode->X + delta));

        var footerNode = addon->GetNodeById(ListCountNodeId);
        footerNode->SetXShort((short)(footerNode->X + delta));

        var listNode = addon->GetNodeById(ListNodeId);
        if (listNode == null) return;

        var listComp = ((AtkComponentNode*)listNode)->Component;
        if (listComp == null) return;

        for (var i = 0; i < listComp->UldManager.NodeListCount; i++)
        {
            var entry = listComp->UldManager.NodeList[i];
            if (entry == null) continue;

            if ((ushort)entry->Type == RowComponentType)
            {
                entry->SetWidth((ushort)(entry->Width + delta));
                var rowComp = ((AtkComponentNode*)entry)->Component;
                if (rowComp != null && rowComp->UldManager.RootNode != null)
                {
                    rowComp->UldManager.RootNode->SetWidth(
                        (ushort)(rowComp->UldManager.RootNode->Width + delta));
                }
                // Inner content host #4 — must grow/shrink with the row outer so
                // children we attached at ColumnX have somewhere to render.
                if (rowComp != null)
                {
                    var contentHost = rowComp->GetNodeById(4);
                    if (contentHost != null)
                        contentHost->SetWidth((ushort)(contentHost->Width + delta));
                    var highlight = rowComp->GetNodeById(13);
                    if (highlight != null)
                        highlight->SetWidth((ushort)(highlight->Width + delta + 20));
                }
            }
            else if (entry->NodeId == ScrollbarNodeId)
            {
                entry->SetXShort((short)(entry->X + delta));
            }
            else if (entry->Width > 500)
            {
                // List-wide background / separator rows — keep them filling.
                entry->SetWidth((ushort)(entry->Width + delta));
            }
        }
    }
}
