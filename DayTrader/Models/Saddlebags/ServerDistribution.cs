using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace DayTrader.Models.Saddlebags
{
    /// <summary>
    /// A model representing sales per server from Saddlebags.
    /// </summary>
    internal class ServerDistribution
    {
        /// <summary>
        /// The number of sales recorded in the last week for Adamantoise.
        /// </summary>
        [JsonPropertyName("Adamantoise")]
        public required uint Adamantoise { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Balmung.
        /// </summary>
        [JsonPropertyName("Balmung")]
        public required uint Balmung { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Behemoth.
        /// </summary>
        [JsonPropertyName("Behemoth")]
        public required uint Behemoth { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Brynhildr.
        /// </summary>
        [JsonPropertyName("Brynhildr")]
        public required uint Brynhildr { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Cactuar.
        /// </summary>
        [JsonPropertyName("Cactuar")]
        public required uint Cactuar { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Coeurl.
        /// </summary>
        [JsonPropertyName("Coeurl")]
        public required uint Coeurl { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Cuchulainn.
        /// </summary>
        [JsonPropertyName("Cuchulainn")]
        public required uint Cuchulainn { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Diabolos.
        /// </summary>
        [JsonPropertyName("Diabolos")]
        public required uint Diabolos { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Excalibur.
        /// </summary>
        [JsonPropertyName("Excalibur")]
        public required uint Excalibur { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Exodus.
        /// </summary>
        [JsonPropertyName("Exodus")]
        public required uint Exodus { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Faerie.
        /// </summary>
        [JsonPropertyName("Faerie")]
        public required uint Faerie { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Famfrit.
        /// </summary>
        [JsonPropertyName("Famfrit")]
        public required uint Famfrit { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Gilgamesh.
        /// </summary>
        [JsonPropertyName("Gilgamesh")]
        public required uint Gilgamesh { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Goblin.
        /// </summary>
        [JsonPropertyName("Goblin")]
        public required uint Goblin { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Golem.
        /// </summary>
        [JsonPropertyName("Golem")]
        public required uint Golem { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Halicarnassus.
        /// </summary>
        [JsonPropertyName("Halicarnassus")]
        public required uint Halicarnassus { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Hyperion.
        /// </summary>
        [JsonPropertyName("Hyperion")]
        public required uint Hyperion { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Jenova.
        /// </summary>
        [JsonPropertyName("Jenova")]
        public required uint Jenova { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Kraken.
        /// </summary>
        [JsonPropertyName("Kraken")]
        public required uint Kraken { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Lamia.
        /// </summary>
        [JsonPropertyName("Lamia")]
        public required uint Lamia { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Leviathan.
        /// </summary>
        [JsonPropertyName("Leviathan")]
        public required uint Leviathan { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Maduin.
        /// </summary>
        [JsonPropertyName("Maduin")]
        public required uint Maduin { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Malboro.
        /// </summary>
        [JsonPropertyName("Malboro")]
        public required uint Malboro { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Marilith.
        /// </summary>
        [JsonPropertyName("Marilith")]
        public required uint Marilith { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Mateus.
        /// </summary>
        [JsonPropertyName("Mateus")]
        public required uint Mateus { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Midgardsormr.
        /// </summary>
        [JsonPropertyName("Midgardsormr")]
        public required uint Midgardsormr { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Rafflesia.
        /// </summary>
        [JsonPropertyName("Rafflesia")]
        public required uint Rafflesia { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Sargatanas.
        /// </summary>
        [JsonPropertyName("Sargatanas")]
        public required uint Sargatanas { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Seraph.
        /// </summary>
        [JsonPropertyName("Seraph")]
        public required uint Seraph { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Siren.
        /// </summary>
        [JsonPropertyName("Siren")]
        public required uint Siren { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Ultros.
        /// </summary>
        [JsonPropertyName("Ultros")]
        public required uint Ultros { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Zalera.
        /// </summary>
        [JsonPropertyName("Zalera")]
        public required uint Zalera { get; set; }

        public uint[] Values()
        {
            return [
                Adamantoise, Balmung, Behemoth, Brynhildr, Cactuar, Coeurl, Cuchulainn, Diabolos, Excalibur,
                Exodus, Faerie, Famfrit, Gilgamesh, Goblin, Golem, Halicarnassus, Hyperion, Jenova, Kraken,
                Lamia, Leviathan, Maduin, Malboro, Marilith, Mateus, Midgardsormr, Rafflesia, Sargatanas,
                Seraph, Siren, Ultros, Zalera
            ];
        }
    }
}
