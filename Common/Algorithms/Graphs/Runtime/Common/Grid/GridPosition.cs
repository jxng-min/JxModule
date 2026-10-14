using System;

namespace JxModule.Algorithms
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int x;
        public readonly int y;

        public GridPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(GridPosition other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public static bool operator ==(GridPosition a, GridPosition b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GridPosition a, GridPosition b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}