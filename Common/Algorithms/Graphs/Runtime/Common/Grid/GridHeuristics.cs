using System;

namespace JxModule.Algorithms
{
    public static class GridHeuristics
    {
        public static float Manhattan(GridPosition from, GridPosition to)
        {
            var dx = Math.Abs((long)from.x - to.x);
            var dy = Math.Abs((long)from.y - to.y);

            return dx + dy;
        }

        public static float Chebyshev(GridPosition from, GridPosition to)
        {
            var dx = Math.Abs((long)from.x - to.x);
            var dy = Math.Abs((long)from.y - to.y);

            return Math.Max(dx, dy);
        }

        public static float Euclidean(GridPosition from, GridPosition to)
        {
            var dx = (double)from.x - to.x;
            var dy = (double)from.y - to.y;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static float Octile(GridPosition from, GridPosition to)
        {
            var dx = Math.Abs((long)from.x - to.x);
            var dy = Math.Abs((long)from.y - to.y);

            var min = Math.Min(dx, dy);
            var max = Math.Max(dx, dy);

            return (float)(max - min + Math.Sqrt(2.0) * min);
        }
    }
}