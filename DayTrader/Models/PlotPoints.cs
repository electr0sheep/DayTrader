using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader.Models
{
    internal class PlotPoints
    {
        private List<float> xs = [];
        private List<float> ys = [];
        private readonly SortedList<float, float> points = [];

        public void Add(DateTimeOffset x, float y)
        {
            this.points.Add(x.ToUnixTimeSeconds(), y);
        }

        public float[] GetXs()
        {
            xs = [];
            foreach (var pair in points)
            {
                xs.Add(pair.Key);
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
