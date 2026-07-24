using Microsoft.Xna.Framework;

namespace GDEngine.Core.Extensions
{
    public static class Vector3Extensions
    {
   
        /// <summary>
        /// Converts the <see cref="Vector3"/> to a formatted string with fixed decimal precision.
        /// </summary>
        /// <param name="vec">The vector to format.</param>
        /// <param name="precision">
        /// The number of decimal places to include for each component.  
        /// Defaults to 1 if not specified.
        /// </param>
        /// <returns>
        /// A string in the form <c>"(x, y, z)"</c> where each component is formatted
        /// using the specified fixed-point precision.
        /// </returns>
        public static string ToFixed(this Vector3 vec, int precision = 1)
        {
            string format = $"F{precision}";
            return $"({vec.X.ToString(format)}, {vec.Y.ToString(format)}, {vec.Z.ToString(format)})";
        }

     
        public static Vector3 setTo(this Vector3 vec, float height)
        {
            return new Vector3(vec.X, height, vec.Z);
        }

    }
}
