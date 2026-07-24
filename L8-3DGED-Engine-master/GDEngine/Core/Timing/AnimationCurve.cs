using Microsoft.Xna.Framework;

namespace GDEngine.Core.Timing
{

    /// <summary>
    /// Interpolation modes supported by <see cref="AnimationCurve"/>.
    /// </summary>
    public enum AnimationCurveInterpolation
    {
        Smooth,
        Linear
    }

    /// <summary>
    /// Scalar animation curve (SECONDS). Wraps MonoGame <see cref="Curve"/> with handy helpers.
    /// </summary>
    /// <see cref="AnimationCurve2D"/>
    /// <see cref="AnimationCurve3D"/>
    public class AnimationCurve
    {
        #region Fields
        private readonly Curve _curve;
        private readonly CurveLoopType _loop;

        // Tangent maintenance
        private bool _dirtyTangents;
        private int _lastEditedIndex = -1;

        // Cached ranges (time/value) to avoid rescans
        private bool _dirtyRanges = true;
        private double _start;
        private double _end;
        private float _minVal;
        private float _maxVal;

        private AnimationCurveInterpolation _interpolationMode = AnimationCurveInterpolation.Linear;
        #endregion

        #region Properties
        public AnimationCurveInterpolation InterpolationMode
        {
            get => _interpolationMode;
            set => _interpolationMode = value;
        }

        public CurveLoopType LoopType => _loop;
        public int KeyCount => _curve.Keys.Count;
        public bool IsEmpty => _curve.Keys.Count == 0;

        // REFACTOR - DONE - Cache start for O(1) property access
        public double StartSeconds { get { RefreshRangesIfNeeded(); return _start; } }

        // REFACTOR - DONE - Cache end for O(1) property access
        public double EndSeconds { get { RefreshRangesIfNeeded(); return _end; } }

        // REFACTOR - DONE - Cache duration for O(1) property access
        public double DurationSeconds { get { RefreshRangesIfNeeded(); return _end - _start; } }

        public CurveKeyCollection Keys => _curve.Keys;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a curve. Defaults to <see cref="CurveLoopType.Cycle"/> for both pre/post.
        /// </summary>
        /// <param name="loopType">Loop behavior for pre/post.</param>
        public AnimationCurve(CurveLoopType loopType = CurveLoopType.Cycle)
        {
            _curve = new Curve();
            _curve.PreLoop = _curve.PostLoop = loopType;
            _loop = loopType;
            _dirtyTangents = true;
            _dirtyRanges = true;
        }
        #endregion

        #region Methods
        private double GetLoopedTime(double timeSeconds)
        {
            RefreshRangesIfNeeded();

            if (KeyCount <= 1)
                return _start;

            double start = _start;
            double end = _end;
            double duration = end - start;

            if (duration <= 0.0)
                return start;

            switch (_loop)
            {
                case CurveLoopType.Cycle:
                case CurveLoopType.CycleOffset:
                    {
                        double t = (timeSeconds - start) % duration;
                        if (t < 0.0)
                            t += duration;
                        return start + t;
                    }

                case CurveLoopType.Oscillate:
                    {
                        double doubleDuration = duration * 2.0;
                        double t = (timeSeconds - start) % doubleDuration;
                        if (t < 0.0)
                            t += doubleDuration;

                        if (t <= duration)
                            return start + t;

                        return end - (t - duration);
                    }

                default:
                    {
                        if (timeSeconds <= start)
                            return start;
                        if (timeSeconds >= end)
                            return end;
                        return timeSeconds;
                    }
            }
        }

        private float EvaluateLinear(double timeSeconds)
        {
            var keys = Keys;
            int keyCount = keys.Count;

            if (keyCount == 0)
                return 0f;
            if (keyCount == 1)
                return keys[0].Value;

            double t = GetLoopedTime(timeSeconds);

            if (t <= keys[0].Position)
                return keys[0].Value;
            if (t >= keys[keyCount - 1].Position)
                return keys[keyCount - 1].Value;

            int upper = 1;
            while (upper < keyCount && t > keys[upper].Position)
                upper++;

            int lower = upper - 1;

            var k0 = keys[lower];
            var k1 = keys[upper];

            double dt = k1.Position - k0.Position;
            if (dt <= 0.0)
                return k0.Value;

            float segmentT = (float)((t - k0.Position) / dt);
            return MathHelper.Lerp(k0.Value, k1.Value, segmentT);
        }

