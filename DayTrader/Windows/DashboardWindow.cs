using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using Dalamud.Interface.Windowing;
using DayTrader;
using DayTrader.FileHelpers;
using DayTrader.Models;
using Lumina.Excel.Sheets;

namespace Plugin.Windows;

public class DashboardWindow : Window, IDisposable
{
    private const char GilSymbol = '';

    private readonly Plugin plugin;

    private List<SaleHistoryItem> rawItems = new();
    private string? loadError;

    private readonly Dictionary<ushort, string> itemNameCache = new();

    private float totalGil;
    private uint unitsSold;
    private int distinctItems;
    private float gilPerDay;
    private int rangeDays;

    private List<ItemRollup> itemRows = new();
    private float[] chartXs = Array.Empty<float>();
    private float[] chartYs = Array.Empty<float>();
    private bool chartFitDirty = true;

    public DashboardWindow(Plugin plugin) : base("Day Trader Dashboard", ImGuiWindowFlags.None)
    {
        this.plugin = plugin;
        Size = new Vector2(820, 640);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void OnOpen()
    {
        ReloadCsv();
    }

    private void ReloadCsv()
    {
        loadError = null;
        try
        {
            var filePath = Path.Join(Service.PluginInterface.ConfigDirectory.FullName, "salehistory.csv");
            if (!File.Exists(filePath))
            {
                rawItems = new();
            }
            else
            {
                rawItems = Readers.ReadItemsFromCsv();
            }
        }
        catch (Exception e)
        {
            loadError = e.Message;
            rawItems = new();
        }
        Recompute();
    }

    private void Recompute()
    {
        var (start, end) = ResolveRange(plugin.Configuration.DashboardTimeRange);
        var filtered = rawItems
            .Where(i => {
                var t = i.SaleDateTime();
                return t >= start && t <= end;
            })
            .ToList();

        totalGil = 0f;
        unitsSold = 0;
        var perItem = new Dictionary<ushort, ItemRollup>();
        foreach (var s in filtered)
        {
            totalGil += s.TotalPrice;
            unitsSold += s.Quantity;

            if (!perItem.TryGetValue(s.ItemId, out var row))
            {
                row = new ItemRollup { Name = ResolveItemName(s.ItemId) };
                perItem[s.ItemId] = row;
            }
            row.TotalGil += s.TotalPrice;
            row.Units += s.Quantity;
            row.Sales++;
            var t = s.SaleDateTime();
            if (t > row.LastSold) row.LastSold = t;
        }

        foreach (var row in perItem.Values)
        {
            row.AvgPpu = row.Units == 0 ? 0f : row.TotalGil / row.Units;
        }

        distinctItems = perItem.Count;
        itemRows = perItem.Values.OrderByDescending(r => r.TotalGil).ToList();

        // For AllTime, clamp the effective span to actual data so the chart and
        // gil/day stat reflect the real history instead of "since year 1."
        var effectiveStart = start;
        if (plugin.Configuration.DashboardTimeRange == DashboardTimeRange.AllTime && filtered.Count > 0)
        {
            effectiveStart = filtered.Min(i => i.SaleDateTime());
        }

        rangeDays = Math.Max(1, (int)Math.Ceiling((end - effectiveStart).TotalDays));
        gilPerDay = totalGil / rangeDays;

        BuildChart(filtered, effectiveStart, end);
        chartFitDirty = true;
    }

    private void BuildChart(List<SaleHistoryItem> filtered, DateTime start, DateTime end)
    {
        if (filtered.Count == 0)
        {
            chartXs = Array.Empty<float>();
            chartYs = Array.Empty<float>();
            return;
        }

        var range = plugin.Configuration.DashboardTimeRange;
        if (range == DashboardTimeRange.Today)
        {
            var buckets = new float[24];
            foreach (var s in filtered)
            {
                var h = s.SaleDateTime().ToLocalTime().Hour;
                if (h >= 0 && h < 24) buckets[h] += s.TotalPrice;
            }
            chartYs = buckets;
            chartXs = Enumerable.Range(0, 24).Select(i => (float)i).ToArray();
        }
        else
        {
            var startDay = start.Date;
            var dayCount = Math.Max(1, (int)Math.Ceiling((end.Date - startDay).TotalDays) + 1);
            var buckets = new float[dayCount];
            foreach (var s in filtered)
            {
                var d = s.SaleDateTime().ToLocalTime().Date;
                var idx = (int)(d - startDay).TotalDays;
                if (idx >= 0 && idx < dayCount) buckets[idx] += s.TotalPrice;
            }
            chartYs = buckets;
            chartXs = Enumerable.Range(0, dayCount)
                .Select(i => (float)new DateTimeOffset(startDay.AddDays(i)).ToUnixTimeSeconds())
                .ToArray();
        }
    }

    private static (DateTime start, DateTime end) ResolveRange(DashboardTimeRange range)
    {
        var now = DateTime.Now;
        return range switch
        {
            DashboardTimeRange.Today => (now.Date, now),
            DashboardTimeRange.Last7Days => (now.Date.AddDays(-6), now),
            DashboardTimeRange.Last30Days => (now.Date.AddDays(-29), now),
            _ => (DateTime.MinValue, now),
        };
    }

    private string ResolveItemName(ushort itemId)
    {
        if (itemNameCache.TryGetValue(itemId, out var cached)) return cached;
        string? name = null;
        try
        {
            var sheet = Service.DataManager.GetExcelSheet<Item>();
            if (sheet != null)
            {
                name = sheet.GetRow(itemId).Name.ToString();
            }
        }
        catch
        {
            // row missing or sheet not loaded — fall through to placeholder
        }
        if (string.IsNullOrEmpty(name)) name = $"#{itemId}";
        itemNameCache[itemId] = name;
        return name;
    }

    public override void Draw()
    {
        DrawHeader();
        DrawHeadlineStats();
        DrawEarningsChart();
        DrawTopItemsTable();
    }

    private void DrawHeader()
    {
        Service.FontManager.H1.Push();
        ImGui.Text("Sale History");
        Service.FontManager.H1.Pop();

        var current = plugin.Configuration.DashboardTimeRange;
        if (DrawRangeButton("All time", DashboardTimeRange.AllTime, current)) RangeChanged(DashboardTimeRange.AllTime);
        ImGui.SameLine();
        if (DrawRangeButton("30d", DashboardTimeRange.Last30Days, current)) RangeChanged(DashboardTimeRange.Last30Days);
        ImGui.SameLine();
        if (DrawRangeButton("7d", DashboardTimeRange.Last7Days, current)) RangeChanged(DashboardTimeRange.Last7Days);
        ImGui.SameLine();
        if (DrawRangeButton("Today", DashboardTimeRange.Today, current)) RangeChanged(DashboardTimeRange.Today);

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(24f, 0f));
        ImGui.SameLine();
        if (ImGui.Button("Refresh")) ReloadCsv();

        if (loadError != null)
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), $"Failed to read sale history: {loadError}");
        }
    }

    private static bool DrawRangeButton(string label, DashboardTimeRange range, DashboardTimeRange current)
    {
        var selected = range == current;
        if (selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
        var clicked = ImGui.Button(label);
        if (selected) ImGui.PopStyleColor();
        return clicked && !selected;
    }

    private void RangeChanged(DashboardTimeRange range)
    {
        plugin.Configuration.DashboardTimeRange = range;
        plugin.Configuration.Save();
        Recompute();
    }

    private void DrawHeadlineStats()
    {
        Service.FontManager.H2.Push();
        ImGuiExtensions.SeparatorText(RangeLabel(plugin.Configuration.DashboardTimeRange));
        Service.FontManager.H2.Pop();

        if (rawItems.Count == 0 && loadError == null)
        {
            ImGui.TextDisabled("No sales recorded yet. Open the retainer sale history in-game to start capturing data.");
            return;
        }

        if (ImGui.BeginTable("dashboard-headline", 4, ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableNextRow();
            DrawStatCell("Total earned", $"{totalGil:N0}{GilSymbol}");
            DrawStatCell("Units sold", $"{unitsSold:N0}");
            DrawStatCell("Gil / day", $"{gilPerDay:N0}{GilSymbol}");
            DrawStatCell("Items sold", $"{distinctItems:N0}");
            ImGui.EndTable();
        }
    }

    private static void DrawStatCell(string label, string value)
    {
        ImGui.TableNextColumn();
        ImGui.TextDisabled(label);
        Service.FontManager.H2.Push();
        ImGui.Text(value);
        Service.FontManager.H2.Pop();
    }

    private static string RangeLabel(DashboardTimeRange r) => r switch
    {
        DashboardTimeRange.Today => "Today",
        DashboardTimeRange.Last7Days => "Last 7 days",
        DashboardTimeRange.Last30Days => "Last 30 days",
        _ => "All time",
    };

    private void DrawEarningsChart()
    {
        Service.FontManager.H2.Push();
        ImGuiExtensions.SeparatorText("Earnings over time");
        Service.FontManager.H2.Pop();

        if (chartYs.Length == 0)
        {
            ImGui.TextDisabled("No sales in range.");
            return;
        }

        if (ImPlot.BeginPlot("##earnings-over-time", new Vector2(-1, 220), ImPlotFlags.NoLegend))
        {
            var yMax = 0.0;
            foreach (var v in chartYs) if (v > yMax) yMax = v;
            if (yMax <= 0) yMax = 1.0;
            yMax *= 1.05; // small headroom above the tallest bar

            var fitCond = chartFitDirty ? ImPlotCond.Always : ImPlotCond.Once;

            ImPlot.SetupAxisLimitsConstraints(ImAxis.Y1, 0, double.MaxValue);
            ImPlot.SetupAxisFormat(ImAxis.Y1, KMFormatter);

            var isHourly = plugin.Configuration.DashboardTimeRange == DashboardTimeRange.Today;
            if (isHourly)
            {
                ImPlot.SetupAxes("", "", ImPlotAxisFlags.None, ImPlotAxisFlags.None);
                ImPlot.SetupAxisLimits(ImAxis.X1, -0.5, 23.5, fitCond);
                ImPlot.SetupAxisLimits(ImAxis.Y1, 0, yMax, fitCond);
                ImPlot.PlotBars("Earnings", ref chartYs[0], chartYs.Length);
            }
            else
            {
                ImPlot.SetupAxisScale(ImAxis.X1, ImPlotScale.Time);
                ImPlot.SetupAxes("", "", ImPlotAxisFlags.None, ImPlotAxisFlags.None);
                ImPlot.SetupAxisLimits(ImAxis.X1, chartXs[0], chartXs[chartXs.Length - 1] + 86400.0, fitCond);
                ImPlot.SetupAxisLimits(ImAxis.Y1, 0, yMax, fitCond);
                var barWidth = 86400.0 * 0.85;
                ImPlot.PlotBars("Earnings", ref chartXs[0], ref chartYs[0], chartYs.Length, barWidth);
            }
            ImPlot.EndPlot();
            chartFitDirty = false;
        }
    }

    // Held in a static field so the GC can't collect it while ImPlot retains the function pointer.
    private static readonly unsafe ImPlotFormatter KMFormatter = KMFormatCallback;

    private static unsafe int KMFormatCallback(double value, byte* buff, int size, void* userData)
    {
        var s = FormatKM(value);
        var bytes = Encoding.UTF8.GetBytes(s);
        var copyLen = Math.Min(bytes.Length, size - 1);
        for (int i = 0; i < copyLen; i++) buff[i] = bytes[i];
        buff[copyLen] = 0;
        return copyLen;
    }

    private static string FormatKM(double v)
    {
        var abs = Math.Abs(v);
        if (abs >= 1_000_000) return $"{v / 1_000_000.0:0.##}m";
        if (abs >= 1_000) return $"{v / 1_000.0:0.##}k";
        return $"{v:0}";
    }

    private void DrawTopItemsTable()
    {
        Service.FontManager.H2.Push();
        ImGuiExtensions.SeparatorText($"Items ({itemRows.Count})");
        Service.FontManager.H2.Pop();

        if (itemRows.Count == 0)
        {
            ImGui.TextDisabled("No items in range.");
            return;
        }

        const ImGuiTableFlags tableFlags =
            ImGuiTableFlags.Sortable
            | ImGuiTableFlags.RowBg
            | ImGuiTableFlags.BordersOuter
            | ImGuiTableFlags.BordersV
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.ScrollY;

        var tableHeight = Math.Min(420f, ImGui.GetContentRegionAvail().Y);
        if (ImGui.BeginTable("dashboard-top-items", 6, tableFlags, new Vector2(-1, tableHeight)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch, 0f, (uint)ItemSortColumn.Name);
            ImGui.TableSetupColumn("Total gil", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending, 0f, (uint)ItemSortColumn.TotalGil);
            ImGui.TableSetupColumn("Units", ImGuiTableColumnFlags.PreferSortDescending, 0f, (uint)ItemSortColumn.Units);
            ImGui.TableSetupColumn("Avg PPU", ImGuiTableColumnFlags.PreferSortDescending, 0f, (uint)ItemSortColumn.AvgPpu);
            ImGui.TableSetupColumn("Sales", ImGuiTableColumnFlags.PreferSortDescending, 0f, (uint)ItemSortColumn.Sales);
            ImGui.TableSetupColumn("Last sold", ImGuiTableColumnFlags.PreferSortDescending, 0f, (uint)ItemSortColumn.LastSold);
            ImGui.TableHeadersRow();

            ApplySort();

            for (int i = 0; i < itemRows.Count; i++)
            {
                var row = itemRows[i];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(row.Name);
                ImGui.TableNextColumn();
                ImGui.Text($"{row.TotalGil:N0}{GilSymbol}");
                ImGui.TableNextColumn();
                ImGui.Text($"{row.Units:N0}");
                ImGui.TableNextColumn();
                ImGui.Text($"{row.AvgPpu:N0}{GilSymbol}");
                ImGui.TableNextColumn();
                ImGui.Text($"{row.Sales:N0}");
                ImGui.TableNextColumn();
                ImGui.Text(FormatRelative(row.LastSold));
            }
            ImGui.EndTable();
        }
    }

    private void ApplySort()
    {
        var specs = ImGui.TableGetSortSpecs();
        if (!specs.SpecsDirty || specs.SpecsCount == 0) return;

        var spec = specs.Specs;
        var col = (ItemSortColumn)spec.ColumnUserID;
        var ascending = spec.SortDirection == ImGuiSortDirection.Ascending;

        IEnumerable<ItemRollup> sorted = col switch
        {
            ItemSortColumn.Name => ascending
                ? itemRows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                : itemRows.OrderByDescending(r => r.Name, StringComparer.OrdinalIgnoreCase),
            ItemSortColumn.Units => ascending
                ? itemRows.OrderBy(r => r.Units)
                : itemRows.OrderByDescending(r => r.Units),
            ItemSortColumn.AvgPpu => ascending
                ? itemRows.OrderBy(r => r.AvgPpu)
                : itemRows.OrderByDescending(r => r.AvgPpu),
            ItemSortColumn.Sales => ascending
                ? itemRows.OrderBy(r => r.Sales)
                : itemRows.OrderByDescending(r => r.Sales),
            ItemSortColumn.LastSold => ascending
                ? itemRows.OrderBy(r => r.LastSold)
                : itemRows.OrderByDescending(r => r.LastSold),
            _ => ascending
                ? itemRows.OrderBy(r => r.TotalGil)
                : itemRows.OrderByDescending(r => r.TotalGil),
        };

        itemRows = sorted.ToList();
        specs.SpecsDirty = false;
    }

    private enum ItemSortColumn : uint
    {
        Name = 1,
        TotalGil = 2,
        Units = 3,
        AvgPpu = 4,
        Sales = 5,
        LastSold = 6,
    }

    private static string FormatRelative(DateTime t)
    {
        if (t == DateTime.MinValue) return "—";
        var delta = DateTime.UtcNow - t.ToUniversalTime();
        if (delta.TotalSeconds < 60) return "just now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
        if (delta.TotalDays < 30) return $"{(int)delta.TotalDays}d ago";
        return t.ToLocalTime().ToString("yyyy-MM-dd");
    }

    private class ItemRollup
    {
        public string Name = "";
        public float TotalGil;
        public uint Units;
        public uint Sales;
        public float AvgPpu;
        public DateTime LastSold = DateTime.MinValue;
    }
}
