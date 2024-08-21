using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader.Models
{
    internal class SaleHistoryItem
    {
        public string? BuyerName { get; set; }
        public ushort ItemId { get; set; }
        public uint PricePerUnitSold { get; set; }
        public ushort Quantity { get; set; }
        public float TotalPrice { get; set; }
        public uint SaleDate { get; set; }

        public DateTime SaleDateTime()
        {
            return DateTime.UnixEpoch.AddSeconds(SaleDate);
        }

        public override bool Equals(object? obj)
        {
            if (obj is SaleHistoryItem other)
            {
                return ItemId == other.ItemId && PricePerUnitSold == other.PricePerUnitSold && SaleDate == other.SaleDate && string.Equals(BuyerName, other.BuyerName);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ItemId, PricePerUnitSold, SaleDate, BuyerName);
        }
    }
}
