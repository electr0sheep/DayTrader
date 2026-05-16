// https://github.com/ocornut/imgui/issues/1901

using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace DayTrader
{
    internal static class ImGuiExtensions
    {
        public static void SeparatorText(string label)
        {
            var drawList = ImGui.GetWindowDrawList();
            var style = ImGui.GetStyle();
            var textSize = ImGui.CalcTextSize(label);
            var padding = new Vector2(20.0f, 3.0f);
            const float thickness = 3.0f;
            var posStart = ImGui.GetCursorScreenPos();
            var avail = ImGui.GetContentRegionAvail();
            var minHeight = Math.Max(textSize.Y + padding.Y * 2, thickness);

            ImGui.Dummy(new Vector2(0, minHeight));

            var sepColor = ImGui.GetColorU32(ImGuiCol.Separator);
            var textColor = ImGui.GetColorU32(ImGuiCol.Text);
            var lineY = posStart.Y + minHeight * 0.5f;
            var labelX = posStart.X + padding.X;
            var textY = posStart.Y + (minHeight - textSize.Y) * 0.5f;

            if (textSize.X > 0)
            {
                drawList.AddLine(new Vector2(posStart.X, lineY), new Vector2(labelX - style.ItemSpacing.X, lineY), sepColor, thickness);
                drawList.AddText(new Vector2(labelX, textY), textColor, label);
                drawList.AddLine(new Vector2(labelX + textSize.X + style.ItemSpacing.X, lineY), new Vector2(posStart.X + avail.X, lineY), sepColor, thickness);
            }
            else
            {
                drawList.AddLine(new Vector2(posStart.X, lineY), new Vector2(posStart.X + avail.X, lineY), sepColor, thickness);
            }
        }

        public static void Spinner(float radius, int thickness, Vector4 color)
        {
            var style = ImGui.GetStyle();
            var pos = ImGui.GetCursorScreenPos();
            var size = new Vector2(radius * 2, (radius + style.FramePadding.Y) * 2);

            ImGui.Dummy(size);
            if (!ImGui.IsItemVisible())
                return;

            var drawList = ImGui.GetWindowDrawList();
            drawList.PathClear();

            const int num_segments = 30;
            var start = (int)Math.Abs(Math.Sin(ImGui.GetTime() * 1.8f) * (num_segments - 5));

            var a_min = (float)Math.PI * 2.0f * start / num_segments;
            var a_max = (float)Math.PI * 2.0f * (num_segments - 3) / num_segments;

            var centre = new Vector2(pos.X + radius, pos.Y + radius + style.FramePadding.Y);

            for (var i = 0; i < num_segments; i++)
            {
                var a = a_min + (i / (float)num_segments * (a_max - a_min));
                drawList.PathLineTo(new Vector2(
                    centre.X + (float)(Math.Cos(a + (ImGui.GetTime() * 8)) * radius),
                    centre.Y + (float)(Math.Sin(a + (ImGui.GetTime() * 8)) * radius))
                );
            }

            drawList.PathStroke(ImGui.GetColorU32(color), ImDrawFlags.None, thickness);
        }
    }
}
