using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 0x34)]
    internal unsafe struct SaleHistoryItem
    {
        [FieldOffset(0x00)] public ushort ItemId;
        [FieldOffset(0x04)] public uint SalePrice;
        [FieldOffset(0x08)] public uint Date;
        [FieldOffset(0x13)] private byte buyer;

        public string BuyerName()
        {
            fixed (byte* p = &buyer)
            {
                return Encoding.UTF8.GetString(p, 21);
            }
        }
    }
}