        /// <summary>
        /// Add a key at <paramref name="timeSeconds"/>. Overwrites value if a key already exists at that time.
        /// </summary>
        public void AddKey(float value, double timeSeconds)
        {
            float t = (float)timeSeconds;
            var keys = Keys;

            // Try overwrite existing key at same time
            for (int i = 0, n = keys.Count; i < n; i++)
            {
                if (keys[i].Position == t)
                {
                    var k = keys[i];
                    k.Value = value;
                    keys[i] = k;

                    _lastEditedIndex = i;
                    _dirtyTangents = true;
                    InvalidateRanges();
                    return;
                }
            }

            // Add a new key (Keys are kept sorted by MonoGame)
            keys.Add(new CurveKey(t, value));

            _lastEditedIndex = FindKeyIndexByTime(t);
            _dirtyTangents = true;
            InvalidateRanges();
        }

        /// <summary>
        /// Set the value on an existing key by index.
        /// </summary>
        public bool SetValue(int index, float newValue)
        {
            var keys = Keys;
            if (index < 0 || index >= keys.Count) return false;

            var k = keys[index];
            k.Value = newValue;
            keys[index] = k;

            _lastEditedIndex = index;
            _dirtyTangents = true;
            InvalidateRanges();
            return true;
        }

        /// <summary>
        /// Remove all keys.
        /// </summary>
        public void Clear()
        {
            Keys.Clear();
            _dirtyTangents = true;
            _lastEditedIndex = -1;
            InvalidateRanges();
        }

        /// <summary>
        /// Evaluate at time (double seconds). If <paramref name="decimalPrecision"/> &lt; 0, returns raw value.
        /// </summary>
        public float Evaluate(double timeSeconds, int decimalPrecision = -1)
        {
            if (IsEmpty)
                return 0f;

            float v;

            if (_interpolationMode == AnimationCurveInterpolation.Smooth)
            {
                EnsureTangents();
                v = _curve.Evaluate((float)timeSeconds);
            }
            else
            {
                v = EvaluateLinear(timeSeconds);
            }

            if (decimalPrecision < 0)
                return v;

            float dp = MathF.Pow(10f, decimalPrecision);
            return MathF.Round(v * dp) / dp;
        }

        /// <summary>
        /// Evaluate at time (float seconds). Skips double-&gt;float cast in hot paths.
        /// </summary>
        public float Evaluate(float timeSeconds, int decimalPrecision = -1)
        {
            if (IsEmpty)
                return 0f;

            float v;

            if (_interpolationMode == AnimationCurveInterpolation.Smooth)
            {
                EnsureTangents();
                v = _curve.Evaluate(timeSeconds);
            }
            else
            {
                v = EvaluateLinear(timeSeconds);
            }

            if (decimalPrecision < 0)
                return v;

            float dp = MathF.Pow(10f, decimalPrecision);
            return MathF.Round(v * dp) / dp;
        }

        /// <summary>
        /// Uniformly sample the curve across [StartSeconds, EndSeconds] into a new array.
        /// </summary>
        public float[] Sample(int count, int decimalPrecision = -1)
        {
            if (count <= 0) return Array.Empty<float>();
            var arr = new float[count];
            Sample(arr.AsSpan(), decimalPrecision);
            return arr;
        }

        /// <summary>
        /// Uniformly sample the curve across [StartSeconds, EndSeconds] into a caller-provided span (allocation-free).
        /// </summary>
        // REFACTOR - DONE - Allocation-free sampling overload using Span<T>
        public void Sample(Span<float> dst, int decimalPrecision = -1)
        {
            if (dst.Length == 0) return;
            if (IsEmpty)
            {
                dst.Fill(0f);
                return;
            }

            EnsureTangents();
            RefreshRangesIfNeeded();

            double dur = DurationSeconds;
            if (dur <= 0.0)
            {
                float v = Evaluate((float)_start, decimalPrecision);
                for (int i = 0, n = dst.Length; i < n; i++) dst[i] = v;
                return;
            }

            int count = dst.Length;
            double inv = count == 1 ? 0.0 : 1.0 / (count - 1);
            for (int i = 0; i < count; i++)
            {
                double t01 = i * inv;
                double s = _start + t01 * dur;
                dst[i] = Evaluate((float)s, decimalPrecision);
            }
        }

