using System;
using System.Collections.Generic;

namespace JxModule.Algorithms
{
    public sealed class GridGraph : IWeightedGraph<GridPosition>
    {
        private static readonly GridPosition[] FourDirections =
        {
            new(0, 1),
            new(1, 0),
            new(0, -1),
            new(-1, 0)
        };

        private static readonly GridPosition[] EightDirections =
        {
            new(0, 1),
            new(1, 0),
            new(0, -1),
            new(-1, 0),
            new(1, 1),
            new(1, -1),
            new(-1, -1),
            new(-1, 1)
        };

        private readonly IGridCostProvider _costProvider;
        private readonly EGridMovement _movement;
        private readonly bool _allowCornerCutting;

        public int Width { get; }
        public int Height { get; }
        
        public GridGraph(int width,
                         int height,
                         IGridCostProvider costProvider,
                         EGridMovement movement = EGridMovement.FourDirections,
                         bool allowCornerCutting = false)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            _costProvider = costProvider ?? throw new ArgumentNullException(nameof(costProvider));
            if (!Enum.IsDefined(typeof(EGridMovement), movement))
            {
                throw new ArgumentOutOfRangeException(nameof(movement));
            }

            Width = width;
            Height = height;
            _movement = movement;
            _allowCornerCutting = allowCornerCutting;
        }
        
        public IEnumerable<GridPosition> GetNeighbors(GridPosition node)
        {
            if (!IsWalkable(node))
            {
                yield break;
            }

            var directions = _movement == EGridMovement.FourDirections ? FourDirections : EightDirections;

            foreach (var direction in directions)
            {
                var neighbor = new GridPosition(node.x + direction.x, node.y + direction.y);
                if (!IsWalkable(neighbor))
                {
                    continue;
                }

                var diagonal = direction.x != 0 && direction.y != 0;
                if (diagonal && !_allowCornerCutting)
                {
                    var horizontal = new GridPosition(neighbor.x, node.y);
                    var vertical = new GridPosition(node.x, neighbor.y);

                    if (!IsWalkable(horizontal) || !IsWalkable(vertical))
                    {
                        continue;
                    }
                }

                yield return neighbor;
            }
        }

        public float GetCost(GridPosition from, GridPosition to)
        {
            if (!IsWalkable(from) || !IsWalkable(to))
            {
                throw new ArgumentException("Position is not walkable.");
            }

            var dx = Math.Abs((long)to.x - from.x);
            var dy = Math.Abs((long)to.y - from.y);

            var cardinal = dx + dy == 1;
            var diagonal = dx == 1 && dy == 1;

            if (!cardinal && !(_movement == EGridMovement.EightDirections && diagonal))
            {
                throw new ArgumentException("Positions are not adjacent.");
            }

            if (diagonal && !_allowCornerCutting)
            {
                if (!IsWalkable(new GridPosition(to.x, from.y)) ||
                    !IsWalkable(new GridPosition(from.x, to.y)))
                {
                    throw new ArgumentException("Diagonal movement crosses a blocked corner.");
                }
            }

            return _costProvider.GetCost(from, to);
        }
        
        public bool IsInBounds(GridPosition position)
        {
            return position.x >= 0 &&
                   position.y >= 0 &&
                   position.x < Width &&
                   position.y < Height;
        }

        public bool IsWalkable(GridPosition position)
        {
            return IsInBounds(position) && _costProvider.IsWalkable(position);
        }
    }
}