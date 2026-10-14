namespace JxModule.Algorithms
{
    public interface IGridCostProvider
    {
        bool IsWalkable(GridPosition position);
        float GetCost(GridPosition from, GridPosition to);
    }
}