        /// <summary>
        /// Fast heuristic for UI scaling: min/max across key values.
        /// </summary>
        public bool TryGetValueRange(out float min, out float max)
        {
            if (IsEmpty)
            {
                min = max = 0f;
                return false;
            }

            // REFACTOR - DONE - Cached value range (no per-call scans)
            RefreshRangesIfNeeded();
            min = _minVal;
            max = _maxVal;
            return true;
        }

        /// <summary>
        /// Create a simple linear ramp from startValue to endValue over durationSeconds.
        /// </summary>
        public static AnimationCurve MakeRamp(float startValue, double durationSeconds, float endValue, CurveLoopType loop = CurveLoopType.Cycle)
        {
            var c = new AnimationCurve(loop);
            c.AddKey(startValue, 0.0);
            c.AddKey(endValue, Math.Max(0.0, durationSeconds));
            return c;
        }

        /// <summary>
        /// Create a pulse (low-&gt;high-&gt;low) with up/hold/down segments (seconds).
        /// </summary>
        public static AnimationCurve MakePulse(float low, float high, double upSeconds, double holdSeconds, double downSeconds, CurveLoopType loop = CurveLoopType.Cycle)
        {
            var c = new AnimationCurve(loop);
            double t0 = 0.0;
            double t1 = t0 + Math.Max(0.0, upSeconds);
            double t2 = t1 + Math.Max(0.0, holdSeconds);
            double t3 = t2 + Math.Max(0.0, downSeconds);

            c.AddKey(low, t0);
            c.AddKey(high, t1);
            c.AddKey(high, t2);
            c.AddKey(low, t3);
            return c;
        }

        // REFACTOR - DONE - Partial tangent recomputation only around last edited key
        private void EnsureTangents()
        {
            if (!_dirtyTangents) return;
            if (IsEmpty) return;

            var keys = Keys;
            int n = keys.Count;

            if (_lastEditedIndex >= 0)
            {
                int a = Math.Max(0, _lastEditedIndex - 1);
                int b = Math.Min(n - 1, _lastEditedIndex + 1);
                for (int i = a; i <= b; i++) ComputeTangentsForKey(i);
            }
            else
            {
                for (int i = 0; i < n; i++) ComputeTangentsForKey(i);
            }

            _lastEditedIndex = -1;
            _dirtyTangents = false;
        }

        private void ComputeTangentsForKey(int i)
        {
            var keys = Keys;
            int prev = i - 1; if (prev < 0) prev = i;
            int next = i + 1; if (next >= keys.Count) next = i;

            var kPrev = keys[prev];
            var k = keys[i];
            var kNext = keys[next];

            float dtPrev = k.Position - kPrev.Position;
            float dtNext = kNext.Position - k.Position;

            float slopeIn, slopeOut;

            if (i == prev && i == next)
            {
                slopeIn = slopeOut = 0f;
            }
            else if (i == prev)
            {
                slopeIn = slopeOut = dtNext != 0 ? (kNext.Value - k.Value) / dtNext : 0f;
            }
            else if (i == next)
            {
                slopeIn = slopeOut = dtPrev != 0 ? (k.Value - kPrev.Value) / dtPrev : 0f;
            }
            else
            {
                float dt = kNext.Position - kPrev.Position;
                float dv = kNext.Value - kPrev.Value;
                float m = dt != 0 ? dv / dt : 0f;
                slopeIn = m;
                slopeOut = m;
            }

            k.TangentIn = slopeIn;
            k.TangentOut = slopeOut;

            keys[i] = k;
        }

        // REFACTOR - DONE - Centralized dirty handling for cached ranges
        private void InvalidateRanges()
        {
            _dirtyRanges = true;
        }

        // REFACTOR - DONE - Cached computation for start/end and min/max value
        private void RefreshRangesIfNeeded()
        {
            if (!_dirtyRanges) return;

            var keys = Keys;
            if (keys.Count == 0)
            {
                _start = _end = 0.0;
                _minVal = _maxVal = 0f;
                _dirtyRanges = false;
                return;
            }

            // Keys are sorted by time in MonoGame
            var k0 = keys[0];
            var kN = keys[keys.Count - 1];
            _start = k0.Position;
            _end = kN.Position;

            float minV = float.PositiveInfinity, maxV = float.NegativeInfinity;
            for (int i = 0, n = keys.Count; i < n; i++)
            {
                float v = keys[i].Value;
                if (v < minV) minV = v;
                if (v > maxV) maxV = v;
            }
            _minVal = minV;
            _maxVal = maxV;

            _dirtyRanges = false;
        }

