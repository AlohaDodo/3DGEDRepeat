
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// High-level navigation service abstraction.
    /// Provides path queries in world space between positions.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Attempts to find a path between two world positions.
        /// Returns true if a path was found and written into outPath.
        /// </summary>
        bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> outPath);
    }
}
