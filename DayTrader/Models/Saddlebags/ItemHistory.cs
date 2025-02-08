// Adapted from https://github.com/fmauNeko/MarketBoardPlugin/blob/develop/MarketBoardPlugin/Models/Universalis/MarketDataResponse.cs

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DayTrader.Models.Saddlebags
{
    /// <summary>
    /// A model representing item history from Saddlebags.
    /// </summary>
    internal class ItemHistory
    {
        /// <summary>
        /// The item ID.
        /// </summary>
        [JsonPropertyName("itemID")]
        public required uint ItemId { get; set; }

        /// <summary>
        /// The total number of purchases for the last week.
        /// </summary>
        [JsonPropertyName("total_purchase_amount")]
        public required uint TotalPurchaseAmount { get; set; }

        /// <summary>
        /// The total quantity sold for the last week.
        /// </summary>
        [JsonPropertyName("total_quantity_sold")]
        public required uint TotalQuantitySold { get; set; }

        /// <summary>
        /// The average quantity sold for the last week.
        /// </summary>
        [JsonPropertyName("average_quantity_sold_per_day")]
        public required uint AverageQuantitySoldPerDay { get; set; }

        /// <summary>
        /// The average number of purchaes per day for the last week.
        /// </summary>
        [JsonPropertyName("average_sales_per_day")]
        public required uint AverageSalesPerDay { get; set; }

        /// <summary>
        /// The median price per unit sold for the last week.
        /// </summary>
        [JsonPropertyName("median_ppu")]
        public required uint MedianPPU { get; set; }

        /// <summary>
        /// The average price per unit sold for the last week.
        /// </summary>
        [JsonPropertyName("average_ppu")]
        public required uint AveragePPU { get; set; }

        /// <summary>
        /// An array of price ranges and how many in that range sold per week.
        /// </summary>
        [JsonPropertyName("price_history")]
        public required PriceHistory[] PriceHistory { get; set; }

        /// <summary>
        /// An array of stack sizes with stats about each stack size.
        /// </summary>
        [JsonPropertyName("stack_chance")]
        public required StackChance[] StackChance { get; set; }

        /// <summary>
        /// Suspect sales.
        /// </summary>
        [JsonPropertyName("dirty_sales")]
        public required DirtySale[] DirtySale { get; set; }

        /// <summary>
        /// The number of sales per hour for the last week for the home server.
        /// </summary>
        [JsonPropertyName("home_server_sales_by_hour_chart")]
        public required SalesByHour[] SalesByHour { get; set; }

        /// <summary>
        /// The number of sales per server per week.
        /// </summary>
        [JsonPropertyName("server_distribution")]
        public required ServerDistribution ServerDistribution { get; set; }
    }
}
