using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader.Interop
{
    [StructLayout(LayoutKind.Explicit, Size = 0x29C)]
    internal unsafe struct SaleHistory
    {
        [FieldOffset(0x08)] private byte saleHistoryItems;

        public ReadOnlySpan<SaleHistoryItem> ItemList()
        {
            fixed (byte* p = &saleHistoryItems)
            {
                return new(p, 20);
            }
        }

        //public ReadOnlySpan<SaleHistoryItem> ItemList => new(new IntPtr(&SaleHistoryItems), 20);
        //fixed (byte* p = &SaleHistoryItems)
        //{

        //}

        //public ReadOnlySpan<SaleHistoryItem> ItemList => fixed (new(&SaleHistoryItem, 20);
    }
}