        private int FindKeyIndexByTime(float t)
        {
            var keys = Keys;
            for (int i = 0, n = keys.Count; i < n; i++)
                if (keys[i].Position == t) return i;
            return -1;
        }

        public override string ToString()
        {
            return $"AnimationCurve(Keys={KeyCount}, Start={StartSeconds:F3}s, End={EndSeconds:F3}s, Loop={_loop})";
        }
        #endregion
    }

    /// <summary>
    /// 2D (x,y) animation curve composed of two scalar curves sharing the same time domain (SECONDS).
    /// </summary>
    /// <see cref="AnimationCurve"/>
    /// <see cref="AnimationCurve3D"/>
    public class AnimationCurve2D
    {
        #region Fields
        private readonly AnimationCurve _x;
        private readonly AnimationCurve _y;
        #endregion

        #region Properties
        public AnimationCurveInterpolation InterpolationMode
        {
            get => _x.InterpolationMode;
            set
            {
                _x.InterpolationMode = value;
                _y.InterpolationMode = value;
            }
        }

        public CurveLoopType LoopType => _x.LoopType;
        public int KeyCount => Math.Max(_x.KeyCount, _y.KeyCount);
        public bool IsEmpty => _x.IsEmpty && _y.IsEmpty;

        public double StartSeconds
        {
            get
            {
                double sx = _x.IsEmpty ? double.PositiveInfinity : _x.StartSeconds;
                double sy = _y.IsEmpty ? double.PositiveInfinity : _y.StartSeconds;
                double s = Math.Min(sx, sy);
                return double.IsPositiveInfinity(s) ? 0.0 : s;
            }
        }

        public double EndSeconds => Math.Max(_x.EndSeconds, _y.EndSeconds);
        public double DurationSeconds => IsEmpty ? 0.0 : EndSeconds - StartSeconds;
        #endregion

        #region Constructors
        public AnimationCurve2D(CurveLoopType loopType = CurveLoopType.Cycle)
        {
            _x = new AnimationCurve(loopType);
            _y = new AnimationCurve(loopType);
        }
        #endregion

        #region Methods
        public void AddKey(Vector2 value, double timeSeconds)
        {
            _x.AddKey(value.X, timeSeconds);
            _y.AddKey(value.Y, timeSeconds);
        }

        public bool SetValue(int index, Vector2 newValue)
        {
            bool a = _x.SetValue(index, newValue.X);
            bool b = _y.SetValue(index, newValue.Y);
            return a || b;
        }

        public void Clear()
        {
            _x.Clear();
            _y.Clear();
        }

        public Vector2 Evaluate(double timeSeconds, int decimalPrecision = -1)
        {
            return new Vector2(
                _x.Evaluate(timeSeconds, decimalPrecision),
                _y.Evaluate(timeSeconds, decimalPrecision)
            );
        }

        // REFACTOR - DONE - Allocation-free sampling overload using Span<T>
        public void Sample(Span<Vector2> dst, int decimalPrecision = -1)
        {
            if (dst.Length == 0) return;
            if (IsEmpty)
            {
                for (int i = 0; i < dst.Length; i++) dst[i] = Vector2.Zero;
                return;
            }

            double dur = DurationSeconds;
            if (dur <= 0.0)
            {
                var v = Evaluate(StartSeconds, decimalPrecision);
                for (int i = 0; i < dst.Length; i++) dst[i] = v;
                return;
            }

            int count = dst.Length;
            double inv = count == 1 ? 0.0 : 1.0 / (count - 1);
            double start = StartSeconds;
            for (int i = 0; i < count; i++)
            {
                double t01 = i * inv;
                double s = start + t01 * dur;
                dst[i] = Evaluate(s, decimalPrecision);
            }
        }

        public Vector2[] Sample(int count, int decimalPrecision = -1)
        {
            if (count <= 0 || IsEmpty) return Array.Empty<Vector2>();
            var arr = new Vector2[count];
            Sample(arr.AsSpan(), decimalPrecision);
            return arr;
        }
        #endregion
    }

