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
    internal static class Readers
    {
        internal static List<SaleHistoryItem> ReadItemsFromCsv()
        {
            List<SaleHistoryItem> items = [];
            var filePath = Path.Join(Service.PluginInterface.ConfigDirectory.FullName, "salehistory.csv");
            var reader = new CsvReader(new StreamReader(filePath), CultureInfo.InvariantCulture);
            items = reader.GetRecords<SaleHistoryItem>().ToList();
            reader.Dispose();
            return items;
        }
    }
}
