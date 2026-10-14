using System.Collections.Generic;

namespace JxModule.Algorithms
{
    public class PathResult<TNode>
    {
        public bool Success { get; }
        public IReadOnlyList<TNode> Path { get; }
        public float TotalCost { get; }
        public int VisitedCount { get; }

        public PathResult(bool success, IReadOnlyList<TNode> path, float totalCost, int visitedCount)
        {
            Success = success;
            Path = path;
            TotalCost = totalCost;
            VisitedCount = visitedCount;
        }
    }
}