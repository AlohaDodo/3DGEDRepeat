namespace GDEngine.Core.Rendering.Base
{
    /// <summary>
    /// Bitmask representing a set of render layers (Unity-style).
    /// Provides core layers and helpers for containment and overlap tests.
    /// </summary>
    public readonly struct LayerMask 
    {
        #region Static Fields
        public static readonly LayerMask None = new LayerMask(0u);
        public static readonly LayerMask World = new LayerMask(1u);
        public static readonly LayerMask IgnoreRaycast = new LayerMask(1u << 1);
        public static readonly LayerMask Transparent = new LayerMask(1u << 2);
        public static readonly LayerMask UI = new LayerMask(1u << 3);
        public static readonly LayerMask Gizmo = new LayerMask(1u << 4);
        public static readonly LayerMask NPC = new LayerMask(1u << 5);
        public static readonly LayerMask Interactables = new LayerMask(1u << 6);
        public static readonly LayerMask Collectables = new LayerMask(1u << 7);
        public static readonly LayerMask Ground = new LayerMask(1u << 8);
        public static readonly LayerMask All = new LayerMask(0xFFFFFFFFu);
        //TODO - Students - Add more LayerMasks here to be able to render/ignore in game
        #endregion

        #region Fields
        private readonly uint _bits;
        #endregion

        #region Properties
        public uint Bits => _bits;
        #endregion

        #region Constructors
        public LayerMask(uint bits)
        {
            _bits = bits;
        }
        #endregion

        #region Methods

        /// <summary>
        /// True if this mask fully contains all bits in <paramref name="other"/>.
        /// </summary>
        public bool Contains(in LayerMask other)
        {
            return (_bits & other._bits) == other._bits;
        }

        /// <summary>
        /// True if this mask shares any bit with <paramref name="other"/>.
        /// </summary>
        public bool Overlaps(in LayerMask other)
        {
            return (_bits & other._bits) != 0u;
        }
        #endregion

        #region Housekeeping Methods
        public bool Equals(LayerMask other)
        {
            return _bits == other._bits;
        }

        public override bool Equals(object? obj)
        {
            return obj is LayerMask mask && Equals(mask);
        }

        public override int GetHashCode()
        {
            return (int)_bits;
        }

        public override string ToString()
        {
            return $"0x{_bits:X8}";
        }

        /// <summary>
        /// 32-bit binary string for this mask (MSB to LSB), zero-padded.
        /// </summary>
        public string ToBinary32()
        {
            return Convert.ToString(_bits, 2).PadLeft(32, '0');
        }

        public static LayerMask operator |(LayerMask a, LayerMask b)
        {
            return new LayerMask(a._bits | b._bits);
        }

        public static LayerMask operator &(LayerMask a, LayerMask b)
        {
            return new LayerMask(a._bits & b._bits);
        }

        public static LayerMask operator ^(LayerMask a, LayerMask b)
        {
            return new LayerMask(a._bits ^ b._bits);
        }

        public static LayerMask operator ~(LayerMask a)
        {
            return new LayerMask(~a._bits);
        }

        public static bool operator ==(LayerMask a, LayerMask b)
        {
            return a._bits == b._bits;
        }

        public static bool operator !=(LayerMask a, LayerMask b)
        {
            return a._bits != b._bits;
        }

        // So checks like `(r.Layer & cam.CullingMask) == 0` still work.
        public static implicit operator uint(LayerMask m)
        {
            return m._bits;
        }

        public static implicit operator LayerMask(uint bits)
        {
            return new LayerMask(bits);
        }
        #endregion
    }
}
