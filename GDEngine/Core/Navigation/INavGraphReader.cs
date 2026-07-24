
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// Read-only navigation graph abstraction used by pathfinders and debug tools.
    /// </summary>
    public interface INavGraphReader
    {
        #region Properties
        int NodeCount { get; }
        #endregion

        #region Methods
        bool IsWalkable(int nodeId);

        Vector3 GetNodePosition(int nodeId);

        int GetNeighbourCount(int nodeId);

        int GetNeighbourId(int nodeId, int neighbourIndex);

        float GetNeighbourCost(int nodeId, int neighbourIndex);

        int FindClosestWalkableNode(Vector3 point);
        #endregion
    }
}
