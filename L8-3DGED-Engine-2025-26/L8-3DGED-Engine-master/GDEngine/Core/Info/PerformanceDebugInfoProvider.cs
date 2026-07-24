#nullable enable
using GDEngine.Core.Rendering.UI;
using GDEngine.Core.Timing;
using System.Text;

namespace GDEngine.Core.Debug
{
    /// <summary>
    /// Provides performance and timing debug information (FPS, frame times,
    /// uptime, memory, GC stats, timescale) as text lines for on-screen display.
    /// Designed to be consumed by <see cref="UIDebugInfo"/>.
    /// </summary>
    /// <see cref="IShowDebugInfo"/>
    public sealed class PerformanceDebugInfoProvider : IShowDebugInfo
    {
        #region Static Fields
        #endregion

        #region Fields
        private DisplayProfile _profile = DisplayProfile.Standard;

        private bool _showMemoryStats;
        private bool _showFPSGraph;   // kept for parity with previous API (graph not drawn here)

        private float _goodFPSThreshold = 55f;
        private float _acceptableFPSThreshold = 45f;
        private float _warningFPSThreshold = 30f;

        private long _lastTotalMemory;
        private int _lastGen0Count;
        private int _lastGen1Count;
        private int _lastGen2Count;

        private readonly StringBuilder _builder = new StringBuilder(128);

        // Small cache for last values so we do not rebuild strings unnecessarily
        private float _lastFps;
        private float _lastAvgFps;
        private float _lastMs;
        private float _lastUptime;
        private bool _lastPaused;
        private string _cachedHeader = string.Empty;
        #endregion

        #region Properties
        /// <summary>
        /// Display profile preset controlling which stats are shown.
        /// </summary>
        public DisplayProfile Profile
        {
            get { return _profile; }
            set
            {
                _profile = value;
                ApplyProfile();
            }
        }

        /// <summary>
        /// Show memory usage and garbage collection statistics in the debug lines.
        /// </summary>
        public bool ShowMemoryStats
        {
            get { return _showMemoryStats; }
            set { _showMemoryStats = value; }
        }

        /// <summary>
        /// Hint that FPS graph information should be generated. The current provider
        /// does not create a graph, but you can use this flag if you later extend
        /// <see cref="UIDebugInfo"/> to render one.
        /// </summary>
        public bool ShowFPSGraph
        {
            get { return _showFPSGraph; }
            set { _showFPSGraph = value; }
        }

        /// <summary>
        /// FPS threshold for "good" performance.
        /// </summary>
        public float GoodFPSThreshold
        {
            get { return _goodFPSThreshold; }
            set { _goodFPSThreshold = value; }
        }

        /// <summary>
        /// FPS threshold for "acceptable" performance.
        /// </summary>
        public float AcceptableFPSThreshold
        {
            get { return _acceptableFPSThreshold; }
            set { _acceptableFPSThreshold = value; }
        }

        /// <summary>
        /// FPS threshold for "warning" performance (below this is critical).
        /// </summary>
        public float WarningFPSThreshold
        {
            get { return _warningFPSThreshold; }
            set { _warningFPSThreshold = value; }
        }
        #endregion

