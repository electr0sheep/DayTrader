// Adapted from https://github.com/fmauNeko/MarketBoardPlugin/blob/develop/MarketBoardPlugin/Models/Universalis/MarketDataResponse.cs

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DayTrader.Models
{
    /// <summary>
    /// A model representing a CurrentlyShownView from Universalis.
    /// </summary>
    internal class CurrentlyShownView
    {
        /// <summary>
        /// The item ID.
        /// </summary>
        [JsonPropertyName("itemID")]
        public uint ItemId { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonPropertyName("worldID")]
        public uint? WorldID { get; set; }

        /// <summary>
        /// The last upload time for this endpoint, in milliseconds since the UNIX epoch.
        /// </summary>
        [JsonPropertyName("lastUploadTime")]
        public ulong LastUploadTime { get; set; }

        /// <summary>
        /// The currently-shown listings.
        /// </summary>
        [JsonPropertyName("listings")]
        public IList<ListingView> Listings { get; set; } = new List<ListingView>();

        /// <summary>
        /// The currently-shown sales.
        /// </summary>
        [JsonPropertyName("recentHistory")]
        public IList<SaleView> RecentHistory { get; set; } = new List<SaleView>();

        /// <summary>
        /// The DC name, if applicable.
        /// </summary>
        [JsonPropertyName("dcName")]
        public string? DcName { get; set; }

        /// <summary>
        /// The region name, if applicable.
        /// </summary>
        [JsonPropertyName("regionName")]
        public string? RegionName { get; set; }

        /// <summary>
        /// The average listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonPropertyName("currentAveragePrice")]
        public double CurrentAveragePrice { get; set; }

        /// <summary>
        /// The average NQ listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonPropertyName("currentAveragePriceNQ")]
        public double CurrentAveragePriceNq { get; set; }

        /// <summary>
        /// The average HQ listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonPropertyName("currentAveragePriceHQ")]
        public double CurrentAveragePriceHq { get; set; }

        /// <summary>
        /// The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
        /// This statistic is more useful in historical queries.
        /// </summary>
        [JsonPropertyName("regularSaleVelocity")]
        public double RegularSaleVelocity { get; set; }

        /// <summary>
        /// The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
        /// This statistic is more useful in historical queries.
        /// </summary>
        [JsonPropertyName("nqSaleVelocity")]
        public double NqSaleVelocity { get; set; }

        /// <summary>
        /// The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
        /// This statistic is more useful in historical queries.
        /// </summary>
        [JsonPropertyName("hqSaleVelocity")]
        public double HqSaleVelocity { get; set; }

        /// <summary>
        /// The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonPropertyName("averagePrice")]
        public double AveragePrice { get; set; }

        /// <summary>
        /// The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonPropertyName("averagePriceNQ")]
        public double AveragePriceNq { get; set; }

        /// <summary>
        /// The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonPropertyName("averagePriceHQ")]
        public double AveragePriceHq { get; set; }

        /// <summary>
        /// The minimum listing price.
        /// </summary>
        [JsonPropertyName("minPrice")]
        public uint MinPrice { get; set; }

        /// <summary>
        /// The minimum NQ listing price.
        /// </summary>
        [JsonPropertyName("minPriceNQ")]
        public uint MinPriceNq { get; set; }

        /// <summary>
        /// The minimum HQ listing price.
        /// </summary>
        [JsonPropertyName("minPriceHQ")]
        public uint MinPriceHq { get; set; }

        /// <summary>
        /// The maximum listing price.
        /// </summary>
        [JsonPropertyName("maxPrice")]
        public uint MaxPrice { get; set; }

        /// <summary>
        /// The maximum NQ listing price.
        /// </summary>
        [JsonPropertyName("maxPriceNQ")]
        public uint MaxPriceNq { get; set; }

        /// <summary>
        /// The maximum HQ listing price.
        /// </summary>
        [JsonPropertyName("maxPriceHQ")]
        public uint MaxPriceHq { get; set; }

        /// <summary>
        /// A map of quantities to listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonPropertyName("stackSizeHistogram")]
        public Dictionary<ushort, uint> StackSizeHistogram { get; } = new Dictionary<ushort, uint>();

        /// <summary>
        /// A map of quantities to NQ listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonPropertyName("stackSizeHistogramNQ")]
        public Dictionary<ushort, uint> StackSizeHistogramNq { get; } = new Dictionary<ushort, uint>();

        /// <summary>
        /// A map of quantities to HQ listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonPropertyName("stackSizeHistogramHQ")]
        public Dictionary<ushort, uint> StackSizeHistogramHq { get; } = new Dictionary<ushort, uint>();

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonPropertyName("worldName")]
        public string? WorldName { get; set; }

        /// <summary>
        /// The last upload times in milliseconds since epoch for each world in the response, if this is a DC request.
        /// </summary>
        [JsonPropertyName("worldUploadTimes")]
        public Dictionary<ushort, ulong> WorldUploadTimes { get; } = new Dictionary<ushort, ulong>();

        /// <summary>
        /// The number of listings retrieved for the request. When using the "listings" limit parameter, this may be
        /// different from the number of sale entries returned in an API response.
        /// </summary>
        [JsonPropertyName("listingsCount")]
        public uint ListingsCount { get; set; }

        /// <summary>
        /// The number of sale entries retrieved for the request. When using the "entries" limit parameter, this may be
        /// different from the number of sale entries returned in an API response.
        /// </summary>
        [JsonPropertyName("recentHistoryCount")]
        public uint RecentHistoryCount { get; set; }

        /// <summary>
        /// The number of items (not listings) up for sale.
        /// </summary>
        [JsonPropertyName("unitsForSale")]
        public uint UnitsForSale { get; set; }

        /// <summary>
        /// The number of items (not sale entries) sold over the retrieved sales.
        /// </summary>
        [JsonPropertyName("unitsSold")]
        public uint UnitsSold { get; set; }
    }
}
