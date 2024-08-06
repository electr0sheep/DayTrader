// Shamelessly and mercilessly copied from https://github.com/ocornut/imgui/commit/99c0bd65df8eb60758b9fe4ba9f32278a628ca12
// Used Otter's code here as my guide https://github.com/Ottermandias/OtterGui/blob/main/Widgets/ToggleButton.cs

using ImGuiNET;
using System;
using OtterGuiInternal;
using OtterGuiInternal.Utility;
using System.Numerics;
using OtterGuiInternal.Structs;

namespace DayTrader
{
    internal static unsafe class ImGuiExtensions
    {
        /// <summary> Creates a SeparatorText component. </summary>
        /// <param name="label"> The text displayed. </param>
        /// <param name="extra_w"> Creates additional blank space to the right of the label. </param>
        /// <param name="x_padding"> Offsets the text by an additional amount. Probably should use text_align_x instead. </param>
        /// <param name="y_padding"> The amount of additional y padding. </param>
        /// <param name="text_align_x"> 0.0f = left, 0.5f = center, 1.0f = right. </param>
        /// <param name="text_align_y"> 0.0f = top, 0.5f = middle, 1.0f = bottom. </param>
        /// <param name="separator_thickness"> The thickness of the separator line. </param>
        public static void SeparatorText(ReadOnlySpan<char> label, float extra_w = 0.0f, float x_padding = 20.0f, float y_padding = 3.0f, float text_align_x = 0.0f, float text_align_y =0.5f, float separator_thickness = 3.0f)
        {
            var window = ImGuiInternal.GetCurrentWindow();
            var style = ImGui.GetStyle();

            var (label_end, label_size, id) = StringHelpers.ComputeSizeAndId(label);
            var pos = window.Dc.CursorPos;
            var padding = new Vector2(x_padding, y_padding);
            var min_size = new Vector2(label_size.X + extra_w + (padding.X * 2.0f), Math.Max(label_size.Y + (padding.Y * 2.0f), separator_thickness));

            var scroll_bar_offset = window.Pointer->ScrollBarY ? window.Pointer->ScrollBarSizes.X : 0.0f;
            var bb = new ImRect(pos, new Vector2(pos.X + window.Pointer->Size.X - scroll_bar_offset, pos.Y + min_size.Y));
            var text_baseline_y = (float)(int)(((bb.GetHeight() - label_size.Y) * text_align_y) + 0.99999f);
            ImGuiInternal.ItemSize(min_size, text_baseline_y);
            if (!ImGuiInternal.ItemAdd(bb, id))
                return;

            var sep1_x1 = pos.X;
            var sep2_x2 = bb.Max.X;
            var seps_y = (float)(int)(((bb.Min.Y + bb.Max.Y) * 0.5f) + 0.99999f);

            var label_avail_w = Math.Max(0.0f, sep2_x2 - sep1_x1 - (padding.X * 2.0f));
            var label_pos = new Vector2(pos.X + padding.X + Math.Max(0.0f, ((label_avail_w - label_size.X - extra_w) * text_align_x) - padding.X), pos.Y + text_baseline_y);

            window.Dc.Pointer->CursorPosPrevLine.X = label_pos.X + label_size.X;

            var separator_col = ImGui.GetColorU32(ImGuiCol.Separator);
            if (label_size.X > 0.0f)
            {
                var sep1_x2 = label_pos.X - style.ItemSpacing.X;
                var sep2_x1 = label_pos.X + label_size.X + extra_w + style.ItemSpacing.X;
                if (sep1_x2 > sep1_x1 && separator_thickness > 0.0f)
                    ImGui.GetWindowDrawList().AddLine(new Vector2(sep1_x1, seps_y), new Vector2(sep1_x2, seps_y), separator_col, separator_thickness);
                if (sep2_x2 > sep2_x1 && separator_thickness > 0.0f)
                    ImGui.GetWindowDrawList().AddLine(new Vector2(sep2_x1, seps_y), new Vector2(sep2_x2, seps_y), separator_col, separator_thickness);
                ImGuiInternal.RenderTextEllipsis(ImGui.GetWindowDrawList(), label_pos, new Vector2(bb.Max.X, bb.Max.Y + style.ItemSpacing.Y), bb.Max.X, bb.Max.X, label, &label_size);
            }
            else
            {
                if (separator_thickness > 0.0f)
                {
                    ImGui.GetWindowDrawList().AddLine(new Vector2(sep1_x1, seps_y), new Vector2(sep2_x2, seps_y), separator_col, separator_thickness);
                }
            }
        }

        // https://github.com/ocornut/imgui/issues/1901
        public static void Spinner(ReadOnlySpan<char> label, float radius, int thickness, Vector4 color)
        {
            var window = ImGuiInternal.GetCurrentWindow();
            if (window.Pointer->SkipItems)
                return;

            var style = ImGui.GetStyle();
            var (_, id) = StringHelpers.ComputeId(label);
            ImVec2 pos = window.Dc.CursorPos;
            ImVec2 size = new(radius * 2, (radius + style.FramePadding.Y) * 2);

            ImRect bb = new(pos, new ImVec2(pos.X + size.X, pos.Y + size.Y));
            ImGuiInternal.ItemSize(bb, style.FramePadding.Y);
            if (!ImGuiInternal.ItemAdd(bb, id))
                return;

            // Render
            ImGui.GetWindowDrawList().PathClear();

            var num_segments = 30;
            var start = (int)Math.Abs(Math.Sin(ImGui.GetTime() * 1.8f) * (num_segments - 5));

            var a_min = (float)Math.PI * 2.0f * start / num_segments;
            var a_max = (float)Math.PI * 2.0f * (num_segments - 3) / num_segments;

            ImVec2 centre = new(pos.X + radius, pos.Y + radius + style.FramePadding.Y);

            for (var i = 0; i < num_segments; i++)
            {
                var a = a_min + (i / (float)num_segments * (a_max - a_min));
                ImGui.GetWindowDrawList().PathLineTo(new ImVec2(
                    centre.X + (float)(Math.Cos(a + (ImGui.GetTime() * 8)) * radius),
                    centre.Y + (float)(Math.Sin(a + (ImGui.GetTime() * 8)) * radius))
                );
            }

            ImGui.GetWindowDrawList().PathStroke(ImGui.GetColorU32(color), ImDrawFlags.None, thickness);
        }
    }
}