        #region Constructors
        public PerformanceDebugInfoProvider()
        {
            ApplyProfile();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Main entry for <see cref="IShowDebugInfo"/> – returns header + detail lines.
        /// </summary>
        public IEnumerable<string> GetDebugLines()
        {
            yield return BuildHeader();

            // Detailed stats, frame times, timescale, memory, etc.
            if (ShouldShowDetailedStats())
            {
                foreach (string line in BuildDetailedStats())
                    yield return line;
            }

            if (ShouldShowFrameTimeStats())
            {
                foreach (string line in BuildFrameTimeStats())
                    yield return line;
            }

            if (ShouldShowTimeScaleInfo())
            {
                foreach (string line in BuildTimeScaleInfo())
                    yield return line;
            }

            if (_showMemoryStats)
            {
                foreach (string line in BuildMemoryStats())
                    yield return line;
            }

            // NOTE: FPS graph was previously a visual element; if you want a textual
            // representation of history you could add it here in the future.
        }

        private void ApplyProfile()
        {
            switch (_profile)
            {
                case DisplayProfile.Minimal:
                    _showMemoryStats = false;
                    _showFPSGraph = false;
                    break;

                case DisplayProfile.Standard:
                    _showMemoryStats = false;
                    _showFPSGraph = false;
                    break;

                case DisplayProfile.Detailed:
                    _showMemoryStats = true;
                    _showFPSGraph = false;
                    break;

                case DisplayProfile.Profiling:
                    _showMemoryStats = true;
                    _showFPSGraph = true;
                    break;
            }
        }

        private bool ShouldShowDetailedStats()
        {
            return _profile == DisplayProfile.Detailed || _profile == DisplayProfile.Profiling;
        }

        private bool ShouldShowFrameTimeStats()
        {
            return _profile == DisplayProfile.Profiling;
        }

        private bool ShouldShowTimeScaleInfo()
        {
            return _profile != DisplayProfile.Minimal;
        }

        private string BuildHeader()
        {
            float fps = Time.CurrentFPS;
            float avgFps = Time.AverageFPS;
            float ms = Time.UnscaledDeltaTimeSecs * 1000f;
            float uptime = (float)Time.RealtimeSinceStartupSecs;
            bool paused = Time.IsPaused;

            bool needsRebuild =
                System.MathF.Abs(fps - _lastFps) > 0.5f ||
                System.MathF.Abs(avgFps - _lastAvgFps) > 0.5f ||
                System.MathF.Abs(ms - _lastMs) > 0.1f ||
                System.MathF.Abs(uptime - _lastUptime) > 0.1f ||
                paused != _lastPaused;

            if (!needsRebuild && _cachedHeader.Length > 0)
                return _cachedHeader;

            _lastFps = fps;
            _lastAvgFps = avgFps;
            _lastMs = ms;
            _lastUptime = uptime;
            _lastPaused = paused;

            _builder.Clear();

            if (_profile == DisplayProfile.Minimal)
            {
                _builder.Append("FPS: ");
                _builder.AppendFormat("{0:0.0}", avgFps);
                if (paused)
                    _builder.Append(" [PAUSED]");
            }
            else
            {
                _builder.Append("FPS: ");
                _builder.AppendFormat("{0:0.0}", fps);
                _builder.Append(" (Avg: ");
                _builder.AppendFormat("{0:0.0}", avgFps);
                _builder.Append(")  |  Frame: ");
                _builder.AppendFormat("{0:0.00}", ms);
                _builder.Append("ms  |  Uptime: ");
                _builder.AppendFormat("{0,6:F2}", uptime);
                _builder.Append("s");
                if (paused)
                    _builder.Append(" [PAUSED]");
            }

            _cachedHeader = _builder.ToString();
            return _cachedHeader;
        }

        private IEnumerable<string> BuildDetailedStats()
        {
            float minFPS = Time.MaxFrameTime > 0 ? 1.0f / Time.MaxFrameTime : 0f;
            float maxFPS = Time.MinFrameTime > 0 ? 1.0f / Time.MinFrameTime : 0f;

            yield return $"FPS Range: {minFPS:0.0} - {maxFPS:0.0}";
            yield return $"Frame Count: {Time.FrameCount}";
        }

        private IEnumerable<string> BuildFrameTimeStats()
        {
            float minMs = Time.MinFrameTime * 1000f;
            float maxMs = Time.MaxFrameTime * 1000f;
            float smoothMs = Time.SmoothDeltaTimeSecs * 1000f;

            yield return $"Frame Time: Min={minMs:0.00}ms, Max={maxMs:0.00}ms";
            yield return $"Smooth DT: {smoothMs:0.00}ms";
        }

        private IEnumerable<string> BuildTimeScaleInfo()
        {
            if (Time.IsPaused)
            {
                yield return $"[PAUSED] TimeScale: {Time.TimeScale:0.00}";
                yield break;
            }

            if (Time.TimeScale != 1.0f)
            {
                string scaleDescription = Time.TimeScale < 1.0f ? "Slow Motion" : "Fast Forward";
                yield return $"TimeScale: {Time.TimeScale:0.00} ({scaleDescription})";
                yield return $"Game Time: {Time.TimeSinceStartupSecs:F2}s";
            }
        }

        private IEnumerable<string> BuildMemoryStats()
        {
            _lastTotalMemory = System.GC.GetTotalMemory(false);
            _lastGen0Count = System.GC.CollectionCount(0);
            _lastGen1Count = System.GC.CollectionCount(1);
            _lastGen2Count = System.GC.CollectionCount(2);

            float memoryMB = _lastTotalMemory / (1024f * 1024f);

            yield return $"Memory: {memoryMB:0.0}MB";
            yield return $"GC: Gen0={_lastGen0Count}, Gen1={_lastGen1Count}, Gen2={_lastGen2Count}";
        }
        #endregion

        #region Lifecycle Methods
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "PerformanceDebugInfoProvider(Profile=" + _profile.ToString() + ")";
        }
        #endregion
    }

    /// <summary>
    /// Display profile presets for performance debug information.
    /// </summary>
    public enum DisplayProfile
    {
        Minimal,
        Standard,
        Detailed,
        Profiling
    }
}
