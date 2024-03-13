using System.Text.Json.Serialization;

namespace DayTrader.Models
{
    internal class MateriaView
    {
        /// <summary>
        /// The materia slot.
        /// </summary>
        [JsonPropertyName("slotID")]
        public uint SlotId { get; set; }

        /// <summary>
        /// The materia item ID.
        /// </summary>
        [JsonPropertyName("materiaID")]
        public uint MateriaId { get; set; }
    }
}
