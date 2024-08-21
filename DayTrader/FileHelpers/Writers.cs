using CsvHelper;
using DayTrader.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader.FileHelpers
{
    internal static class Writers
    {
        internal static void WriteItemsToCsv(List<SaleHistoryItem> items)
        {
            // TODO: Could potentially rewrite this to do something like `if (!dedupedItems.add) { add to append list and just write that }`
            HashSet<SaleHistoryItem> dedupedItems = [];
            var filePath = Path.Join(Service.PluginInterface.ConfigDirectory.FullName, "salehistory.csv");
            StreamWriter? stream;
            if (File.Exists(filePath))
            {
                var existingItems = Readers.ReadItemsFromCsv();
                foreach (var item in existingItems)
                {
                    dedupedItems.Add(item);
                }
            }
            stream = new StreamWriter(File.Open(filePath, FileMode.OpenOrCreate));
            foreach (var item in items)
            {
                dedupedItems.Add(
                    new SaleHistoryItem
                    {
                        BuyerName = item.BuyerName,
                        ItemId = item.ItemId,
                        PricePerUnitSold = item.PricePerUnitSold,
                        Quantity = item.Quantity,
                        SaleDate = item.SaleDate,
                        TotalPrice = item.TotalPrice
                    }
                );
            }
            var writer = new CsvWriter(stream, CultureInfo.InvariantCulture);
            writer.WriteRecords(dedupedItems);
            writer.Flush();
            writer.Dispose();
        }
    }
}
