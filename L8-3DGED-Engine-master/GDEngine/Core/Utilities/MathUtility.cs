using Microsoft.Xna.Framework;

namespace GDEngine.Core.Utilities
{
    public static class MathUtility
    {
        #region Static Fields
        private static readonly Random _randomNumberGenerator = new Random();
        #endregion

        #region Static Methods

        /// <summary>
        /// Generates a random 2D shake direction in the X/Y plane.
        /// </summary>
        public static Vector3 RandomShakeXY()
        {
            float x = (float)(_randomNumberGenerator.NextDouble() * 2.0 - 1.0);
            float y = (float)(_randomNumberGenerator.NextDouble() * 2.0 - 1.0);
            return new Vector3(x, y, 0f);
        }

        #endregion
    }
}

