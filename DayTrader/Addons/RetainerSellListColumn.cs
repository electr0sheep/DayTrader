using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Plugin;

namespace DayTrader.Addons;

// Hello-world v2: adds an "Age" column to the RetainerSellList addon.
// Header text is attached inside the column-header host (#5); per-row "TODO"
// text is attached inside each row component (children of the list #11), so it
// rides with the row when the list scrolls or rebinds.
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
    private const int MaxRows = 12;

    private const short ColumnX = 570;
    private const short RowYInRow = 5;
    private const ushort ColumnWidth = 80;

    private readonly Plugin.Plugin plugin;
    private AtkTextNode* headerNode;
    private readonly AtkTextNode*[] rowNodes = new AtkTextNode*[MaxRows];
    private readonly AtkComponentNode*[] rowOwners = new AtkComponentNode*[MaxRows];
    private bool columnApplied;
    private bool diagDumped;

    public RetainerSellListColumn(Plugin.Plugin plugin)
    {
        this.plugin = plugin;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, AddonName, OnSetup);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, AddonName, OnFinalize);
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, AddonName, OnSetup);
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
        }

        WidenAddonLayout(addon, (short)ColumnWidth);

        var headerHost = addon->GetNodeById(HeaderHostNodeId);
        if (headerHost != null)
        {
            headerNode = MakeTextNode("Age", HeaderNodeId, ColumnX, 0, ColumnWidth, 16, AlignmentType.Center);
            if (headerNode != null)
            {
                AppendToSiblingTail(headerHost, (AtkResNode*)headerNode);
                //AppendToNodeList(&addon->UldManager, (AtkResNode*)headerNode);
                addon->UldManager.UpdateDrawNodeList();
            }
        }

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

                    var todo = MakeTextNode("TODO", RowNodeIdBase + (uint)rowIdx, rowColumnX, RowYInRow, ColumnWidth, 23, AlignmentType.Right);
                    if (todo == null) continue;

                    AppendToSiblingTail(row, (AtkResNode*)todo);
                    //AppendToNodeList(&rowComp->UldManager, (AtkResNode*)todo);
                    rowComp->UldManager.UpdateDrawNodeList();

                    rowNodes[rowIdx] = todo;
                    rowOwners[rowIdx] = rowCompNode;
                    rowIdx++;
                }
            }
        }

        columnApplied = true;
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

    private static AtkTextNode* MakeTextNode(string text, uint nodeId, short x, short y, ushort w, ushort h, AlignmentType alignmentType)
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
        node->FontSize = 12;
        node->TextFlags = TextFlags.Edge;
        node->TextColor = new ByteColor { R = 0xD1, G = 0xD1, B = 0xD1, A = 0xFF };
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
