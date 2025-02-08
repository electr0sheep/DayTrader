using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Plugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public bool RequestSaddlebags { get; set; } = true;
        public bool ShowPriceHistory { get; set; } = true;
        public bool ShowSaleDistribution { get; set; } = true;
        public bool ShowStackSizeHistory { get; set; } = true;
        public bool ShowSalesPerHour { get; set; } = true;
        public bool ShowSalesGraph { get; set; } = true;

        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
