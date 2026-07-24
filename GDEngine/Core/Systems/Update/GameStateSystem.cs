using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Events;
using GDEngine.Core.Gameplay;
using GDEngine.Core.Rendering.UI;
using GDEngine.Core.Services;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Manages the game's high-level outcome (InProgress, Won, Lost, Paused).
    /// Evaluates win/lose conditions each frame, publishes state events,
    /// and exposes debug info for display via <see cref="UIDebugInfo"/>.
    /// </summary>
    /// <see cref="IGameCondition"/>
    /// <see cref="GameOutcomeState"/>
    /// <see cref="GameStateChangedEvent"/>
    /// <see cref="GameWonEvent"/>
    /// <see cref="GameLostEvent"/>
    public sealed class GameStateSystem : PausableSystemBase, IShowDebugInfo
    {
        #region Static Fields
        #endregion

        #region Fields
        private IGameCondition? _winCondition;
        private IGameCondition? _loseCondition;

        private GameOutcomeState _state;

        private Scene? _scene;
        private EngineContext? _context;
        private EventBus? _events;

        private bool _showConditionTrees = true;
        private bool _showOnlyFailingConditions;
        #endregion

        #region Properties
        public GameOutcomeState State
        {
            get { return _state; }
        }

        public IGameCondition? WinCondition
        {
            get { return _winCondition; }
        }

        public IGameCondition? LoseCondition
        {
            get { return _loseCondition; }
        }

        /// <summary>
        /// Master toggle for including the condition trees in debug output.
        /// </summary>
        public bool ShowConditionTrees
        {
            get { return _showConditionTrees; }
            set { _showConditionTrees = value; }
        }

        /// <summary>
        /// If true, the debug overlay only shows failing conditions
        /// (and composites that contain failing children), to reduce noise.
        /// </summary>
        public bool ShowOnlyFailingConditions
        {
            get { return _showOnlyFailingConditions; }
            set { _showOnlyFailingConditions = value; }
        }
        #endregion

        #region Constructors
        public GameStateSystem(int order = 0)
            : base(FrameLifecycle.Update, order)
        {
            _state = GameOutcomeState.InProgress;

            // Pause should stop win/lose conditions from ticking but still allow debug overlay to draw if needed.
            PauseMode = PauseMode.Update;
        }
        #endregion

        #region Configuration
        public void ConfigureConditions(IGameCondition? winCondition, IGameCondition? loseCondition)
        {
            _winCondition = winCondition;
            _loseCondition = loseCondition;
        }

        public void Reset()
        {
            _state = GameOutcomeState.InProgress;
        }

        public void Pause()
        {
            if (_state == GameOutcomeState.InProgress)
                _state = GameOutcomeState.Paused;
        }

        public void Resume()
        {
            if (_state == GameOutcomeState.Paused)
                _state = GameOutcomeState.InProgress;
        }
        #endregion

        #region Update Logic
        private GameOutcomeState TickLogic()
        {
            if (_state != GameOutcomeState.InProgress)
                return _state;

            if (_loseCondition != null && _loseCondition.IsSatisfied())
            {
                ChangeState(GameOutcomeState.Lost);
                return _state;
            }

            if (_winCondition != null && _winCondition.IsSatisfied())
            {
                ChangeState(GameOutcomeState.Won);
                return _state;
            }

            return _state;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!Enabled)
                return;


            // Total overkill because its too often!
            TickLogic();
        }
        #endregion

        #region Events
        public event Action<GameOutcomeState, GameOutcomeState>? StateChanged;

        private void ChangeState(GameOutcomeState newState)
        {
            if (newState == _state)
                return;

            GameOutcomeState old = _state;
            _state = newState;

            StateChanged?.Invoke(old, newState);

            if (_events != null)
            {
                _events.Publish(new GameStateChangedEvent(old, newState));

                if (newState == GameOutcomeState.Won)
                    _events.Publish(new GameWonEvent());
                else if (newState == GameOutcomeState.Lost)
                    _events.Publish(new GameLostEvent());
            }
        }
        #endregion

        #region Debug Overlay
        public IEnumerable<string> GetDebugLines()
        {
            // Header: overall state
            yield return "GameState  State=" + _state.ToString();

            // Summary: win/lose configured?
            yield return string.Concat(
                "Win=", _winCondition != null ? "configured" : "none",
                "  Lose=", _loseCondition != null ? "configured" : "none"
            );

            if (!_showConditionTrees)
                yield break;

            bool failingOnly = _showOnlyFailingConditions;

            // Win tree
            yield return "Win condition:";
            if (_winCondition == null)
            {
                yield return "  <none>";
            }
            else
            {
                foreach (string line in BuildConditionLines(_winCondition, 1, failingOnly))
                    yield return line;
            }

            // Lose tree
            yield return "Lose condition:";
            if (_loseCondition == null)
            {
                yield return "  <none>";
            }
            else
            {
                foreach (string line in BuildConditionLines(_loseCondition, 1, failingOnly))
                    yield return line;
            }
        }

        private static IEnumerable<string> BuildConditionLines(IGameCondition root, int depth, bool showOnlyFailing)
        {
            List<string> lines = new List<string>(32);
            AppendConditionLines(root, depth, showOnlyFailing, lines);
            return lines;
        }

        /// <summary>
        /// Recursively append condition lines.
        /// When showOnlyFailing is true, we:
        /// - Hide satisfied leaf conditions.
        /// - Hide satisfied composite conditions that have no failing children.
        /// </summary>
        private static void AppendConditionLines(
            IGameCondition condition,
            int depth,
            bool showOnlyFailing,
            IList<string> output)
        {
            bool satisfied;

            try
            {
                satisfied = condition.IsSatisfied();
            }
            catch
            {
                satisfied = false;
            }

            CompositeCondition? composite = condition as CompositeCondition;

            if (composite == null)
            {
                // Leaf condition
                if (showOnlyFailing && satisfied)
                    return;

                string prefix = satisfied ? "[+]" : "[ ]";
                string indent = new string(' ', depth * 2);

                output.Add(indent + prefix + " " + condition.Description);
                return;
            }

            // Composite: process children first into a temporary buffer
            List<string> childLines = new List<string>(16);
            int nextDepth = depth + 1;

            IReadOnlyList<IGameCondition> children = composite.ChildConditions;
            for (int i = 0; i < children.Count; i++)
            {
                IGameCondition child = children[i];
                if (child != null)
                    AppendConditionLines(child, nextDepth, showOnlyFailing, childLines);
            }

            if (showOnlyFailing)
            {
                // If satisfied AND no failing children (i.e., no child lines),
                // we skip this whole subtree.
                if (satisfied && childLines.Count == 0)
                    return;
            }

            // Write this composite
            string compositePrefix = satisfied ? "[+]" : "[ ]";
            string compositeIndent = new string(' ', depth * 2);
            output.Add(compositeIndent + compositePrefix + " " + condition.Description);

            // Then its (filtered) children
            for (int i = 0; i < childLines.Count; i++)
                output.Add(childLines[i]);
        }
        #endregion

        #region Lifecycle
        protected override void OnAdded()
        {
            _scene = Scene;
            _context = _scene?.Context;
            _events = _context?.Events;
        }

        protected override void OnRemoved()
        {
            // No external event subscriptions to clean up.
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "GameStateSystem(State=" + _state.ToString() + ")";
        }






































































































































































































































































































        #endregion
    }
}
