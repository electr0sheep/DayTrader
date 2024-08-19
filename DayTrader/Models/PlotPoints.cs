using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayTrader.Models
{
    internal class PlotPoints
    {
        private readonly List<float> xs = [];
        private readonly List<float> ys = [];

        public void Add(DateTimeOffset x, float y)
        {
            this.xs.Add(x.ToUnixTimeSeconds());
            this.ys.Add(y);
        }

        public float[] GetXs()
        {
            return [.. this.xs];
        }

        public float[] GetYs()
        {
            return [.. this.ys];
        }

        public int GetSize()
        {
            return this.xs.Count;
        }
    }
}