    /// <summary>
    /// 3D (x,y,z) animation curve composed of three scalar curves sharing the same time domain (SECONDS).
    /// </summary>
    /// <see cref="AnimationCurve"/>
    /// <see cref="AnimationCurve2D"/>
    public class AnimationCurve3D
    {
        #region Fields
        private readonly AnimationCurve _x;
        private readonly AnimationCurve _y;
        private readonly AnimationCurve _z;
        #endregion

        #region Properties
        public CurveLoopType LoopType => _x.LoopType;
        public int KeyCount => Math.Max(_x.KeyCount, Math.Max(_y.KeyCount, _z.KeyCount));
        public bool IsEmpty => _x.IsEmpty && _y.IsEmpty && _z.IsEmpty;

        public double StartSeconds
        {
            get
            {
                double sx = _x.IsEmpty ? double.PositiveInfinity : _x.StartSeconds;
                double sy = _y.IsEmpty ? double.PositiveInfinity : _y.StartSeconds;
                double sz = _z.IsEmpty ? double.PositiveInfinity : _z.StartSeconds;
                double s = Math.Min(sx, Math.Min(sy, sz));
                return double.IsPositiveInfinity(s) ? 0.0 : s;
            }
        }

        public double EndSeconds => Math.Max(_x.EndSeconds, Math.Max(_y.EndSeconds, _z.EndSeconds));
        public double DurationSeconds => IsEmpty ? 0.0 : EndSeconds - StartSeconds;

        public AnimationCurveInterpolation InterpolationMode
        {
            get => _x.InterpolationMode;
            set
            {
                _x.InterpolationMode = value;
                _y.InterpolationMode = value;
                _z.InterpolationMode = value;
            }
        }
        #endregion

        #region Constructors
        public AnimationCurve3D(CurveLoopType loopType = CurveLoopType.Cycle)
        {
            _x = new AnimationCurve(loopType);
            _y = new AnimationCurve(loopType);
            _z = new AnimationCurve(loopType);
        }
        #endregion

        #region Methods
        public void AddKey(Vector3 value, double timeSeconds)
        {
            _x.AddKey(value.X, timeSeconds);
            _y.AddKey(value.Y, timeSeconds);
            _z.AddKey(value.Z, timeSeconds);
        }

        public bool SetValue(int index, Vector3 newValue)
        {
            bool a = _x.SetValue(index, newValue.X);
            bool b = _y.SetValue(index, newValue.Y);
            bool c = _z.SetValue(index, newValue.Z);
            return a || b || c;
        }

        public void Clear()
        {
            _x.Clear();
            _y.Clear();
            _z.Clear();
        }

        public Vector3 Evaluate(double timeSeconds, int decimalPrecision = -1)
        {
            return new Vector3(
                _x.Evaluate(timeSeconds, decimalPrecision),
                _y.Evaluate(timeSeconds, decimalPrecision),
                _z.Evaluate(timeSeconds, decimalPrecision)
            );
        }

        // REFACTOR - DONE - Allocation-free sampling overload using Span<T>
        public void Sample(Span<Vector3> dst, int decimalPrecision = -1)
        {
            if (dst.Length == 0) return;
            if (IsEmpty)
            {
                for (int i = 0; i < dst.Length; i++) dst[i] = Vector3.Zero;
                return;
            }

            double dur = DurationSeconds;
            if (dur <= 0.0)
            {
                var v = Evaluate(StartSeconds, decimalPrecision);
                for (int i = 0; i < dst.Length; i++) dst[i] = v;
                return;
            }

            int count = dst.Length;
            double inv = count == 1 ? 0.0 : 1.0 / (count - 1);
            double start = StartSeconds;
            for (int i = 0; i < count; i++)
            {
                double t01 = i * inv;
                double s = start + t01 * dur;
                dst[i] = Evaluate(s, decimalPrecision);
            }
        }

        public Vector3[] Sample(int count, int decimalPrecision = -1)
        {
            if (count <= 0 || IsEmpty) return Array.Empty<Vector3>();
            var arr = new Vector3[count];
            Sample(arr.AsSpan(), decimalPrecision);
            return arr;
        }
        #endregion
    }
}
