using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DayTrader.Models.Saddlebags
{
    /// <summary>
    /// A model outlining stats about specific stack sizes sold in the last week from Saddlebags.
    /// </summary>
    internal class StackChance
    {
        /// <summary>
        /// The size of the stack.
        /// </summary>
        [JsonPropertyName("stack_size")]
        public required uint StackSize { get; set; }

        /// <summary>
        /// The number of sales in the last week for this stack size.
        /// </summary>
        [JsonPropertyName("number_of_sales")]
        public required uint NumberOfSales { get; set; }

        /// <summary>
        /// The percent of sales in the last week this stack represents (not units sold).
        /// </summary>
        [JsonPropertyName("percent_of_sales")]
        public required float PercentOfSales { get; set; }

        /// <summary>
        /// The percent of sales in the last week this stack represents.
        /// If only two sales were made, one with a single unit and one with 9 units,
        /// This metric would be 90% for the 9 unit stack.
        /// </summary>
        [JsonPropertyName("percent_of_total_quantity_sold")]
        public required float PercentOfSalesPerUnit { get; set; }

        /// <summary>
        /// I don't think this is used, it was 0 for everything.
        /// </summary>
        //[JsonPropertyName("average_price_for_size")]
        //public required uint AveragePriceForSize { get; set; }
    }
}
