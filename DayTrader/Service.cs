using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader
{
    internal class Service
    {
        public static FontManager FontManager { get; set; } = null!;
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static ICondition Condition { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
    }
}
