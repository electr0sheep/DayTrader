using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader.Models
{
    internal class SaleHistoryItem
    {
        public ushort ItemId { get; set; }
        public uint SalePrice { get; set; }
        public uint SaleDate { get; set; }
        public string? BuyerName { get; set; }

        public DateTime SaleDateTime()
        {
            return DateTime.UnixEpoch.AddSeconds(SaleDate);
        }

        public override bool Equals(object? obj)
        {
            if (obj is SaleHistoryItem other)
            {
                return ItemId == other.ItemId && SalePrice == other.SalePrice && SaleDate == other.SaleDate && string.Equals(BuyerName, other.BuyerName);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ItemId, SalePrice, SaleDate, BuyerName);
        }
    }
}
