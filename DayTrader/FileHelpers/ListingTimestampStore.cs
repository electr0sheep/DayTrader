using DayTrader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DayTrader.FileHelpers
{
    internal class ListingTimestampStore
    {
        private const string FileName = "marketboard_listings.json";

        private readonly object gate = new();
        private List<RetainerListingTimestamp> entries = new();

        private static string FilePath =>
            Path.Join(Service.PluginInterface.ConfigDirectory.FullName, FileName);

        public void Load()
        {
            lock (gate)
            {
                try
                {
                    if (!File.Exists(FilePath))
                    {
                        entries = new List<RetainerListingTimestamp>();
                        return;
                    }
                    var json = File.ReadAllText(FilePath);
                    entries = JsonSerializer.Deserialize<List<RetainerListingTimestamp>>(json)
                              ?? new List<RetainerListingTimestamp>();
                }
                catch (Exception e)
                {
                    Service.PluginLog.Error($"[ListingTimestampStore] Load failed: {e.Message}");
                    entries = new List<RetainerListingTimestamp>();
                }
            }
        }

        public void Save()
        {
            lock (gate)
            {
                try
                {
                    var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(FilePath, json);
                }
                catch (Exception e)
                {
                    Service.PluginLog.Error($"[ListingTimestampStore] Save failed: {e.Message}");
                }
            }
        }

        // New listing: a slot that was empty (or had a different item) now holds this item.
        // Both CreatedAt and ModifiedAt reset to now.
        public void RecordNewListing(ulong retainerId, string? retainerName, short slot, uint itemId, uint price)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            lock (gate)
            {
                entries.RemoveAll(e => e.RetainerId == retainerId && e.SlotIndex == slot);
                entries.Add(new RetainerListingTimestamp
                {
                    RetainerId = retainerId,
                    RetainerName = retainerName,
                    SlotIndex = slot,
                    ItemId = itemId,
                    Price = price,
                    CreatedAt = now,
                    ModifiedAt = now,
                });
            }
            Save();
        }

        // Price adjustment on an existing listing: only ModifiedAt advances.
        // If we don't have an entry yet (plugin installed after listing went up),
        // create one with CreatedAt == ModifiedAt so the first observable age starts now.
        public void RecordPriceUpdate(ulong retainerId, string? retainerName, short slot, uint itemId, uint price)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            lock (gate)
            {
                var entry = FindLocked(retainerId, slot);
                if (entry != null)
                {
                    entry.RetainerName ??= retainerName;
                    if (itemId != 0) entry.ItemId = itemId;
                    entry.Price = price;
                    entry.ModifiedAt = now;
                }
                else
                {
                    entries.Add(new RetainerListingTimestamp
                    {
                        RetainerId = retainerId,
                        RetainerName = retainerName,
                        SlotIndex = slot,
                        ItemId = itemId,
                        Price = price,
                        CreatedAt = now,
                        ModifiedAt = now,
                    });
                }
            }
            Save();
        }

        // Seller pulled a listing back from the market. No sale can match this entry,
        // so drop it. Caller has already verified that the slot is empty post-withdrawal;
        // partial withdrawals are not routed here.
        public void RemoveListing(ulong retainerId, short slot)
        {
            bool changed;
            lock (gate)
            {
                var removed = entries.RemoveAll(e => e.RetainerId == retainerId && e.SlotIndex == slot);
                changed = removed > 0;
            }
            if (changed) Save();
        }

        public RetainerListingTimestamp? Get(ulong retainerId, short slot)
        {
            lock (gate)
            {
                return FindLocked(retainerId, slot);
            }
        }

        private RetainerListingTimestamp? FindLocked(ulong retainerId, short slot)
        {
            foreach (var e in entries)
            {
                if (e.RetainerId == retainerId && e.SlotIndex == slot) return e;
            }
            return null;
        }
    }
}
