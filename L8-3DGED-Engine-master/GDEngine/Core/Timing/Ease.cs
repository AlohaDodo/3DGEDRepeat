namespace GDEngine.Core.Timing
{
    /// <summary>
    /// Easing functions matching the set and formulas from easings.net.
    /// Use PascalCase names (e.g., EaseInSine) and x is [0,1] where y (typically in [0,1]).
    /// </summary>
    /// <see cref="https://easings.net/"/>
    public static class Ease
    {
        #region Static Fields
        private static readonly float TAU = 2f * MathF.PI; // 2π, reused in sine helpers
        #endregion

        #region Methods
        /// <summary>Linear: y = x.</summary>
        public static float Linear(float x)
        {
            return x;
        }

        // --- Sine ---

        /// <summary>EaseInSine: slow start, fast end.</summary>
        public static float EaseInSine(float x)
        {
            return 1f - MathF.Cos((x * MathF.PI) / 2f);
        }

        /// <summary>EaseOutSine: fast start, slow end.</summary>
        public static float EaseOutSine(float x)
        {
            return MathF.Sin((x * MathF.PI) / 2f);
        }

        /// <summary>EaseInOutSine: slow at start and end.</summary>
        public static float EaseInOutSine(float x)
        {
            return -0.5f * (MathF.Cos(MathF.PI * x) - 1f);
        }

        // --- Quadratic ---

        /// <summary>EaseInQuad.</summary>
        public static float EaseInQuad(float x)
        {
            return x * x;
        }

        /// <summary>EaseOutQuad.</summary>
        public static float EaseOutQuad(float x)
        {
            float t = 1f - x;
            return 1f - t * t;
        }

        /// <summary>EaseInOutQuad.</summary>
        public static float EaseInOutQuad(float x)
        {
            if (x < 0.5f)
                return 2f * x * x;

            float t = -2f * x + 2f;
            return 1f - (t * t) * 0.5f;
        }

        // --- Cubic ---

        /// <summary>EaseInCubic.</summary>
        public static float EaseInCubic(float x)
        {
            return x * x * x;
        }

        /// <summary>EaseOutCubic.</summary>
        public static float EaseOutCubic(float x)
        {
            float t = 1f - x;
            float t2 = t * t;
            return 1f - t * t2;
        }

        /// <summary>EaseInOutCubic.</summary>
        public static float EaseInOutCubic(float x)
        {
            if (x < 0.5f)
                return 4f * x * x * x;

            float t = -2f * x + 2f;
            float t2 = t * t;
            return 1f - (t * t2) * 0.5f;
        }

        // --- Quartic ---

        /// <summary>EaseInQuart.</summary>
        public static float EaseInQuart(float x)
        {
            float x2 = x * x;
            return x2 * x2;
        }

        /// <summary>EaseOutQuart.</summary>
        public static float EaseOutQuart(float x)
        {
            float t = 1f - x;
            float t2 = t * t;
            return 1f - t2 * t2;
        }

        /// <summary>EaseInOutQuart.</summary>
        public static float EaseInOutQuart(float x)
        {
            if (x < 0.5f)
            {
                float t = x * x;
                return 8f * t * t;
            }

            float u = -2f * x + 2f;       // (2-2x)
            float u2 = u * u;
            return 1f - (u2 * u2) * 0.5f;
        }

        // --- Quintic ---

        /// <summary>EaseInQuint.</summary>
        public static float EaseInQuint(float x)
        {
            float x2 = x * x;
            return x2 * x2 * x;
        }

        /// <summary>EaseOutQuint.</summary>
        public static float EaseOutQuint(float x)
        {
            float t = 1f - x;
            float t2 = t * t;
            return 1f - t2 * t2 * t;
        }

        /// <summary>EaseInOutQuint.</summary>
        public static float EaseInOutQuint(float x)
        {
            if (x < 0.5f)
            {
                float t = x * x;
                return 16f * t * t * x;
            }

            float u = -2f * x + 2f;
            float u2 = u * u;
            return 1f - (u2 * u2 * u) * 0.5f;
        }

        // --- Exponential ---

        /// <summary>EaseInExpo. Returns 0 at x=0.</summary>
        public static float EaseInExpo(float x)
        {
            if (x <= 0f)
                return 0f;

            return MathF.Pow(2f, 10f * x - 10f);
        }

        /// <summary>EaseOutExpo. Returns 1 at x=1.</summary>
        public static float EaseOutExpo(float x)
        {
            if (x >= 1f)
                return 1f;

            return 1f - MathF.Pow(2f, -10f * x);
        }

        /// <summary>EaseInOutExpo. Returns 0 at x=0 and 1 at x=1.</summary>
        public static float EaseInOutExpo(float x)
        {
            if (x <= 0f)
                return 0f;

            if (x >= 1f)
                return 1f;

            if (x < 0.5f)
                return MathF.Pow(2f, 20f * x - 10f) * 0.5f;

            return (2f - MathF.Pow(2f, -20f * x + 10f)) * 0.5f;
        }

        // --- Circular ---

        /// <summary>EaseInCirc.</summary>
        public static float EaseInCirc(float x)
        {
            return 1f - MathF.Sqrt(1f - x * x);
        }

        /// <summary>EaseOutCirc.</summary>
        public static float EaseOutCirc(float x)
        {
            float t = x - 1f;
            return MathF.Sqrt(1f - t * t);
        }

        /// <summary>EaseInOutCirc.</summary>
        public static float EaseInOutCirc(float x)
        {
            if (x < 0.5f)
            {
                float t = 2f * x;
                return (1f - MathF.Sqrt(1f - t * t)) * 0.5f;
            }

            float u = -2f * x + 2f;
            return (MathF.Sqrt(1f - u * u) + 1f) * 0.5f;
        }

        // --- Back ---

        /// <summary>EaseInBack (overshoots behind the start).</summary>
        public static float EaseInBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * x * x * x - c1 * x * x;
        }

        /// <summary>EaseOutBack (overshoots past the end).</summary>
        public static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float t = x - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        /// <summary>EaseInOutBack.</summary>
        public static float EaseInOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;

            if (x < 0.5f)
            {
                float t = 2f * x;
                return (t * t * ((c2 + 1f) * t - c2)) * 0.5f;
            }

            float u = 2f * x - 2f;
            return (u * u * ((c2 + 1f) * u + c2) + 2f) * 0.5f;
        }

        // --- Elastic ---

        /// <summary>EaseInElastic.</summary>
        public static float EaseInElastic(float x)
        {
            if (x <= 0f)
                return 0f;

            if (x >= 1f)
                return 1f;

            const float c4 = (2f * MathF.PI) / 3f;
            return -MathF.Pow(2f, 10f * x - 10f) * MathF.Sin((x * 10f - 10.75f) * c4);
        }

        /// <summary>EaseOutElastic.</summary>
        public static float EaseOutElastic(float x)
        {
            if (x <= 0f)
                return 0f;

            if (x >= 1f)
                return 1f;

            const float c4 = (2f * MathF.PI) / 3f;
            return MathF.Pow(2f, -10f * x) * MathF.Sin((x * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>EaseInOutElastic.</summary>
        public static float EaseInOutElastic(float x)
        {
            if (x <= 0f)
                return 0f;

            if (x >= 1f)
                return 1f;

            const float c5 = (2f * MathF.PI) / 4.5f;

            if (x < 0.5f)
                return -0.5f * MathF.Pow(2f, 20f * x - 10f) * MathF.Sin((20f * x - 11.125f) * c5);

            return 0.5f * MathF.Pow(2f, -20f * x + 10f) * MathF.Sin((20f * x - 11.125f) * c5) + 1f;
        }

        // --- Bounce ---

        /// <summary>EaseOutBounce.</summary>
        public static float EaseOutBounce(float x)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (x < 1f / d1)
                return n1 * x * x;

            if (x < 2f / d1)
            {
                x -= 1.5f / d1;
                return n1 * x * x + 0.75f;
            }

            if (x < 2.5f / d1)
            {
                x -= 2.25f / d1;
                return n1 * x * x + 0.9375f;
            }

            x -= 2.625f / d1;
            return n1 * x * x + 0.984375f;
        }

        /// <summary>EaseInBounce.</summary>
        public static float EaseInBounce(float x)
        {
            return 1f - EaseOutBounce(1f - x);
        }

        /// <summary>EaseInOutBounce.</summary>
        public static float EaseInOutBounce(float x)
        {
            if (x < 0.5f)
                return (1f - EaseOutBounce(1f - 2f * x)) * 0.5f;

            return (1f + EaseOutBounce(2f * x - 1f)) * 0.5f;
        }

        // --- Sine utilities ---

        /// <summary>
        /// Default sine wave mapped to [0,1]: y = 0.5 * (sin(2π * x) + 1).
        /// Conforms directly to Func so you can pass Ease.Sine01Eval.
        /// </summary>
        public static float Sine01(float x)
        {
            return 0.5f * (MathF.Sin(TAU * x) + 1f);
        }

        /// <summary>
        /// Default signed sine wave in [-1,1]: y = sin(2π * x).
        /// Conforms directly to Func&lt;float,float&gt;.
        /// </summary>
        public static float SineSigned(float x)
        {
            return MathF.Sin(TAU * x);
        }

        /// <summary>
        /// Factory: build a sine "easing" in [0,1] with custom cycles and phase (radians).
        /// Returns a Func you can plug into controllers.
        /// y(x) = 0.5 * (sin(2π * cycles * x + phase) + 1)
        /// </summary>
        public static Func<float, float> MakeSine01(float cycles = 1f, float phaseRadians = 0f)
        {
            float k = TAU * cycles;
            return (x) => 0.5f * (MathF.Sin(k * x + phaseRadians) + 1f);
        }

        /// <summary>
        /// Factory: build a signed sine in [-1,1] with custom cycles and phase (radians).
        /// y(x) = sin(2π * cycles * x + phase)
        /// </summary>
        public static Func<float, float> MakeSineSigned(float cycles = 1f, float phaseRadians = 0f)
        {
            float k = TAU * cycles;
            return (x) => MathF.Sin(k * x + phaseRadians);
        }

        #endregion
    }
}
