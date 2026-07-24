using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// Pathfinding strategy abstraction.
    /// Works over an arbitrary navigation graph.
    /// </summary>
    public interface IPathfinder
    {
        #region Methods
        bool TryFindPath(
            INavGraphReader graph,
            Vector3 start,
            Vector3 end,
            List<Vector3> worldPath);
        #endregion
    }
}
