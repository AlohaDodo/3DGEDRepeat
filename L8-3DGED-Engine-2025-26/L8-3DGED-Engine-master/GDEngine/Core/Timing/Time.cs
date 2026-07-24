using Microsoft.Xna.Framework;

namespace GDEngine.Core.Timing
{
    /// <summary>
    /// Static time properties updated each frame that allow timescaling, fixed-interval events,
    /// physics timestep support, frame rate tracking, and pause functionality
    /// </summary>
    public static class Time 
    {
        #region Properties

        /// <summary>
        /// Time elapsed since last frame, scaled by TimeScale
        /// </summary>
        public static float DeltaTimeSecs { get; private set; }

        /// <summary>
        /// Time elapsed since last frame, unaffected by TimeScale
        /// </summary>
        public static float UnscaledDeltaTimeSecs { get; private set; }

        /// <summary>
        /// Smoothed delta time to reduce frame time spikes. Useful for cameras and animations.
        /// </summary>
        public static float SmoothDeltaTimeSecs { get; private set; }

        /// <summary>
        /// Global time scale multiplier. Set to 0 for pause, 0.5 for slow-motion, 2.0 for fast-forward, etc.
        /// </summary>
        public static float TimeScale { get; set; } = 1.0f;

        /// <summary>
        /// Total number of frames rendered since start
        /// </summary>
        public static int FrameCount { get; private set; }

        /// <summary>
        /// Total real time in seconds since startup, unaffected by TimeScale
        /// </summary>
        public static double RealtimeSinceStartupSecs { get; private set; }

        /// <summary>
        /// Total scaled time in seconds since startup, affected by TimeScale
        /// </summary>
        public static float TimeSinceStartupSecs { get; private set; }

        /// <summary>
        /// Whether the game time is currently paused
        /// </summary>
        public static bool IsPaused { get; private set; }

        /// <summary>
        /// Current frames per second
        /// </summary>
        public static float CurrentFPS { get; private set; }

        /// <summary>
        /// Average FPS over the last second
        /// </summary>
        public static float AverageFPS { get; private set; }

        /// <summary>
        /// Minimum frame time recorded in the last second (in seconds)
        /// </summary>
        public static float MinFrameTime { get; private set; }

        /// <summary>
        /// Maximum frame time recorded in the last second (in seconds)
        /// </summary>
        public static float MaxFrameTime { get; private set; }

        /// <summary>
        /// Maximum allowed delta time per frame. Prevents "spiral of death" during heavy frame spikes.
        /// </summary>
        public static float MaxDeltaTime { get; set; } = 0.1f; // 100ms cap

        /// <summary>
        /// Fixed timestep for physics updates (in seconds). Default is 60 Hz.
        /// </summary>
        public static float FixedDeltaTime { get; set; } = 1.0f / 60.0f; // 16.67ms

        #endregion

        #region Events

        /// <summary>
        /// Fired at the fixed physics timestep rate. Use for physics updates and deterministic logic.
        /// </summary>
        public static event Action? OnFixedUpdate;

        /// <summary>
        /// Fired at approximately 10 Hz (every 100ms). Useful for frequent but not frame-by-frame updates like nearby AI.
        /// </summary>
        public static event Action? OnFixedUpdate100ms;

        /// <summary>
        /// Fired at approximately 4 Hz (every 250ms). Useful for moderate frequency updates like UI refresh.
        /// </summary>
        public static event Action? OnFixedUpdate250ms;

        /// <summary>
        /// Fired at approximately 2 Hz (every 500ms). Useful for less frequent updates like distant object LOD.
        /// </summary>
        public static event Action? OnFixedUpdate500ms;

        /// <summary>
        /// Fired at approximately 1 Hz (every 1000ms). Useful for infrequent updates like statistics gathering.
        /// </summary>
        public static event Action? OnFixedUpdate1000ms;

        #endregion

        #region Private Fields

        // Fixed-interval accumulators
        private static float _fixedUpdateAccumulator = 0f;
        private static float _accumulator100ms = 0f;
        private static float _accumulator250ms = 0f;
        private static float _accumulator500ms = 0f;
        private static float _accumulator1000ms = 0f;

        // Fixed interval constants
        private const float Interval100ms = 0.1f;
        private const float Interval250ms = 0.25f;
        private const float Interval500ms = 0.5f;
        private const float Interval1000ms = 1.0f;

        // Smoothing for delta time
        private const int SmoothingFrames = 10;
        private static readonly float[] _deltaTimeHistory = new float[SmoothingFrames];
        private static int _deltaTimeHistoryIndex = 0;

        // FPS tracking
        private static float _fpsAccumulator = 0f;
        private static int _fpsFrameCount = 0;
        private static float _fpsUpdateInterval = 1.0f; // Update FPS once per second

        // Frame timing statistics
        private static float _currentMinFrameTime = float.MaxValue;
        private static float _currentMaxFrameTime = float.MinValue;

        // Pause state tracking
        private static float _timeScaleBeforePause = 1.0f;

        // Maximum fixed timestep iterations to prevent spiral of death
        private const int MaxFixedUpdateIterations = 5;

        #endregion

        #region Update

