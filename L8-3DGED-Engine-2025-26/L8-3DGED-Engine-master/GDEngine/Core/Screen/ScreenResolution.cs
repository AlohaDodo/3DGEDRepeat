#nullable enable
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Screen
{
    /// <summary>
    /// Screen resolution presets plus aspect/letterbox helpers using <see cref="Integer2"/>.
    /// </summary>
    /// <see cref="Integer2"/>
    public static class ScreenResolution
    {
        #region Static Fields
        // 16:9 
        public static readonly Integer2 R_HD_16_9_1280x720 = new Integer2(1280, 720);     // 720p
        public static readonly Integer2 R_WXGA_16_9_1366x768 = new Integer2(1366, 768);     // WXGA (16:9 variant)
        public static readonly Integer2 R_HDPlus_16_9_1600x900 = new Integer2(1600, 900);     // 900p
        public static readonly Integer2 R_FHD_16_9_1920x1080 = new Integer2(1920, 1080);    // 1080p
        public static readonly Integer2 R_QHD_16_9_2560x1440 = new Integer2(2560, 1440);    // 1440p
        public static readonly Integer2 R_QHDPlus_16_9_3200x1800 = new Integer2(3200, 1800);    // QHD+
        public static readonly Integer2 R_UHD_16_9_3840x2160 = new Integer2(3840, 2160);    // 2160p / UHD     // common alias
        public static readonly Integer2 R_5K_16_9_5120x2880 = new Integer2(5120, 2880);    // 5K
        public static readonly Integer2 R_8K_16_9_7680x4320 = new Integer2(7680, 4320);    // 8K

        // 16:10 
        public static readonly Integer2 R_WXGA_16_10_1280x800 = new Integer2(1280, 800);
        public static readonly Integer2 R_WXGAPlus_16_10_1440x900 = new Integer2(1440, 900);
        public static readonly Integer2 R_WUXGA_16_10_1920x1200 = new Integer2(1920, 1200);
        public static readonly Integer2 R_WQXGA_16_10_2560x1600 = new Integer2(2560, 1600);
        public static readonly Integer2 R_WQUXGA_16_10_3840x2400 = new Integer2(3840, 2400);

        // 21:9 Ultra-wide 
        public static readonly Integer2 R_UWFHD_21_9_2560x1080 = new Integer2(2560, 1080);    // Ultra-Wide FHD
        public static readonly Integer2 R_UWQHD_21_9_3440x1440 = new Integer2(3440, 1440);    // Ultra-Wide QHD
        public static readonly Integer2 R_UWQHDPlus_21_9_3840x1600 = new Integer2(3840, 1600);  // Ultra-Wide QHD+
        public static readonly Integer2 R_5K2K_21_9_5120x2160 = new Integer2(5120, 2160);    // 5K2K ultra-wide

        // 32:9 Super-ultra-wide 
        public static readonly Integer2 R_DFHD_32_9_3840x1080 = new Integer2(3840, 1080);    // Dual FHD
        public static readonly Integer2 R_DQHD_32_9_5120x1440 = new Integer2(5120, 1440);    // Dual QHD
        public static readonly Integer2 R_DQUHD_32_9_7680x2160 = new Integer2(7680, 2160);    // Dual UHD (informal)

        // 4:3 & 5:4 
        public static readonly Integer2 R_VGA_4_3_640x480 = new Integer2(640, 480);
        public static readonly Integer2 R_XGA_4_3_1024x768 = new Integer2(1024, 768);
        public static readonly Integer2 R_XGAPlus_4_3_1152x864 = new Integer2(1152, 864);     // XGA+
        public static readonly Integer2 R_SXGA_5_4_1280x1024 = new Integer2(1280, 1024);    // 5:4
        public static readonly Integer2 R_SXGAPlus_4_3_1400x1050 = new Integer2(1400, 1050);
        public static readonly Integer2 R_UXGA_4_3_1600x1200 = new Integer2(1600, 1200);
        public static readonly Integer2 R_QXGA_4_3_2048x1536 = new Integer2(2048, 1536);

        // 3:2 laptop 
        public static readonly Integer2 R_3_2_1920x1280 = new Integer2(1920, 1280);    // Surface-class
        public static readonly Integer2 R_3_2_2160x1440 = new Integer2(2160, 1440);    // 3:2 QHD-class
        public static readonly Integer2 R_3_2_3000x2000 = new Integer2(3000, 2000);    // 3:2 3K-class
        #endregion

        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Computes greatest common divisor (Euclid) for aspect reduction.
        /// </summary>
        /// <see cref="Integer2"/>
        public static int Gcd(int a, int b)
        {
            if (a < 0) a = -a;
            if (b < 0) b = -b;
            if (a == 0)
                return b;
            if (b == 0)
                return a;

            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }

            if (a == 0)
                return 1;

            return a;
        }

        /// <summary>
        /// Returns width/height as a float aspect ratio (0 if height is 0).
        /// </summary>
        /// <see cref="Integer2"/>
        public static float Aspect(this Integer2 size)
        {
            if (size.Y <= 0)
                return 0f;

            return (float)size.X / size.Y;
        }

        /// <summary>
        /// Returns width/height as a float aspect ratio (0 if height is 0).
        /// </summary>
        public static float Aspect(int width, int height)
        {
            if (height <= 0)
                return 0f;

            return (float)width / height;
        }

        /// <summary>
        /// Reduces a size to its simplest integer aspect pair (e.g., 1920×1080 -> 16×9).
        /// </summary>
        /// <see cref="Integer2"/>
        public static Integer2 ReduceAspect(Integer2 size)
        {
            int g = Gcd(size.X, size.Y);
            if (g == 0)
                return new Integer2(0, 0);

            return new Integer2(size.X / g, size.Y / g);
        }

        /// <summary>
        /// Sets the preferred backbuffer size from width/height integers and calls ApplyChanges().
        /// </summary>
        public static void SetResolution(GraphicsDeviceManager graphicsDeviceManager, 
            int width, int height)
        {
            graphicsDeviceManager.PreferredBackBufferWidth = Math.Max(1, width);
            graphicsDeviceManager.PreferredBackBufferHeight = Math.Max(1, height);

            graphicsDeviceManager.ApplyChanges();
        }

        /// <summary>
        /// Sets the preferred backbuffer size from an Integer2 and calls ApplyChanges().
        /// </summary>
        /// <see cref="Integer2"/>
        public static void SetResolution(GraphicsDeviceManager graphicsDeviceManager, Integer2 resolution)
        {
            SetResolution(graphicsDeviceManager, resolution.X, resolution.Y);
        }

        /// <summary>
        /// Gets the current backbuffer size as an <see cref="Integer2"/>.
        /// Uses live PresentationParameters when available; otherwise falls back to preferred size.
        /// </summary>
        /// <see cref="Integer2"/>
        public static Integer2 GetResolution(GraphicsDeviceManager graphicsDeviceManager)
        {
            var graphicsDevice = graphicsDeviceManager.GraphicsDevice;

            // If we actually have a graphics device then read its w,h
            if (graphicsDevice != null)
            {
                var pp = graphicsDevice.PresentationParameters;
                return new Integer2(Math.Max(1, pp.BackBufferWidth),
                    Math.Max(1, pp.BackBufferHeight));
            }

            // Otherwise fall back on preferred
            return new Integer2(Math.Max(1, graphicsDeviceManager.PreferredBackBufferWidth),
                Math.Max(1, graphicsDeviceManager.PreferredBackBufferHeight));
        }
        #endregion

        #region Lifecycle Methods
        #endregion

        #region Housekeeping Methods
        #endregion
    }
}
