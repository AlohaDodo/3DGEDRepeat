// WindowUtil.cs
#nullable enable
using Microsoft.Xna.Framework;


#if WINDOWS || WINDOWSDX || DESKTOPGL
// For multi-monitor info on Windows:
using System.Windows.Forms;
#endif

namespace GDEngine.Core.Utilities
{
    /// <summary>
    /// Helpers for positioning/centering the game window on multi-monitor setups.
    /// </summary>
    public static class WindowUtility
    {
        /// <summary>
        /// Center the window on a given monitor index (0-based). If index is invalid, uses primary.
        /// </summary>
        public static void CenterOnMonitor(Game game, int monitorIndex)
        {
//            var size = game.GraphicsDevice.PresentationParameters;
//            var w = Math.Max(1, size.BackBufferWidth);
//            var h = Math.Max(1, size.BackBufferHeight);

//#if WINDOWS || WINDOWSDX || DESKTOPGL
//            var screens = Screen.AllScreens;
//            Screen? screen = (monitorIndex >= 0 && monitorIndex < screens.Length)
//                ? screens[monitorIndex]
//                : Screen.PrimaryScreen;

//            if (screen == null)
//                throw new ArgumentNullException("Could not retrieve screen data");

//            var area = screen.WorkingArea; // excludes taskbar
//            var x = area.X + Math.Max(0, (area.Width - w) / 2);
//            var y = area.Y + Math.Max(0, (area.Height - h) / 2);

//            game.Window.Position = new Point(x, y);
//#else
//            // Non-Windows fallback: center on primary desktop using adapter display mode
//            var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
//            var x = Math.Max(0, (display.Width  - w) / 2);
//            var y = Math.Max(0, (display.Height - h) / 2);
//            game.Window.Position = new Point(x, y);
//#endif
        }

        /// <summary>
        /// Place the window’s top-left corner at (offsetX, offsetY) relative to the chosen monitor.
        /// For example, (50,50) on monitor 1.
        /// </summary>
        public static void PlaceOnMonitor(Game game, int monitorIndex, int offsetX, int offsetY)
        {
//#if WINDOWS || WINDOWSDX || DESKTOPGL
//            var screens = Screen.AllScreens;
//            Screen? screen = (monitorIndex >= 0 && monitorIndex < screens.Length)
//                ? screens[monitorIndex]
//                : Screen.PrimaryScreen;

//            if (screen == null)
//                throw new ArgumentNullException("Could not retrieve screen data");

//            var area = screen.WorkingArea;
//            game.Window.Position = new Point(area.X + offsetX, area.Y + offsetY);
//#else
//            // Non-Windows: position is virtual-space based; just set the offsets.
//            game.Window.Position = new Point(Math.Max(0, offsetX), Math.Max(0, offsetY));
//#endif
        }
    }
}
