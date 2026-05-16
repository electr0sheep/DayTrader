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
        public uint Adamantoise { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Balmung.
        /// </summary>
        [JsonPropertyName("Balmung")]
        public uint Balmung { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Behemoth.
        /// </summary>
        [JsonPropertyName("Behemoth")]
        public uint Behemoth { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Brynhildr.
        /// </summary>
        [JsonPropertyName("Brynhildr")]
        public uint Brynhildr { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Cactuar.
        /// </summary>
        [JsonPropertyName("Cactuar")]
        public uint Cactuar { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Coeurl.
        /// </summary>
        [JsonPropertyName("Coeurl")]
        public uint Coeurl { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Cuchulainn.
        /// </summary>
        [JsonPropertyName("Cuchulainn")]
        public uint Cuchulainn { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Diabolos.
        /// </summary>
        [JsonPropertyName("Diabolos")]
        public uint Diabolos { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Excalibur.
        /// </summary>
        [JsonPropertyName("Excalibur")]
        public uint Excalibur { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Exodus.
        /// </summary>
        [JsonPropertyName("Exodus")]
        public uint Exodus { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Faerie.
        /// </summary>
        [JsonPropertyName("Faerie")]
        public uint Faerie { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Famfrit.
        /// </summary>
        [JsonPropertyName("Famfrit")]
        public uint Famfrit { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Gilgamesh.
        /// </summary>
        [JsonPropertyName("Gilgamesh")]
        public uint Gilgamesh { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Goblin.
        /// </summary>
        [JsonPropertyName("Goblin")]
        public uint Goblin { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Golem.
        /// </summary>
        [JsonPropertyName("Golem")]
        public uint Golem { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Halicarnassus.
        /// </summary>
        [JsonPropertyName("Halicarnassus")]
        public uint Halicarnassus { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Hyperion.
        /// </summary>
        [JsonPropertyName("Hyperion")]
        public uint Hyperion { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Jenova.
        /// </summary>
        [JsonPropertyName("Jenova")]
        public uint Jenova { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Kraken.
        /// </summary>
        [JsonPropertyName("Kraken")]
        public uint Kraken { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Lamia.
        /// </summary>
        [JsonPropertyName("Lamia")]
        public uint Lamia { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Leviathan.
        /// </summary>
        [JsonPropertyName("Leviathan")]
        public uint Leviathan { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Maduin.
        /// </summary>
        [JsonPropertyName("Maduin")]
        public uint Maduin { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Malboro.
        /// </summary>
        [JsonPropertyName("Malboro")]
        public uint Malboro { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Marilith.
        /// </summary>
        [JsonPropertyName("Marilith")]
        public uint Marilith { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Mateus.
        /// </summary>
        [JsonPropertyName("Mateus")]
        public uint Mateus { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Midgardsormr.
        /// </summary>
        [JsonPropertyName("Midgardsormr")]
        public uint Midgardsormr { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Rafflesia.
        /// </summary>
        [JsonPropertyName("Rafflesia")]
        public uint Rafflesia { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Sargatanas.
        /// </summary>
        [JsonPropertyName("Sargatanas")]
        public uint Sargatanas { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Seraph.
        /// </summary>
        [JsonPropertyName("Seraph")]
        public uint Seraph { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Siren.
        /// </summary>
        [JsonPropertyName("Siren")]
        public uint Siren { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Ultros.
        /// </summary>
        [JsonPropertyName("Ultros")]
        public uint Ultros { get; set; }
        /// <summary>
        /// The number of sales recorded in the last week for Zalera.
        /// </summary>
        [JsonPropertyName("Zalera")]
        public uint Zalera { get; set; }

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
