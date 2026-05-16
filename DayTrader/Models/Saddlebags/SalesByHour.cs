using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DayTrader.Models.Saddlebags
{
    /// <summary>
    /// A model representing sales per hour from Saddlebags.
    /// </summary>
    internal class SalesByHour
    {
        /// <summary>
        /// The begin time for the number of sales in this hour.
        /// </summary>
        [JsonPropertyName("time")]
        public required uint Time { get; set; }

        /// <summary>
        /// The number of sales during this hour.
        /// </summary>
        [JsonPropertyName("sale_amt")]
        public required uint SaleAmt { get; set; }
    }
}
