using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Events;
using GDEngine.Core.Orchestration;
using GDEngine.Core.Rendering.UI;
using GDEngine.Core.Services;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Frame-driven system that hosts the <see cref="Orchestrator"/> and updates it in the Update lifecycle.
    /// Also exposes orchestration state via <see cref="IShowDebugInfo"/> for UI debug overlays.
    /// </summary>
    /// <see cref="SystemBase"/>
    /// <see cref="IShowDebugInfo"/>
    public sealed class OrchestrationSystem : SystemBase, IShowDebugInfo
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Orchestrator _orchestrator;
        private readonly Orchestrator.OrchestratorOptions _options;
        private EngineContext? _context;
        private Scene? _scene;

        private bool _showPerSequenceDebug = true;
        #endregion

        #region Properties
        public Orchestrator Orchestrator
        {
            get { return _orchestrator; }
        }

        /// <summary>
        /// If true, <see cref="GetDebugLines"/> will include per-sequence lines from
        /// <see cref="Orchestrator.DebugSummary"/> under the summary header.
        /// </summary>
        public bool ShowPerSequenceDebug
        {
            get { return _showPerSequenceDebug; }
            set { _showPerSequenceDebug = value; }
        }
        #endregion

        #region Constructors
        public OrchestrationSystem(int order = 0)
            : base(FrameLifecycle.Update, order)
        {
            _orchestrator = new Orchestrator();
            _options = Orchestrator.OrchestratorOptions.Default;
        }
        #endregion

        #region Methods
        public void Configure(Action<Orchestrator.OrchestratorOptions> configure)
        {
            if (configure != null)
                configure(_options);
        }

        public void SetEventPublisher(Action<object> publish)
        {
            _orchestrator.SetEventPublisher(publish);
        }

        /// <summary>
        /// Provide orchestration debug lines for on-screen display via <see cref="UIDebugInfo"/>.
        /// </summary>
        public IEnumerable<string> GetDebugLines()
        {
            Orchestrator orch = _orchestrator;

            // Header line: time mode, scale, paused flag
            System.Text.StringBuilder header = new System.Text.StringBuilder(64);
            header.Append("Orchestrator  Time=");
            header.Append(orch.CurrentOptions.Time.ToString());
            header.Append("  x");
            header.Append(orch.CurrentOptions.LocalScale.ToString("0.##"));
            if (orch.CurrentOptions.Paused)
                header.Append(" [PAUSED]");
            yield return header.ToString();

            // Summary line: counts
            System.Text.StringBuilder counts = new System.Text.StringBuilder(48);
            counts.Append("Sequences=");
            counts.Append(orch.SequenceCount);
            counts.Append("  Active=");
            counts.Append(orch.ActiveCount);
            yield return counts.ToString();

            if (!_showPerSequenceDebug)
                yield break;

            string summary = orch.DebugSummary();
            if (string.IsNullOrEmpty(summary))
                yield break;

            string[] lines = summary.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Length > 0)
                    yield return line;
            }
        }
        #endregion

        #region Lifecycle Methods
        protected override void OnAdded()
        {
            _scene = Scene;
            _context = _scene != null ? _scene.Context : null;

            // Automatically wire up the event publisher if context is available
            if (_context != null && _context.Events != null)
            {
                _orchestrator.SetEventPublisher(_context.Events.Publish);
            }

        }

        public override void Update(float deltaTime)
        {
            if (!Enabled)
                return;

            if (_scene == null || _context == null)
                return;

            float srcDelta;
            if (_options.Time == Orchestrator.OrchestrationTime.Scaled)
                srcDelta = GDEngine.Core.Timing.Time.DeltaTimeSecs;
            else
                srcDelta = GDEngine.Core.Timing.Time.UnscaledDeltaTimeSecs;

            float localScale = _options.LocalScale;
            if (localScale < 0f)
                localScale = 0f;

            float dt = _options.Paused ? 0f : srcDelta * localScale;

            Orchestrator.OrchestrationTick tick = new Orchestrator.OrchestrationTick(dt, _context, _scene);
            _orchestrator.TickWithOptions(tick, _options);
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "OrchestrationSystem(" + _orchestrator.ToString() + ")";
        }
        #endregion
    }
}
