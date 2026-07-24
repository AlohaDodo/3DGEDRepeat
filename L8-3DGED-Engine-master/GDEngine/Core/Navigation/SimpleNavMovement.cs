using GDEngine.Core.Components;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// Basic movement strategy that walks the transform along the path on the XZ plane.
    /// </summary>
    public sealed class SimpleNavMovement : IAgentMovement
    {
        #region Static Fields
        #endregion

        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public SimpleNavMovement()
        {
        }
        #endregion

        #region Methods
        public void TickMovement(
            Transform transform,
            List<Vector3> path,
            ref int currentIndex,
            float speed,
            float stoppingDistance,
            float deltaTime)
        {
            if (path == null)
                return;

            if (transform == null)
                return;

            if (currentIndex >= path.Count)
                return;

            if (speed <= 0f)
                return;

            Vector3 currentPos = transform.Position;
            Vector3 target = path[currentIndex];

            float step = speed * deltaTime;
            Vector3 delta = target - currentPos;
            delta.Y = 0f;

            if (delta.LengthSquared() <= step * step)
            {
                transform.TranslateTo(target);
                currentIndex++;
                return;
            }

            delta.Normalize();
            Vector3 move = delta * step;
            transform.TranslateBy(move, true);
        }
        #endregion
    }
}
