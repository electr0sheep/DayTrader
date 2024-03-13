using Dalamud.Interface.ManagedFontAtlas;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader
{
    internal class FontManager
    {
        public readonly IFontHandle H1;
        public readonly IFontHandle H2;
        public readonly IFontHandle H3;
        public FontManager()
        {
            H1 = Service.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(
                    tk => tk.AddDalamudDefaultFont(32f)
            ));

            H2 = Service.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(
                    tk => tk.AddDalamudDefaultFont(26f)
            ));

            H3 = Service.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(
                    tk => tk.AddDalamudDefaultFont(22f)
            ));
        }
    }
}
