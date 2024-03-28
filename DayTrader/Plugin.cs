using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Plugin.Windows;
using DayTrader;

namespace Plugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Day Trader";
        private const string CommandName = "/pdaytrader";

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("DayTrader");

        private ConfigWindow ConfigWindow { get; init; }
        private RetainerSellOverlay MainWindow { get; init; }
        private HelpWindow HelpWindow { get; init; }
        private RetainerSellListOverlay RetainerSellListOverlay { get; init; }

        public Plugin(DalamudPluginInterface PluginInterface)
        {
            PluginInterface.Create<Service>();

            Service.FontManager = new();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new RetainerSellOverlay(this);
            HelpWindow = new HelpWindow(this);
            RetainerSellListOverlay = new(this);
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(HelpWindow);
            WindowSystem.AddWindow(RetainerSellListOverlay);

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            
            Service.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            HelpWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