        public static void Update(GameTime gameTime)
        {
            // Get raw delta time and clamp it
            float rawDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            rawDeltaTime = Math.Min(rawDeltaTime, MaxDeltaTime);

            // Update unscaled time
            UnscaledDeltaTimeSecs = rawDeltaTime;
            RealtimeSinceStartupSecs += UnscaledDeltaTimeSecs;

            // Update scaled time (respects TimeScale and pause)
            DeltaTimeSecs = IsPaused ? 0f : UnscaledDeltaTimeSecs * TimeScale;
            TimeSinceStartupSecs += DeltaTimeSecs;

            // Update smoothed delta time
            UpdateSmoothDeltaTime();

            // Update frame count
            FrameCount++;

            // Update FPS and frame timing statistics
            UpdateFrameStatistics();

            // Update fixed timestep (physics)
            UpdateFixedTimestep();

            // Update fixed-interval accumulators and fire events
            UpdateFixedIntervals();
        }

        /// <summary>
        /// Calculates smoothed delta time using a rolling average to reduce frame time spikes
        /// </summary>
        private static void UpdateSmoothDeltaTime()
        {
            // Store current delta in circular buffer
            _deltaTimeHistory[_deltaTimeHistoryIndex] = UnscaledDeltaTimeSecs;
            _deltaTimeHistoryIndex = (_deltaTimeHistoryIndex + 1) % SmoothingFrames;

            // Calculate average
            float sum = 0f;
            for (int i = 0; i < SmoothingFrames; i++)
            {
                sum += _deltaTimeHistory[i];
            }
            SmoothDeltaTimeSecs = sum / SmoothingFrames;
        }

        /// <summary>
        /// Updates FPS calculations and frame timing statistics
        /// </summary>
        private static void UpdateFrameStatistics()
        {
            // Update current FPS (instantaneous)
            CurrentFPS = UnscaledDeltaTimeSecs > 0 ? 1.0f / UnscaledDeltaTimeSecs : 0f;

            // Track min/max frame times
            _currentMinFrameTime = Math.Min(_currentMinFrameTime, UnscaledDeltaTimeSecs);
            _currentMaxFrameTime = Math.Max(_currentMaxFrameTime, UnscaledDeltaTimeSecs);

            // Update average FPS over interval
            _fpsAccumulator += UnscaledDeltaTimeSecs;
            _fpsFrameCount++;

            if (_fpsAccumulator >= _fpsUpdateInterval)
            {
                AverageFPS = _fpsFrameCount / _fpsAccumulator;
                MinFrameTime = _currentMinFrameTime;
                MaxFrameTime = _currentMaxFrameTime;

                // Reset for next interval
                _fpsAccumulator = 0f;
                _fpsFrameCount = 0;
                _currentMinFrameTime = float.MaxValue;
                _currentMaxFrameTime = float.MinValue;
            }
        }

        /// <summary>
        /// Updates fixed timestep accumulator and fires OnFixedUpdate events.
        /// Prevents spiral of death by capping maximum iterations.
        /// </summary>
        private static void UpdateFixedTimestep()
        {
            // Use scaled delta time for fixed updates (respects pause)
            _fixedUpdateAccumulator += DeltaTimeSecs;

            int iterations = 0;
            while (_fixedUpdateAccumulator >= FixedDeltaTime && iterations < MaxFixedUpdateIterations)
            {
                OnFixedUpdate?.Invoke();
                _fixedUpdateAccumulator -= FixedDeltaTime;
                iterations++;
            }

            // If we hit max iterations, discard remaining time to prevent spiral of death
            if (iterations >= MaxFixedUpdateIterations)
            {
                _fixedUpdateAccumulator = 0f;
            }
        }

        /// <summary>
        /// Updates accumulators and fires fixed-interval events when thresholds are reached.
        /// Uses UnscaledDeltaTimeSecs to remain independent of TimeScale.
        /// </summary>
        private static void UpdateFixedIntervals()
        {
            // 100ms interval (10 Hz)
            _accumulator100ms += UnscaledDeltaTimeSecs;
            if (_accumulator100ms >= Interval100ms)
            {
                _accumulator100ms -= Interval100ms;
                OnFixedUpdate100ms?.Invoke();
            }

            // 250ms interval (4 Hz)
            _accumulator250ms += UnscaledDeltaTimeSecs;
            if (_accumulator250ms >= Interval250ms)
            {
                _accumulator250ms -= Interval250ms;
                OnFixedUpdate250ms?.Invoke();
            }

            // 500ms interval (2 Hz)
            _accumulator500ms += UnscaledDeltaTimeSecs;
            if (_accumulator500ms >= Interval500ms)
            {
                _accumulator500ms -= Interval500ms;
                OnFixedUpdate500ms?.Invoke();
            }

            // 1000ms interval (1 Hz)
            _accumulator1000ms += UnscaledDeltaTimeSecs;
            if (_accumulator1000ms >= Interval1000ms)
            {
                _accumulator1000ms -= Interval1000ms;
                OnFixedUpdate1000ms?.Invoke();
            }
        }

        #endregion

        #region Pause Control

        /// <summary>
        /// Pauses game time by setting TimeScale to 0. Unscaled time continues to advance.
        /// </summary>
        public static void Pause()
        {
            if (!IsPaused)
            {
                _timeScaleBeforePause = TimeScale;
                TimeScale = 0f;
                IsPaused = true;
            }
        }

        /// <summary>
        /// Resumes game time by restoring the previous TimeScale value.
        /// </summary>
        public static void Resume()
        {
            if (IsPaused)
            {
                TimeScale = _timeScaleBeforePause;
                IsPaused = false;
            }
        }

        /// <summary>
        /// Toggles between paused and resumed states.
        /// </summary>
        public static void TogglePause()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }

        #endregion
    }
}