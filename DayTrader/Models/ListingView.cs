// Adapted from https://github.com/fmauNeko/MarketBoardPlugin/blob/develop/MarketBoardPlugin/Models/Universalis/MarketDataListing.cs

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
    /// A model representing a ListingView from Universalis.
    /// </summary>
    internal class ListingView
    {
        /// <summary>
        /// The time that this listing was posted, in seconds since the UNIX epoch.
        /// </summary>
        [JsonPropertyName("lastReviewTime")]
        public ulong LastReviewTime { get; set; }

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
        /// The ID of the dye on this item.
        /// </summary>
        [JsonPropertyName("stainID")]
        public uint StainId { get; set; }

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
        /// The creator's character name.
        /// </summary>
        [JsonPropertyName("creatorName")]
        public string? CreatorName { get; set; }

        /// <summary>
        /// A SHA256 hash of the creator's ID.
        /// </summary>
        [JsonPropertyName("creatorID")]
        public string? CreatorId { get; set; }

        /// <summary>
        /// Whether or not the item is high-quality.
        /// </summary>
        [JsonPropertyName("hq")]
        public bool Hq { get; set; }

        /// <summary>
        /// Whether or not the item is crafted.
        /// </summary>
        [JsonPropertyName("isCrafted")]
        public bool IsCrafted { get; set; }

        /// <summary>
        /// A SHA256 hash of the ID of this listing. Due to some current client-side bugs (i.e. Dalamud, not this plugin), this will almost always be null.
        /// </summary>
        [JsonPropertyName("listingID")]
        public string? ListingId { get; set; }

        /// <summary>
        /// The materia on this item.
        /// </summary>
        [JsonPropertyName("materia")]
        public IList<MateriaView> Materia { get; set; } = new List<MateriaView>();

        /// <summary>
        /// Whether or not the item is being sold on a mannequin.
        /// </summary>
        [JsonPropertyName("onMannequin")]
        public bool OnMannequin { get; set; }

        /// <summary>
        /// The city ID of the retainer. This is a game ID; all possible values can be seen at
        /// https://xivapi.com/Town.<br></br>
        /// <br></br>
        /// Limsa Lominsa = 1<br></br>
        /// Gridania = 2<br></br>
        /// Ul'dah = 3<br></br>
        /// Ishgard = 4<br></br>
        /// Kugane = 7<br></br>
        /// Crystarium = 10<br></br>
        /// Old Sharlayan = 12
        /// </summary>
        [JsonPropertyName("retainerCity")]
        public long RetainerCity { get; set; }

        /// <summary>
        /// A SHA256 hash of the retainer's ID.
        /// </summary>
        [JsonPropertyName("retainerID")]
        public string? RetainerId { get; set; }

        /// <summary>
        /// The retainer's name.
        /// </summary>
        [JsonPropertyName("retainerName")]
        public string? RetainerName { get; set; }

        /// <summary>
        /// A SHA256 hash of the seller's ID.
        /// </summary>
        [JsonPropertyName("sellerID")]
        public string? SellerId { get; set; }

        /// <summary>
        /// The total price.
        /// </summary>
        [JsonPropertyName("total")]
        public uint Total { get; set; }

        /// <summary>
        /// The Gil sales tax (GST) to be added to the total price during purchase.
        /// </summary>
        [JsonPropertyName("tax")]
        public uint Tax { get; set; }
    }
}
