using System;

namespace DayTrader.Models
{
    internal class RetainerListingTimestamp
    {
        public ulong RetainerId { get; set; }
        public string? RetainerName { get; set; }
        public short SlotIndex { get; set; }
        public uint ItemId { get; set; }
        public uint Price { get; set; }
        public long CreatedAt { get; set; }
        public long ModifiedAt { get; set; }

        public DateTime CreatedAtDateTime() => DateTime.UnixEpoch.AddSeconds(CreatedAt);
        public DateTime ModifiedAtDateTime() => DateTime.UnixEpoch.AddSeconds(ModifiedAt);
    }
}
