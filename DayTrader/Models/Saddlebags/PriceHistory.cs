using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DayTrader.Models.Saddlebags
{
    /// <summary>
    /// A model representing price history from Saddlebags.
    /// </summary>
    internal class PriceHistory
    {
        /// <summary>
        /// The price range.
        /// </summary>
        [JsonPropertyName("price_range")]
        public required string PriceRange { get; set; }

        /// <summary>
        /// The number of sales in the last week in the price range.
        /// </summary>
        [JsonPropertyName("sales_amount")]
        public required uint SalesAmount { get; set; }
    }
}
