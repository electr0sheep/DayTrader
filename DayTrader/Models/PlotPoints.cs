using System;
using System.Collections.Generic;

namespace DayTrader.Models
{
    internal class PlotPoints
    {
        private List<float> xs = [];
        private List<float> ys = [];
        private readonly SortedList<DateTimeOffset, float> points = [];

        public void Add(DateTimeOffset x, float y)
        {
            this.points.Add(x, y);
        }

        public float[] GetXs()
        {
            xs = [];
            foreach (var pair in points)
            {
                xs.Add(pair.Key.ToUnixTimeSeconds());
            }
            return [.. this.xs];
        }

        public float[] GetYs()
        {
            ys = [];
            foreach (var pair in points)
            {
                ys.Add(pair.Value);
            }
            return [.. this.ys];
        }

        public int GetSize()
        {
            return this.points.Count;
        }
    }
}
