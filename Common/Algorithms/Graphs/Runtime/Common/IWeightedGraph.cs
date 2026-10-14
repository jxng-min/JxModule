using System.Collections.Generic;

namespace JxModule.Algorithms
{
    public interface IWeightedGraph<TNode>
    {
        IEnumerable<TNode> GetNeighbors(TNode node);
        float GetCost(TNode from, TNode to);
    }
}
