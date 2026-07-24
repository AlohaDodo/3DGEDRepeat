#nullable enable
using Microsoft.Xna.Framework; // Vector2, Vector3, Vector4

namespace GDEngine.Core
{
    /// <summary>
    /// Integer 2D vector with basic ops, constants, and conversions to/from <see cref="Vector2"/>.
    /// </summary>
    /// <see cref="Integer3"/>
    /// <see cref="Integer4"/>
    /// <seealso cref="https://simsekahmett.medium.com/understanding-and-using-the-unchecked-keyword-in-c-e6489d6b73aa"/>
    public struct Integer2 : IEquatable<Integer2>
    {
        #region Static Fields
        public static readonly Integer2 Zero = new Integer2(0, 0);
        public static readonly Integer2 One = new Integer2(1, 1);
        public static readonly Integer2 UnitX = new Integer2(1, 0);
        public static readonly Integer2 UnitY = new Integer2(0, 1);
        #endregion

        #region Fields
        private int _x;
        private int _y;
        #endregion

        #region Properties
        public int X { get => _x; set => _x = value; }
        public int Y { get => _y; set => _y = value; }
        #endregion

        #region Constructors
        public Integer2(int x, int y)
        {
            _x = x;
            _y = y;
        }
        public Integer2(float x, float y)
            : this((int)x, (int)y)
        {
   
        }

        public static Integer2 FromVector2(Vector2 v)
        {
            return new Integer2((int)MathF.Round(v.X), (int)MathF.Round(v.Y));
        }
        #endregion

        #region Methods
        /// <summary>Returns X*X + Y*Y.</summary>
        public int LengthSquared()
        {
            return _x * _x + _y * _y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(_x, _y);
        }

        public override string ToString()
        {
            return $"{_x},{_y}";
        }
        #endregion

        #region Operator Overloading
        // Operators (vector–vector)
        public static Integer2 operator +(Integer2 a, Integer2 b) => new Integer2(a._x + b._x, a._y + b._y);
        public static Integer2 operator -(Integer2 a, Integer2 b) => new Integer2(a._x - b._x, a._y - b._y);

        // Operators (vector–scalar)
        public static Integer2 operator *(Integer2 a, int s) => new Integer2(a._x * s, a._y * s);
        public static Integer2 operator *(int s, Integer2 a) => new Integer2(a._x * s, a._y * s);
        public static Integer2 operator /(Integer2 a, int s) => new Integer2(a._x / s, a._y / s);

        // Comparison
        public static bool operator ==(Integer2 a, Integer2 b) => a.Equals(b);
        public static bool operator !=(Integer2 a, Integer2 b) => !a.Equals(b);

        // Explicit conversions 
        public static explicit operator Vector2(Integer2 v) => v.ToVector2();
        public static explicit operator Integer2(Vector2 v) => FromVector2(v);
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        public bool Equals(Integer2 other)
        {
            return _x == other._x && _y == other._y;
        }

        public override bool Equals(object? obj)
        {
            return obj is Integer2 o && Equals(o);
        }

        public override int GetHashCode()
        {
            int seed = 397;
            return (_x * seed) ^ _y; //TODO - guard against overflow
        }
        #endregion

    }

    //TODO - EXERCISE - Add Integer3, Integer4

}