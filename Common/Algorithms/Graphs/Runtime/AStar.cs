using System;
using System.Collections.Generic;
using JxModule.Collections.Generic;

namespace JxModule.Algorithms
{
    public static class AStar
    {
        #region SearchContext Definition
        private sealed class SearchContext<TNode>
        {
            public readonly PriorityQueue<TNode, float> open = new();
            public readonly Dictionary<TNode, float> gScore;
            public readonly Dictionary<TNode, TNode> cameFrom;
            public readonly HashSet<TNode> closed;

            public int visitedCount;

            public SearchContext(IEqualityComparer<TNode> comparer)
            {
                gScore = new Dictionary<TNode, float>(comparer);
                cameFrom = new Dictionary<TNode, TNode>(comparer);
                closed = new HashSet<TNode>(comparer);
            }
        }
        #endregion SearchContext Definition

        public static PathResult<TNode> FindPath<TNode>(IWeightedGraph<TNode> graph,
                                                        TNode departure,
                                                        TNode destination,
                                                        Func<TNode, TNode, float> heuristic,
                                                        IEqualityComparer<TNode> comparer = null)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (heuristic == null)
            {
                throw new ArgumentNullException(nameof(heuristic));
            }

            comparer ??= EqualityComparer<TNode>.Default;

            var context = new SearchContext<TNode>(comparer);
            context.gScore[departure] = 0f;
            context.open.Enqueue(departure, ValidateCost(heuristic(departure, destination)));
            
            return Search(graph, destination, heuristic, comparer, context);
        }

        private static PathResult<TNode> Search<TNode>(IWeightedGraph<TNode> graph,
                                                       TNode destination,
                                                       Func<TNode, TNode, float> heuristic,
                                                       IEqualityComparer<TNode> comparer,
                                                       SearchContext<TNode> context)
        {
            while (context.open.Count > 0)
            {
                var current = context.open.Dequeue();
                if (!context.closed.Add(current))
                    continue;

                context.visitedCount++;

                if (comparer.Equals(current, destination))
                {
                    return ReconstructPath(current, context);
                }

                ExploreNeighbors(graph, current, destination, heuristic, context);
            }

            return new PathResult<TNode>(false, Array.Empty<TNode>(), float.PositiveInfinity, context.visitedCount);
        }

        private static void ExploreNeighbors<TNode>(IWeightedGraph<TNode> graph,
                                                    TNode current,
                                                    TNode destination,
                                                    Func<TNode, TNode, float> heuristic,
                                                    SearchContext<TNode> context)
        {
            foreach (var neighbor in graph.GetNeighbors(current))
            {
                var cost = ValidateCost(graph.GetCost(current, neighbor));
                var tentative = ValidateCost(context.gScore[current] + cost);

                if (context.gScore.TryGetValue(neighbor, out var previousCost) && tentative >= previousCost)
                {
                    continue;
                }

                var estimate = ValidateCost(heuristic(neighbor, destination));
                var priority = ValidateCost(tentative + estimate);

                context.cameFrom[neighbor] = current;
                context.gScore[neighbor] = tentative;

                context.closed.Remove(neighbor);
                context.open.Enqueue(neighbor, priority);
            }
        }

        private static PathResult<TNode> ReconstructPath<TNode>(TNode destination, SearchContext<TNode> context)
        {
            var path = new List<TNode> { destination };
            var current = destination;

            while (context.cameFrom.TryGetValue(current, out var previous))
            {
                current = previous;
                path.Add(current);
            }

            path.Reverse();

            return new PathResult<TNode>(true, path, context.gScore[destination], context.visitedCount);
        }

        private static float ValidateCost(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentException("Pathfinding costs and heuristic values must be finite and non-negative.");
            }

            return value;
        }
    }
}
