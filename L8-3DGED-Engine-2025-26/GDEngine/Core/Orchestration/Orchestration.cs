using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Services;
using GDEngine.Core.Systems;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Orchestration
{
    /// <summary>
    /// Directive controller that runs named, step-based sequences across the engine.
    /// Provides a fluent Builder API and integrates with Scene, EventBus (via delegate),
    /// ImpulseSystem, and other systems.
    /// </summary>
    /// <see cref="OrchestrationSystem"/>
    /// <example>
    public sealed partial class Orchestrator
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Dictionary<string, Sequence> _sequences = new Dictionary<string, Sequence>();
        private readonly List<Sequence> _active = new List<Sequence>(16);
        private readonly Dictionary<string, int> _barriers = new Dictionary<string, int>();

        private Action<object>? _publish;
        private OrchestratorOptions _lastOptions = OrchestratorOptions.Default;
        #endregion

        #region Properties
        /// <summary>
        /// Total registered sequences.
        /// </summary>
        public int SequenceCount
        {
            get { return _sequences.Count; }
        }

        /// <summary>
        /// Number of sequences currently running.
        /// </summary>
        public int ActiveCount
        {
            get { return _active.Count; }
        }

        /// <summary>
        /// Last applied options from the owning <see cref="OrchestrationSystem"/>.
        /// </summary>
        public OrchestratorOptions CurrentOptions
        {
            get { return _lastOptions; }
        }
        #endregion

        #region Constructors
        public Orchestrator()
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// Assign an event publisher (for example, EventBus.Publish).
        /// </summary>
        public void SetEventPublisher(Action<object> publish)
        {
            _publish = publish;
        }

        /// <summary>
        /// Create a fluent builder for a new sequence.
        /// </summary>
        public Builder Build(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Sequence name cannot be null or empty.", nameof(name));

            return new Builder(this, name);
        }

        /// <summary>
        /// Start a sequence by name if registered and allowed by its guard.
        /// </summary>
        public bool Start(string name, Scene scene, EngineContext context)
        {
            if (!_sequences.TryGetValue(name, out Sequence? seq))
                return false;

            if (seq.IsRunning)
                return false;

            OrchestrationContext ctx = new OrchestrationContext(context, scene, _publish);
            if (seq.Guard != null && !seq.Guard.CanStart(ctx))
                return false;

            seq.ValidateOrThrow(scene);
            seq.Reset();
            seq.IsRunning = true;
            _active.Add(seq);
            if (seq.OnStarted != null)
                seq.OnStarted();

            return true;
        }

        /// <summary>
        /// Stop a running sequence by name.
        /// </summary>
        public bool Stop(string name)
        {
            if (!_sequences.TryGetValue(name, out Sequence? seq))
                return false;

            if (!seq.IsRunning)
                return false;

            seq.IsRunning = false;
            if (seq.OnStopped != null)
                seq.OnStopped();

            _active.Remove(seq);
            return true;
        }

        /// <summary>
        /// Remove a sequence definition (even if not running).
        /// </summary>
        public bool Remove(string name)
        {
            if (!_sequences.TryGetValue(name, out Sequence? seq))
                return false;

            if (seq.IsRunning)
                _active.Remove(seq);

            return _sequences.Remove(name);
        }

        /// <summary>
        /// Pause a running sequence by name, preserving its current state.
        /// </summary>
        /// <param name="name">Name of the sequence to pause</param>
        /// <returns>True if the sequence was paused, false if not found or not running</returns>
        public bool Pause(string name)
        {
            if (!_sequences.TryGetValue(name, out Sequence? seq))
                return false;

            if (!seq.IsRunning)
                return false;

            seq.IsPaused = true;
            return true;
        }

        /// <summary>
        /// Resume a paused sequence by name.
        /// </summary>
        /// <param name="name">Name of the sequence to resume</param>
        /// <returns>True if the sequence was resumed, false if not found or not paused</returns>
        public bool Resume(string name)
        {
            if (!_sequences.TryGetValue(name, out Sequence? seq))
                return false;

            if (!seq.IsRunning || !seq.IsPaused)
                return false;

            seq.IsPaused = false;
            return true;
        }

        /// <summary>
        /// Toggle pause state of a sequence.
        /// </summary>
        /// <param name="name">Name of the sequence to toggle</param>
        /// <returns>True if the sequence is now paused, false if now running, null if not found</returns>
        public bool? TogglePause(string name)
        {
            if (!_sequences.TryGetValue(name, out Sequence? seq))
                return null;

            if (!seq.IsRunning)
                return null;

            seq.IsPaused = !seq.IsPaused;
            return seq.IsPaused;
        }

        /// <summary>
        /// Check if a sequence is currently paused.
        /// </summary>
        /// <param name="name">Name of the sequence to check</param>
        /// <returns>True if paused, false if running or not found</returns>
        public bool IsPaused(string name)
        {
            if (!_sequences.TryGetValue(name, out Sequence? seq))
                return false;

            return seq.IsRunning && seq.IsPaused;
        }

        /// <summary>
        /// Tick all active sequences using the given frame data.
        /// Skips paused sequences without removing them from the active list.
        /// </summary>
        public void Tick(in OrchestrationTick tick)
        {
            if (_active.Count == 0)
                return;

            Sequence[] snapshot = _active.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                Sequence seq = snapshot[i];
                if (!seq.IsRunning)
                    continue;

                // Skip paused sequences (NEW)
                if (seq.IsPaused)
                    continue;

                OrchestrationContext ctx = new OrchestrationContext(tick.Context, tick.Scene, _publish);
                seq.Tick(tick.DeltaTime, ctx);

                if (!seq.IsRunning)
                    _active.Remove(seq);
            }
        }

        /// <summary>
        /// Internal helper used by <see cref="OrchestrationSystem"/> to apply options.
        /// </summary>
        internal void TickWithOptions(in OrchestrationTick tick, OrchestratorOptions options)
        {
            _lastOptions = options;
            Tick(tick);
        }

        /// <summary>
        /// Internal registration called by the <see cref="Builder"/>.
        /// </summary>
        internal void Register(Sequence seq)
        {
            if (_sequences.ContainsKey(seq.Name))
                throw new InvalidOperationException("Sequence '" + seq.Name + "' is already registered.");

            _sequences.Add(seq.Name, seq);
        }

        /// <summary>
        /// Signal that a sequence has reached a named barrier and return the total arrivals.
        /// </summary>
        internal int SignalBarrier(string name)
        {
            if (!_barriers.TryGetValue(name, out int count))
                count = 0;

            count++;
            _barriers[name] = count;
            return count;
        }

        /// <summary>
        /// Capture a minimal snapshot of the current step index for the named sequence.
        /// </summary>
        public OrchestrationSnapshot? Capture(string name)
        {
            if (!_sequences.TryGetValue(name, out Sequence? seq))
                return null;

            return new OrchestrationSnapshot(seq.Name, seq.CurrentStepIndex);
        }

        /// <summary>
        /// Restore a previously captured snapshot, resuming the sequence at that step index.
        /// </summary>
        public bool Restore(OrchestrationSnapshot snapshot)
        {
            if (!_sequences.TryGetValue(snapshot.Name, out Sequence? seq))
                return false;

            seq.IsRunning = true;
            seq.ForceStepIndex(snapshot.StepIndex);
            if (!_active.Contains(seq))
                _active.Add(seq);

            return true;
        }

        /// <summary>
        /// Build a simple multi-line string describing all sequences and their states.
        /// </summary>
        public string DebugSummary()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Sequences:");
            foreach (KeyValuePair<string, Sequence> pair in _sequences)
            {
                Sequence seq = pair.Value;
                sb.Append(" - ");
                sb.Append(seq.Name);
                sb.Append(" (Running=");
                sb.Append(seq.IsRunning ? "true" : "false");

                // Add paused indicator (NEW)
                if (seq.IsRunning && seq.IsPaused)
                    sb.Append(", PAUSED");

                sb.Append(", Steps=");
                sb.Append(seq.StepCount);
                sb.Append(", CurrentStep=");
                sb.Append(seq.CurrentStepIndex);
                sb.AppendLine(")");
            }

            return sb.ToString();
        }
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "Orchestrator(Sequences=" + _sequences.Count + ", Active=" + _active.Count + ")";
        }
        #endregion

        // =====================================================================
        // Inner types: options, tick, context, snapshot
        // =====================================================================

        #region Inner Types

        /// <summary>
        /// Immutable tick data used when advancing the orchestrator.
        /// </summary>
        public readonly struct OrchestrationTick
        {
            public readonly float DeltaTime;
            public readonly EngineContext Context;
            public readonly Scene Scene;

            public OrchestrationTick(float deltaTime, EngineContext context, Scene scene)
            {
                DeltaTime = deltaTime;
                Context = context;
                Scene = scene;
            }
        }

        /// <summary>
        /// Context passed to steps with access to core engine services.
        /// </summary>
        public readonly struct OrchestrationContext
        {
            public readonly EngineContext Context;
            public readonly Scene Scene;
            public readonly Action<object>? Publish;

            public OrchestrationContext(EngineContext context, Scene scene, Action<object>? publish)
            {
                Context = context;
                Scene = scene;
                Publish = publish;
            }

            public void Emit(object evt)
            {
                if (Publish != null)
                    Publish(evt);
            }
        }

        /// <summary>
        /// Minimal snapshot containing the sequence name and current step index.
        /// </summary>
        public readonly struct OrchestrationSnapshot
        {
            public readonly string Name;
            public readonly int StepIndex;

            public OrchestrationSnapshot(string name, int stepIndex)
            {
                Name = name;
                StepIndex = stepIndex;
            }
        }

        /// <summary>
        /// Step run state.
        /// </summary>
        public enum StepStatus : sbyte
        {
            Running = 0,
            Succeeded = 1,
            Failed = 2
        }

        /// <summary>
        /// Optional guard contract used to decide if a sequence can start.
        /// </summary>
        public interface IGuard
        {
            bool CanStart(OrchestrationContext ctx);
        }

        /// <summary>
        /// Shared options controlling how time flows for sequences.
        /// </summary>
        public sealed class OrchestratorOptions
        {
            #region Static Fields
            #endregion

            #region Fields
            private OrchestrationTime _time = OrchestrationTime.Scaled;
            private float _localScale = 1f;
            private bool _paused;
            #endregion

            #region Properties
            public OrchestrationTime Time
            {
                get { return _time; }
                set { _time = value; }
            }

            public float LocalScale
            {
                get { return _localScale; }
                set { _localScale = value; }
            }

            public bool Paused
            {
                get { return _paused; }
                set { _paused = value; }
            }

            public static OrchestratorOptions Default
            {
                get { return new OrchestratorOptions(); }
            }
            #endregion

            #region Constructors
            public OrchestratorOptions()
            {
            }
            #endregion

            #region Methods
            #endregion

            #region Lifecycle Methods
            #endregion

            #region Housekeeping Methods
            public override string ToString()
            {
                return "OrchestratorOptions(Time=" + _time + ", Scale=" + _localScale + ", Paused=" + (_paused ? "true" : "false") + ")";
            }
            #endregion
        }

        /// <summary>
        /// Time source selection used by <see cref="OrchestratorOptions"/>.
        /// </summary>
        public enum OrchestrationTime : sbyte
        {
            Scaled = 0,
            Unscaled = 1
        }

        #endregion

        // =====================================================================
        // Sequence and builder
        // =====================================================================

        #region Sequence And Builder

        /// <summary>
        /// A named sequence of steps with optional guard and callbacks.
        /// </summary>
        public sealed class Sequence
        {
            #region Static Fields
            #endregion

            #region Fields
            private readonly List<IStep> _steps;
            private readonly List<Func<Scene, string?>> _validators;

            private int _index;
            private bool _enteredStep;
            #endregion

            #region Properties
            public bool IsPaused { get; internal set; }
            public string Name { get; private set; }
            public bool Once { get; private set; }
            public bool IsRunning { get; internal set; }
            public IGuard? Guard { get; private set; }
            public Action? OnStarted { get; private set; }
            public Action? OnCompleted { get; private set; }
            public Action? OnStopped { get; private set; }

            public int StepCount
            {
                get { return _steps.Count; }
            }

            public int CurrentStepIndex
            {
                get { return _index; }
            }
            #endregion

            #region Constructors
            public Sequence(string name,
                List<IStep> steps,
                bool once,
                IGuard? guard,
                Action? onStarted,
                Action? onCompleted,
                Action? onStopped,
                List<Func<Scene, string?>> validators)
            {
                Name = name;
                _steps = steps;
                Once = once;
                Guard = guard;
                OnStarted = onStarted;
                OnCompleted = onCompleted;
                OnStopped = onStopped;
                _validators = validators;
                Reset();
            }
            #endregion

            #region Methods
            public void Reset()
            {
                _index = 0;
                _enteredStep = false;
            }

            public void Tick(float dt, OrchestrationContext ctx)
            {
                if (_index >= _steps.Count)
                {
                    Complete();
                    return;
                }

                IStep step = _steps[_index];

                if (!_enteredStep)
                {
                    step.OnEnter(ctx);
                    _enteredStep = true;
                }

                StepStatus status = step.Tick(dt, ctx);
                if (status == StepStatus.Running)
                    return;

                step.OnExit(ctx);
                _enteredStep = false;
                _index++;

                if (_index >= _steps.Count)
                    Complete();
            }

            internal void ForceStepIndex(int index)
            {
                if (index < 0)
                    index = 0;

                if (index >= _steps.Count)
                    index = _steps.Count - 1;

                _index = index;
                _enteredStep = false;
            }

            internal void ValidateOrThrow(Scene scene)
            {
                if (_validators == null || _validators.Count == 0)
                    return;

                List<string> errors = new List<string>();
                for (int i = 0; i < _validators.Count; i++)
                {
                    Func<Scene, string?> rule = _validators[i];
                    string? message = rule(scene);
                    if (!string.IsNullOrWhiteSpace(message))
                        errors.Add(message);
                }

                _validators.Clear();

                if (errors.Count > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("Sequence '" + Name + "' failed validation:");
                    for (int i = 0; i < errors.Count; i++)
                    {
                        sb.Append(" - ");
                        sb.AppendLine(errors[i]);
                    }

                    throw new InvalidOperationException(sb.ToString());
                }
            }

            private void Complete()
            {
                IsRunning = false;
                if (OnCompleted != null)
                    OnCompleted();
            }
            #endregion

            #region Lifecycle Methods
            #endregion

            #region Housekeeping Methods
            public override string ToString()
            {
                return "Sequence(Name=" + Name + ", Steps=" + _steps.Count + ", Once=" + (Once ? "true" : "false") + ", Running=" + (IsRunning ? "true" : "false") + ")";
            }
            #endregion
        }

        /// <summary>
        /// Fluent builder for constructing and registering sequences.
        /// </summary>
        public sealed partial class Builder
        {
            #region Static Fields
            #endregion

            #region Fields
            private readonly Orchestrator _owner;
            private readonly string _name;
            private readonly List<IStep> _steps = new List<IStep>();
            private readonly List<Func<Scene, string?>> _validators = new List<Func<Scene, string?>>();

            private IGuard? _guard;
            private bool _once;
            private Action? _onStarted;
            private Action? _onCompleted;
            private Action? _onStopped;
            #endregion

            #region Properties
            #endregion

            #region Constructors
            public Builder(Orchestrator owner, string name)
            {
                _owner = owner;
                _name = name;
            }
            #endregion

            #region Methods

            public Builder Once()
            {
                _once = true;
                return this;
            }

            public Builder WithGuard(IGuard guard)
            {
                _guard = guard;
                return this;
            }

            public Builder OnStarted(Action callback)
            {
                _onStarted = callback;
                return this;
            }

            public Builder OnCompleted(Action callback)
            {
                _onCompleted = callback;
                return this;
            }

            public Builder OnStopped(Action callback)
            {
                _onStopped = callback;
                return this;
            }

            public Builder Validate(Func<Scene, string?> rule)
            {
                if (rule != null)
                    _validators.Add(rule);

                return this;
            }

            // Basic steps -

            public Builder Do(Action<SequenceAPI> action)
            {
                _steps.Add(new Steps.ActionStep(action));
                return this;
            }

            public Builder WaitSeconds(float seconds)
            {
                _steps.Add(new Steps.WaitSecondsStep(seconds));
                return this;
            }

            public Builder WaitFrames(int frames)
            {
                _steps.Add(new Steps.WaitFramesStep(frames));
                return this;
            }

            public Builder Publish(object evt)
            {
                _steps.Add(new Steps.PublishStep(() => evt));
                return this;
            }

            public Builder Publish(Func<object> evtFactory)
            {
                _steps.Add(new Steps.PublishStep(evtFactory));
                return this;
            }

            public Builder CallSystem<TSystem>(Action<TSystem> call)
                where TSystem : SystemBase
            {
                _steps.Add(new Steps.CallSystemStep<TSystem>(call));
                return this;
            }

            public Builder StartSequence(string name)
            {
                _steps.Add(new Steps.StartSequenceStep(name));
                return this;
            }

            public Builder StopSequence(string name)
            {
                _steps.Add(new Steps.StopSequenceStep(name));
                return this;
            }

            // Component and transform helpers 

            public Builder ToggleComponent<T>(T component, bool enabled)
                where T : Component
            {
                _steps.Add(new Steps.ToggleComponentInstanceStep<T>(component, enabled));
                return this;
            }

            public Builder ToggleComponentOnGO<T>(GameObject gameObject, bool enabled)
                where T : Component
            {
                _steps.Add(new Steps.ToggleComponentOnGOStep<T>(gameObject, enabled));
                return this;
            }

            public Builder MoveTo(Transform transform,
                Vector3 worldTarget,
                float durationSeconds,
                Func<float, float>? ease = null)
            {
                _steps.Add(new Steps.MoveToWorldStep(transform, worldTarget, durationSeconds, ease));
                return this;
            }

            public Builder RotateTo(Transform transform,
                Quaternion worldTarget,
                float durationSeconds,
                Func<float, float>? ease = null)
            {
                _steps.Add(new Steps.RotateToWorldStep(transform, worldTarget, durationSeconds, ease));
                return this;
            }

            public Builder RotateEulerTo(Transform transform,
                Vector3 worldEulerRadians,
                float durationSeconds,
                Func<float, float>? ease = null)
            {
                Quaternion target = Quaternion.CreateFromYawPitchRoll(worldEulerRadians.Y, worldEulerRadians.X, worldEulerRadians.Z);
                _steps.Add(new Steps.RotateToWorldStep(transform, target, durationSeconds, ease));
                return this;
            }

            public Builder ScaleTo(Transform transform,
                Vector3 targetLocalScale,
                float durationSeconds,
                Func<float, float>? ease = null)
            {
                _steps.Add(new Steps.ScaleToStep(transform, targetLocalScale, durationSeconds, ease));
                return this;
            }

            public Builder ScaleTo(Transform transform,
                float uniformScale,
                float durationSeconds,
                Func<float, float>? ease = null)
            {
                Vector3 scale = new Vector3(uniformScale, uniformScale, uniformScale);
                return ScaleTo(transform, scale, durationSeconds, ease);
            }

            public Builder LookAt(Transform transform,
                Vector3 worldTargetPoint,
                float durationSeconds,
                Func<float, float>? ease = null,
                Vector3? upWorld = null)
            {
                Vector3 up = upWorld ?? Vector3.Up;
                _steps.Add(new Steps.LookAtToWorldStep(transform, worldTargetPoint, up, durationSeconds, ease));
                return this;
            }

            public Builder Follow(Transform subject,
                AnimationCurve3D path,
                float playSeconds,
                Vector3? upWorld = null,
                Vector3? worldOffset = null)
            {
                Vector3 up = upWorld ?? Vector3.Up;
                Vector3 offset = worldOffset ?? Vector3.Zero;
                _steps.Add(new Steps.FollowAndLookAtStep(subject, path, playSeconds, null, null, up, offset));
                return this;
            }

            public Builder FollowAndLookAt(Transform subject,
                AnimationCurve3D path,
                float playSeconds,
                Transform lookAtTarget,
                Vector3? upWorld = null,
                Vector3? worldOffset = null)
            {
                Vector3 up = upWorld ?? Vector3.Up;
                Vector3 offset = worldOffset ?? Vector3.Zero;
                _steps.Add(new Steps.FollowAndLookAtStep(subject, path, playSeconds, lookAtTarget, null, up, offset));
                return this;
            }

            public Builder FollowAndLookAt(Transform subject,
                AnimationCurve3D path,
                float playSeconds,
                Func<Vector3>? worldTargetProvider,
                Vector3? upWorld = null,
                Vector3? worldOffset = null)
            {
                Vector3 up = upWorld ?? Vector3.Up;
                Vector3 offset = worldOffset ?? Vector3.Zero;
                _steps.Add(new Steps.FollowAndLookAtStep(subject, path, playSeconds, null, worldTargetProvider, up, offset));
                return this;
            }

            // Control flow and concurrency -

            public Builder If(Func<OrchestrationContext, bool> predicate,
                Action<SubBuilder> thenBranch,
                Action<SubBuilder>? elseBranch = null)
            {
                List<IStep> thenSteps = new List<IStep>();
                List<IStep>? elseSteps = null;

                if (elseBranch != null)
                    elseSteps = new List<IStep>();

                SubBuilder thenBuilder = new SubBuilder(thenSteps);
                thenBranch(thenBuilder);

                if (elseBranch != null && elseSteps != null)
                {
                    SubBuilder elseBuilder = new SubBuilder(elseSteps);
                    elseBranch(elseBuilder);
                }

                _steps.Add(new Steps.IfElseStep(predicate, thenSteps, elseSteps));
                return this;
            }

            public Builder WaitUntil(Func<OrchestrationContext, bool> predicate,
                float? timeoutSeconds = null,
                Steps.TimeoutPolicy onTimeout = Steps.TimeoutPolicy.Fail)
            {
                _steps.Add(new Steps.WaitUntilStep(predicate, timeoutSeconds, onTimeout));
                return this;
            }

            public Builder WithTimeout(float seconds,
                Action<SubBuilder> inner,
                Steps.TimeoutPolicy policy = Steps.TimeoutPolicy.Fail)
            {
                List<IStep> innerSteps = new List<IStep>();
                SubBuilder builder = new SubBuilder(innerSteps);
                inner(builder);
                _steps.Add(new Steps.TimeoutWrapperStep(seconds, policy, innerSteps));
                return this;
            }

            public Builder Retry(int attempts,
                Action<SubBuilder> inner)
            {
                int tries = Math.Max(1, attempts);
                List<IStep> innerSteps = new List<IStep>();
                SubBuilder builder = new SubBuilder(innerSteps);
                inner(builder);
                _steps.Add(new Steps.RetryWrapperStep(tries, innerSteps));
                return this;
            }

            public Builder ParallelAll(Action<SubBuilder> composeChildren)
            {
                List<IStep> children = new List<IStep>();
                SubBuilder builder = new SubBuilder(children);
                composeChildren(builder);
                _steps.Add(new Steps.ParallelAllStep(children));
                return this;
            }

            public Builder ParallelAny(Action<SubBuilder> composeChildren)
            {
                List<IStep> children = new List<IStep>();
                SubBuilder builder = new SubBuilder(children);
                composeChildren(builder);
                _steps.Add(new Steps.ParallelAnyStep(children));
                return this;
            }

            public Builder Barrier(string name, int expectedCount)
            {
                int count = expectedCount;
                if (count < 1)
                    count = 1;

                _steps.Add(new Steps.BarrierStep(name, count));
                return this;
            }

            /// <summary>
            /// Finalise and register the sequence with the owning <see cref="Orchestrator"/>.
            /// </summary>
            public void Register()
            {
                Sequence seq = new Sequence(_name, _steps, _once, _guard, _onStarted, _onCompleted, _onStopped, _validators);
                _owner.Register(seq);
            }
            #endregion

            #region Lifecycle Methods
            #endregion

            #region Housekeeping Methods
            public override string ToString()
            {
                return "Builder(Name=" + _name + ", Steps=" + _steps.Count + ")";
            }
            #endregion
        }

        /// <summary>
        /// Lightweight builder used for nested sub-graphs (If, Parallel, Timeout).
        /// </summary>
        public sealed class SubBuilder
        {
            #region Static Fields
            #endregion

            #region Fields
            private readonly List<IStep> _steps;
            #endregion

            #region Properties
            #endregion

            #region Constructors
            public SubBuilder(List<IStep> steps)
            {
                _steps = steps;
            }
            #endregion

            #region Methods
            public SubBuilder Do(Action<SequenceAPI> action)
            {
                _steps.Add(new Steps.ActionStep(action));
                return this;
            }

            public SubBuilder WaitSeconds(float seconds)
            {
                _steps.Add(new Steps.WaitSecondsStep(seconds));
                return this;
            }

            public SubBuilder WaitFrames(int frames)
            {
                _steps.Add(new Steps.WaitFramesStep(frames));
                return this;
            }

            public SubBuilder Publish(object evt)
            {
                _steps.Add(new Steps.PublishStep(() => evt));
                return this;
            }

            public SubBuilder Publish(Func<object> evtFactory)
            {
                _steps.Add(new Steps.PublishStep(evtFactory));
                return this;
            }

            public SubBuilder CallSystem<TSystem>(Action<TSystem> call)
                where TSystem : SystemBase
            {
                _steps.Add(new Steps.CallSystemStep<TSystem>(call));
                return this;
            }
            #endregion

            #region Lifecycle Methods
            #endregion

            #region Housekeeping Methods
            public override string ToString()
            {
                return "SubBuilder(Steps=" + _steps.Count + ")";
            }
            #endregion
        }

        /// <summary>
        /// Minimal API exposed to steps for starting and stopping sequences.
        /// </summary>
        public readonly struct SequenceAPI
        {
            #region Fields
            private readonly Orchestrator _owner;
            private readonly Scene _scene;
            private readonly EngineContext _context;
            #endregion

            #region Properties
            public Scene Scene
            {
                get { return _scene; }
            }

            public EngineContext Context
            {
                get { return _context; }
            }
            #endregion

            #region Constructors
            public SequenceAPI(Orchestrator owner, Scene scene, EngineContext context)
            {
                _owner = owner;
                _scene = scene;
                _context = context;
            }
            #endregion

            #region Methods
            public void Start(string name)
            {
                _owner.Start(name, _scene, _context);
            }

            public void Stop(string name)
            {
                _owner.Stop(name);
            }
            #endregion

            #region Lifecycle Methods
            #endregion

            #region Housekeeping Methods
            public override string ToString()
            {
                return "SequenceAPI(Scene=" + (_scene != null ? _scene.Name : "null") + ")";
            }
            #endregion
        }

        #endregion

        // =====================================================================
        // Step contracts and built-in steps
        // =====================================================================

        #region Steps

        /// <summary>
        /// Minimal contract for orchestration steps.
        /// </summary>
        public interface IStep
        {
            void OnEnter(OrchestrationContext ctx);

            StepStatus Tick(float dt, OrchestrationContext ctx);

            void OnExit(OrchestrationContext ctx);
        }

        /// <summary>
        /// Common step implementations used by the orchestrator.
        /// </summary>
        public static partial class Steps
        {
            #region Static Fields
            #endregion

            #region Inner Types

            public enum TimeoutPolicy : sbyte
            {
                Fail = 0,
                SkipStep = 1,
                Succeed = 2
            }

            // Timing 

            public sealed class WaitSecondsStep : IStep
            {
                #region Fields
                private readonly float _seconds;
                private float _elapsed;
                #endregion

                #region Constructors
                public WaitSecondsStep(float seconds)
                {
                    if (seconds < 0f)
                        seconds = 0f;

                    _seconds = seconds;
                    _elapsed = 0f;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _elapsed = 0f;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    _elapsed += dt;
                    if (_elapsed < _seconds)
                        return StepStatus.Running;

                    return StepStatus.Succeeded;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class WaitFramesStep : IStep
            {
                #region Fields
                private readonly int _frames;
                private int _count;
                #endregion

                #region Constructors
                public WaitFramesStep(int frames)
                {
                    if (frames < 0)
                        frames = 0;

                    _frames = frames;
                    _count = 0;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _count = 0;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    _count++;
                    if (_count <= _frames)
                        return StepStatus.Running;

                    return StepStatus.Succeeded;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            // Events and systems 

            public sealed class PublishStep : IStep
            {
                #region Fields
                private readonly Func<object> _factory;
                private bool _done;
                #endregion

                #region Constructors
                public PublishStep(Func<object> factory)
                {
                    _factory = factory;
                    _done = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    object evt = _factory();
                    ctx.Emit(evt);
                    _done = true;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_done)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class CallSystemStep<TSystem> : IStep
                where TSystem : SystemBase
            {
                #region Fields
                private readonly Action<TSystem> _call;
                private bool _done;
                #endregion

                #region Constructors
                public CallSystemStep(Action<TSystem> call)
                {
                    _call = call;
                    _done = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    TSystem? sys = ctx.Scene.GetSystem<TSystem>();
                    if (sys != null)
                        _call(sys);

                    _done = true;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_done)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            // Sequence control 

            public sealed class StartSequenceStep : IStep
            {
                #region Fields
                private readonly string _name;
                private bool _done;
                #endregion

                #region Constructors
                public StartSequenceStep(string name)
                {
                    _name = name;
                    _done = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    OrchestrationSystem? sys = ctx.Scene.GetSystem<OrchestrationSystem>();
                    if (sys != null)
                        sys.Orchestrator.Start(_name, ctx.Scene, ctx.Context);

                    _done = true;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_done)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class StopSequenceStep : IStep
            {
                #region Fields
                private readonly string _name;
                private bool _done;
                #endregion

                #region Constructors
                public StopSequenceStep(string name)
                {
                    _name = name;
                    _done = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    OrchestrationSystem? sys = ctx.Scene.GetSystem<OrchestrationSystem>();
                    if (sys != null)
                        sys.Orchestrator.Stop(_name);

                    _done = true;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_done)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class ActionStep : IStep
            {
                #region Fields
                private readonly Action<SequenceAPI> _action;
                private bool _done;
                #endregion

                #region Constructors
                public ActionStep(Action<SequenceAPI> action)
                {
                    _action = action;
                    _done = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    OrchestrationSystem? sys = ctx.Scene.GetSystem<OrchestrationSystem>();
                    if (sys != null)
                    {
                        SequenceAPI api = new SequenceAPI(sys.Orchestrator, ctx.Scene, ctx.Context);
                        _action(api);
                    }

                    _done = true;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_done)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            // Component toggles 

            public sealed class ToggleComponentInstanceStep<T> : IStep
                where T : Component
            {
                #region Fields
                private readonly T _component;
                private readonly bool _enabled;
                private bool _done;
                #endregion

                #region Constructors
                public ToggleComponentInstanceStep(T component, bool enabled)
                {
                    _component = component;
                    _enabled = enabled;
                    _done = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _component.Enabled = _enabled;
                    _done = true;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_done)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class ToggleComponentOnGOStep<T> : IStep
                where T : Component
            {
                #region Fields
                private readonly GameObject _gameObject;
                private readonly bool _enabled;
                private bool _done;
                #endregion

                #region Constructors
                public ToggleComponentOnGOStep(GameObject gameObject, bool enabled)
                {
                    _gameObject = gameObject;
                    _enabled = enabled;
                    _done = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    List<T> components = _gameObject.GetComponents<T>();
                    for (int i = 0; i < components.Count; i++)
                        components[i].Enabled = _enabled;

                    _done = true;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_done)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            // Transform tweens 

            public sealed class MoveToWorldStep : IStep
            {
                #region Fields
                private readonly Transform _transform;
                private readonly Vector3 _worldTarget;
                private readonly float _duration;
                private readonly Func<float, float> _ease;

                private Vector3 _startLocal;
                private Vector3 _destLocal;
                private float _t;
                #endregion

                #region Constructors
                public MoveToWorldStep(Transform transform,
                    Vector3 worldTarget,
                    float durationSeconds,
                    Func<float, float>? ease)
                {
                    _transform = transform;
                    _worldTarget = worldTarget;
                    if (durationSeconds < 0f)
                        durationSeconds = 0f;
                    _duration = durationSeconds;
                    _ease = ease ?? Ease.Linear;
                    _startLocal = Vector3.Zero;
                    _destLocal = Vector3.Zero;
                    _t = 0f;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    Transform? parent = _transform.Parent;
                    _startLocal = _transform.LocalPosition;

                    if (parent == null)
                    {
                        _destLocal = _worldTarget;
                    }
                    else
                    {
                        Matrix invParent = Matrix.Invert(parent.WorldMatrix);
                        _destLocal = Vector3.Transform(_worldTarget, invParent);
                    }

                    _t = 0f;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_duration <= 0f)
                    {
                        _transform.TranslateTo(_destLocal);
                        return StepStatus.Succeeded;
                    }

                    _t += dt;
                    float u = _t / _duration;
                    if (u < 0f)
                        u = 0f;
                    if (u > 1f)
                        u = 1f;

                    float w = _ease(u);
                    Vector3 current = Vector3.Lerp(_startLocal, _destLocal, w);
                    _transform.TranslateTo(current);

                    if (_t < _duration)
                        return StepStatus.Running;

                    return StepStatus.Succeeded;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class RotateToWorldStep : IStep
            {
                #region Fields
                private readonly Transform _transform;
                private readonly Quaternion _worldTarget;
                private readonly float _duration;
                private readonly Func<float, float> _ease;

                private Quaternion _startLocal;
                private Quaternion _destLocal;
                private float _t;
                #endregion

                #region Constructors
                public RotateToWorldStep(Transform transform,
                    Quaternion worldTarget,
                    float durationSeconds,
                    Func<float, float>? ease)
                {
                    _transform = transform;
                    _worldTarget = Quaternion.Normalize(worldTarget);
                    if (durationSeconds < 0f)
                        durationSeconds = 0f;
                    _duration = durationSeconds;
                    _ease = ease ?? Ease.Linear;
                    _startLocal = Quaternion.Identity;
                    _destLocal = Quaternion.Identity;
                    _t = 0f;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    Transform? parent = _transform.Parent;
                    Quaternion worldCurrent = _transform.Rotation;
                    if (parent == null)
                    {
                        _startLocal = worldCurrent;
                        _destLocal = _worldTarget;
                    }
                    else
                    {
                        Quaternion invParent = Quaternion.Inverse(parent.Rotation);
                        _startLocal = Quaternion.Normalize(Quaternion.Concatenate(worldCurrent, invParent));
                        _destLocal = Quaternion.Normalize(Quaternion.Concatenate(_worldTarget, invParent));
                    }

                    _t = 0f;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_duration <= 0f)
                    {
                        ApplyToLocal(_destLocal);
                        return StepStatus.Succeeded;
                    }

                    _t += dt;
                    float u = _t / _duration;
                    if (u < 0f)
                        u = 0f;
                    if (u > 1f)
                        u = 1f;

                    float w = _ease(u);
                    Quaternion localTarget = Quaternion.Slerp(_startLocal, _destLocal, w);
                    ApplyToLocal(localTarget);

                    if (_t < _duration)
                        return StepStatus.Running;

                    return StepStatus.Succeeded;
                }

                private void ApplyToLocal(Quaternion desiredLocal)
                {
                    Transform? parent = _transform.Parent;
                    Quaternion worldNow = _transform.Rotation;

                    Quaternion localNow;
                    if (parent == null)
                    {
                        localNow = worldNow;
                    }
                    else
                    {
                        Quaternion invParent = Quaternion.Inverse(parent.Rotation);
                        localNow = Quaternion.Normalize(Quaternion.Concatenate(worldNow, invParent));
                    }

                    Quaternion deltaLocal = Quaternion.Normalize(Quaternion.Concatenate(desiredLocal, Quaternion.Inverse(localNow)));
                    _transform.RotateBy(deltaLocal, false);
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class ScaleToStep : IStep
            {
                #region Fields
                private readonly Transform _transform;
                private readonly Vector3 _targetLocalScale;
                private readonly float _duration;
                private readonly Func<float, float> _ease;

                private Vector3 _startLocalScale;
                private float _t;
                #endregion

                #region Constructors
                public ScaleToStep(Transform transform,
                    Vector3 targetLocalScale,
                    float durationSeconds,
                    Func<float, float>? ease)
                {
                    _transform = transform;
                    _targetLocalScale = targetLocalScale;
                    if (durationSeconds < 0f)
                        durationSeconds = 0f;
                    _duration = durationSeconds;
                    _ease = ease ?? Ease.Linear;
                    _startLocalScale = Vector3.One;
                    _t = 0f;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _startLocalScale = _transform.LocalScale;
                    _t = 0f;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_duration <= 0f)
                    {
                        _transform.ScaleTo(_targetLocalScale);
                        return StepStatus.Succeeded;
                    }

                    _t += dt;
                    float u = _t / _duration;
                    if (u < 0f)
                        u = 0f;
                    if (u > 1f)
                        u = 1f;

                    float w = _ease(u);
                    Vector3 s = Vector3.Lerp(_startLocalScale, _targetLocalScale, w);
                    _transform.ScaleTo(s);

                    if (_t < _duration)
                        return StepStatus.Running;

                    return StepStatus.Succeeded;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            // Look-at and follow 

            public sealed class LookAtToWorldStep : IStep
            {
                #region Fields
                private readonly Transform _transform;
                private readonly Vector3 _worldTargetPoint;
                private readonly Vector3 _upWorld;
                private readonly float _duration;
                private readonly Func<float, float> _ease;

                private RotateToWorldStep? _rotateStep;
                #endregion

                #region Constructors
                public LookAtToWorldStep(Transform transform,
                    Vector3 worldTargetPoint,
                    Vector3 upWorld,
                    float durationSeconds,
                    Func<float, float>? ease)
                {
                    _transform = transform;
                    _worldTargetPoint = worldTargetPoint;
                    _upWorld = upWorld;
                    if (durationSeconds < 0f)
                        durationSeconds = 0f;
                    _duration = durationSeconds;
                    _ease = ease ?? Ease.Linear;
                    _rotateStep = null;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    Vector3 eye = _transform.Position;
                    Vector3 dir = _worldTargetPoint - eye;
                    if (dir.LengthSquared() < 1e-6f)
                    {
                        _rotateStep = new RotateToWorldStep(_transform, _transform.Rotation, 0f, _ease);
                        _rotateStep.OnEnter(ctx);
                        return;
                    }

                    Matrix view = Matrix.CreateLookAt(eye, _worldTargetPoint, _upWorld);
                    Matrix invView = Matrix.Invert(view);
                    Quaternion worldRot = Quaternion.CreateFromRotationMatrix(invView);

                    _rotateStep = new RotateToWorldStep(_transform, Quaternion.Normalize(worldRot), _duration, _ease);
                    _rotateStep.OnEnter(ctx);
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_rotateStep == null)
                        return StepStatus.Succeeded;

                    return _rotateStep.Tick(dt, ctx);
                }

                public void OnExit(OrchestrationContext ctx)
                {
                    if (_rotateStep != null)
                        _rotateStep.OnExit(ctx);
                }
                #endregion
            }

            public sealed class FollowAndLookAtStep : IStep
            {
                #region Fields
                private readonly Transform _subject;
                private readonly AnimationCurve3D _path;
                private readonly float _playSeconds;
                private readonly Transform? _lookAtTarget;
                private readonly Func<Vector3>? _worldTargetProvider;
                private readonly Vector3 _upWorld;
                private readonly Vector3 _worldOffset;

                private double _curveStart;
                private double _curveEnd;
                private float _t;
                #endregion

                #region Constructors
                public FollowAndLookAtStep(Transform subject,
                    AnimationCurve3D path,
                    float playSeconds,
                    Transform? lookAtTarget,
                    Func<Vector3>? worldTargetProvider,
                    Vector3 upWorld,
                    Vector3 worldOffset)
                {
                    _subject = subject;
                    _path = path;
                    if (playSeconds < 0f)
                        playSeconds = 0f;
                    _playSeconds = playSeconds;
                    _lookAtTarget = lookAtTarget;
                    _worldTargetProvider = worldTargetProvider;
                    _upWorld = upWorld;
                    _worldOffset = worldOffset;
                    _curveStart = 0.0;
                    _curveEnd = 0.0;
                    _t = 0f;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _curveStart = _path.StartSeconds;
                    _curveEnd = _path.EndSeconds;
                    _t = 0f;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_curveEnd <= _curveStart)
                    {
                        ApplyAtCurveTime(_curveStart, ctx);
                        return StepStatus.Succeeded;
                    }

                    if (_playSeconds <= 0f)
                    {
                        ApplyAtCurveTime(_curveEnd, ctx);
                        return StepStatus.Succeeded;
                    }

                    _t += dt;
                    float u = _t / _playSeconds;
                    if (u < 0f)
                        u = 0f;
                    if (u > 1f)
                        u = 1f;

                    double curveTime = _curveStart + u * (_curveEnd - _curveStart);
                    ApplyAtCurveTime(curveTime, ctx);

                    if (_t < _playSeconds)
                        return StepStatus.Running;

                    return StepStatus.Succeeded;
                }

                private void ApplyAtCurveTime(double curveTime, OrchestrationContext ctx)
                {
                    Vector3 worldPos = _path.Evaluate(curveTime) + _worldOffset;

                    Transform? parent = _subject.Parent;
                    Vector3 localPos;
                    if (parent == null)
                    {
                        localPos = worldPos;
                    }
                    else
                    {
                        Matrix invParent = Matrix.Invert(parent.WorldMatrix);
                        localPos = Vector3.Transform(worldPos, invParent);
                    }

                    _subject.TranslateTo(localPos);

                    Vector3 lookTarget;
                    bool haveExplicitTarget;

                    if (_lookAtTarget != null)
                    {
                        lookTarget = _lookAtTarget.Position;
                        haveExplicitTarget = true;
                    }
                    else if (_worldTargetProvider != null)
                    {
                        lookTarget = _worldTargetProvider();
                        haveExplicitTarget = true;
                    }
                    else
                    {
                        const double eps = 1e-3;
                        double nextTime = _path.EndSeconds;
                        double candidate = curveTime + eps;
                        if (candidate < nextTime)
                            nextTime = candidate;

                        Vector3 nextPos = _path.Evaluate(nextTime) + _worldOffset;
                        Vector3 dir = nextPos - worldPos;
                        if (dir.LengthSquared() < 1e-8f)
                        {
                            ApplyWorldRotation(_subject.Rotation);
                            return;
                        }

                        lookTarget = worldPos + dir;
                        haveExplicitTarget = false;
                    }

                    Matrix view = Matrix.CreateLookAt(worldPos, lookTarget, _upWorld);
                    Matrix invView = Matrix.Invert(view);
                    Quaternion worldRot = Quaternion.CreateFromRotationMatrix(invView);
                    ApplyWorldRotation(Quaternion.Normalize(worldRot));
                }

                private void ApplyWorldRotation(Quaternion desiredWorld)
                {
                    Transform? parent = _subject.Parent;
                    Quaternion worldNow = _subject.Rotation;

                    Quaternion localNow;
                    if (parent == null)
                    {
                        localNow = worldNow;
                    }
                    else
                    {
                        Quaternion invParent = Quaternion.Inverse(parent.Rotation);
                        localNow = Quaternion.Normalize(Quaternion.Concatenate(worldNow, invParent));
                    }

                    Quaternion desiredLocal;
                    if (parent == null)
                    {
                        desiredLocal = desiredWorld;
                    }
                    else
                    {
                        Quaternion invParent = Quaternion.Inverse(parent.Rotation);
                        desiredLocal = Quaternion.Normalize(Quaternion.Concatenate(desiredWorld, invParent));
                    }

                    Quaternion deltaLocal = Quaternion.Normalize(Quaternion.Concatenate(desiredLocal, Quaternion.Inverse(localNow)));
                    _subject.RotateBy(deltaLocal, false);
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            // Control flow 

            public sealed class IfElseStep : IStep
            {
                #region Fields
                private readonly Func<OrchestrationContext, bool> _predicate;
                private readonly List<IStep> _thenSteps;
                private readonly List<IStep>? _elseSteps;

                private List<IStep>? _active;
                private int _index;
                private bool _enteredStep;
                #endregion

                #region Constructors
                public IfElseStep(Func<OrchestrationContext, bool> predicate,
                    List<IStep> thenSteps,
                    List<IStep>? elseSteps)
                {
                    _predicate = predicate;
                    _thenSteps = thenSteps;
                    _elseSteps = elseSteps;
                    _active = null;
                    _index = 0;
                    _enteredStep = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    bool cond = _predicate(ctx);
                    if (cond)
                        _active = _thenSteps;
                    else
                        _active = _elseSteps ?? new List<IStep>();

                    _index = 0;
                    _enteredStep = false;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_active == null || _active.Count == 0)
                        return StepStatus.Succeeded;

                    if (_index >= _active.Count)
                        return StepStatus.Succeeded;

                    IStep step = _active[_index];

                    if (!_enteredStep)
                    {
                        step.OnEnter(ctx);
                        _enteredStep = true;
                    }

                    StepStatus status = step.Tick(dt, ctx);
                    if (status == StepStatus.Running)
                        return StepStatus.Running;

                    step.OnExit(ctx);
                    _enteredStep = false;
                    _index++;

                    if (_index >= _active.Count)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class WaitUntilStep : IStep
            {
                #region Fields
                private readonly Func<OrchestrationContext, bool> _predicate;
                private readonly float? _timeout;
                private readonly TimeoutPolicy _policy;
                private float _elapsed;
                #endregion

                #region Constructors
                public WaitUntilStep(Func<OrchestrationContext, bool> predicate,
                    float? timeoutSeconds,
                    TimeoutPolicy policy)
                {
                    _predicate = predicate;
                    _timeout = timeoutSeconds;
                    _policy = policy;
                    _elapsed = 0f;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _elapsed = 0f;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_predicate(ctx))
                        return StepStatus.Succeeded;

                    if (_timeout.HasValue)
                    {
                        _elapsed += dt;
                        if (_elapsed >= _timeout.Value)
                        {
                            if (_policy == TimeoutPolicy.SkipStep || _policy == TimeoutPolicy.Succeed)
                                return StepStatus.Succeeded;

                            return StepStatus.Failed;
                        }
                    }

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class TimeoutWrapperStep : IStep
            {
                #region Fields
                private readonly float _timeout;
                private readonly TimeoutPolicy _policy;
                private readonly List<IStep> _inner;

                private int _index;
                private float _elapsed;
                private bool _enteredStep;
                #endregion

                #region Constructors
                public TimeoutWrapperStep(float timeoutSeconds,
                    TimeoutPolicy policy,
                    List<IStep> inner)
                {
                    if (timeoutSeconds < 0f)
                        timeoutSeconds = 0f;

                    _timeout = timeoutSeconds;
                    _policy = policy;
                    _inner = inner;
                    _index = 0;
                    _elapsed = 0f;
                    _enteredStep = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _elapsed = 0f;
                    _index = 0;
                    _enteredStep = false;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_inner.Count == 0)
                        return StepStatus.Succeeded;

                    _elapsed += dt;
                    if (_elapsed > _timeout)
                    {
                        if (_policy == TimeoutPolicy.SkipStep || _policy == TimeoutPolicy.Succeed)
                            return StepStatus.Succeeded;

                        return StepStatus.Failed;
                    }

                    if (_index >= _inner.Count)
                        return StepStatus.Succeeded;

                    IStep step = _inner[_index];
                    if (!_enteredStep)
                    {
                        step.OnEnter(ctx);
                        _enteredStep = true;
                    }

                    StepStatus status = step.Tick(dt, ctx);
                    if (status == StepStatus.Running)
                        return StepStatus.Running;

                    step.OnExit(ctx);
                    _enteredStep = false;
                    _index++;

                    if (_index >= _inner.Count)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class RetryWrapperStep : IStep
            {
                #region Fields
                private readonly int _attempts;
                private readonly List<IStep> _inner;

                private int _currentAttempt;
                private int _index;
                private bool _enteredStep;
                #endregion

                #region Constructors
                public RetryWrapperStep(int attempts, List<IStep> inner)
                {
                    if (attempts < 1)
                        attempts = 1;

                    _attempts = attempts;
                    _inner = inner;
                    _currentAttempt = 0;
                    _index = 0;
                    _enteredStep = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _currentAttempt = 1;
                    _index = 0;
                    _enteredStep = false;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_inner.Count == 0)
                        return StepStatus.Succeeded;

                    if (_index >= _inner.Count)
                        return StepStatus.Succeeded;

                    IStep step = _inner[_index];
                    if (!_enteredStep)
                    {
                        step.OnEnter(ctx);
                        _enteredStep = true;
                    }

                    StepStatus status = step.Tick(dt, ctx);
                    if (status == StepStatus.Running)
                        return StepStatus.Running;

                    step.OnExit(ctx);
                    _enteredStep = false;

                    if (status == StepStatus.Failed)
                    {
                        if (_currentAttempt >= _attempts)
                            return StepStatus.Failed;

                        _currentAttempt++;
                        _index = 0;
                        return StepStatus.Running;
                    }

                    _index++;
                    if (_index >= _inner.Count)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            // Parallel 

            public sealed class ParallelAllStep : IStep
            {
                #region Fields
                private readonly List<IStep> _children;
                private bool[]? _done;
                private bool _entered;
                #endregion

                #region Constructors
                public ParallelAllStep(List<IStep> children)
                {
                    _children = children;
                    _done = null;
                    _entered = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _entered = true;
                    int count = _children.Count;
                    _done = new bool[count];
                    for (int i = 0; i < count; i++)
                        _children[i].OnEnter(ctx);
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (!_entered || _children.Count == 0)
                        return StepStatus.Succeeded;

                    bool anyFail = false;
                    bool allDone = true;

                    for (int i = 0; i < _children.Count; i++)
                    {
                        if (_done == null || _done[i])
                            continue;

                        IStep step = _children[i];
                        StepStatus status = step.Tick(dt, ctx);
                        if (status == StepStatus.Running)
                        {
                            allDone = false;
                            continue;
                        }

                        step.OnExit(ctx);
                        _done[i] = true;
                        if (status == StepStatus.Failed)
                            anyFail = true;
                    }

                    if (anyFail)
                        return StepStatus.Failed;

                    if (allDone)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class ParallelAnyStep : IStep
            {
                #region Fields
                private readonly List<IStep> _children;
                private bool[]? _done;
                private bool _entered;
                #endregion

                #region Constructors
                public ParallelAnyStep(List<IStep> children)
                {
                    _children = children;
                    _done = null;
                    _entered = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _entered = true;
                    int count = _children.Count;
                    _done = new bool[count];
                    for (int i = 0; i < count; i++)
                        _children[i].OnEnter(ctx);
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (!_entered || _children.Count == 0)
                        return StepStatus.Succeeded;

                    bool anySuccess = false;
                    bool allFinished = true;

                    for (int i = 0; i < _children.Count; i++)
                    {
                        if (_done == null || _done[i])
                            continue;

                        IStep step = _children[i];
                        StepStatus status = step.Tick(dt, ctx);
                        if (status == StepStatus.Running)
                        {
                            allFinished = false;
                            continue;
                        }

                        step.OnExit(ctx);
                        _done[i] = true;
                        if (status == StepStatus.Succeeded)
                            anySuccess = true;
                    }

                    if (anySuccess)
                        return StepStatus.Succeeded;

                    if (allFinished)
                        return StepStatus.Failed;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            public sealed class BarrierStep : IStep
            {
                #region Fields
                private readonly string _name;
                private readonly int _expectedCount;
                private bool _arrived;
                #endregion

                #region Constructors
                public BarrierStep(string name, int expectedCount)
                {
                    _name = name;
                    if (expectedCount < 1)
                        expectedCount = 1;

                    _expectedCount = expectedCount;
                    _arrived = false;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    OrchestrationSystem? sys = ctx.Scene.GetSystem<OrchestrationSystem>();
                    if (sys == null)
                        return;

                    Orchestrator orch = sys.Orchestrator;
                    int count = orch.SignalBarrier(_name);
                    if (count >= _expectedCount)
                        _arrived = true;
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_arrived)
                        return StepStatus.Succeeded;

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                }
                #endregion
            }

            #endregion
        }

        /// <summary>
        /// Helper guard implementations.
        /// </summary>
        public static class SimpleGuards
        {
            #region Methods
            public static IGuard From(Func<OrchestrationContext, bool> predicate)
            {
                return new LambdaGuard(predicate);
            }
            #endregion

            #region Inner Types
            private sealed class LambdaGuard : IGuard
            {
                #region Fields
                private readonly Func<OrchestrationContext, bool> _predicate;
                #endregion

                #region Constructors
                public LambdaGuard(Func<OrchestrationContext, bool> predicate)
                {
                    _predicate = predicate;
                }
                #endregion

                #region Methods
                public bool CanStart(OrchestrationContext ctx)
                {
                    return _predicate(ctx);
                }
                #endregion

                #region Lifecycle Methods
                #endregion

                #region Housekeeping Methods
                public override string ToString()
                {
                    return "LambdaGuard";
                }
                #endregion
            }
            #endregion
        }

        #endregion
    }
}
