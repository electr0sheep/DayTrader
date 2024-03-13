// Adapted from https://github.com/fmauNeko/MarketBoardPlugin/blob/develop/MarketBoardPlugin/Models/Universalis/MarketDataRecentHistory.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DayTrader.Models
{
    /// <summary>
    /// A model representing a SaleView V2 response from Universalis.
    /// </summary>
    internal class SaleView
    {
        /// <summary>
        /// Whether or not the item was high-quality.
        /// </summary>
        [JsonPropertyName("hq")]
        public bool Hq { get; set; }

        /// <summary>
        /// The price per unit sold.
        /// </summary>
        [JsonPropertyName("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        /// <summary>
        /// The stack size sold.
        /// </summary>
        [JsonPropertyName("quantity")]
        public uint Quantity { get; set; }

        /// <summary>
        /// The sale time, in seconds since the UNIX epoch.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public ulong Timestamp { get; set; }

        /// <summary>
        /// Whether or not this was purchased from a mannequin. This may be null.
        /// </summary>
        [JsonPropertyName("onMannequin")]
        public bool? OnMannequin { get; set; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonPropertyName("worldName")]
        public string? WorldName { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonPropertyName("worldID")]
        public ushort? WorldId { get; set; }

        /// <summary>
        /// The buyer name.
        /// </summary>
        [JsonPropertyName("buyerName")]
        public string? BuyerName { get; set; }

        /// <summary>
        /// The total price.
        /// </summary>
        [JsonPropertyName("total")]
        public uint Total { get; set; }
    }
}
