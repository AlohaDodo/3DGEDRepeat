using GDEngine.Core.Components;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// Strategy abstraction for agent movement along a path.
    /// </summary>
    public interface IAgentMovement
    {
        #region Methods
        void TickMovement(Transform transform, List<Vector3> path, ref int currentIndex,
            float speed, float stoppingDistance, float deltaTime);
        #endregion
    }
}
