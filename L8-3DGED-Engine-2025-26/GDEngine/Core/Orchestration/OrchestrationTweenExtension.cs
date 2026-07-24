using GDEngine.Core.Timing;

namespace GDEngine.Core.Orchestration
{
    public partial class Orchestrator
    {
        public partial class Builder
        {
            /// <summary>
            /// Animate a value over time using an easing function.
            /// The tween callback receives a value from 0 to 1 (or custom range) eased over the duration.
            /// </summary>
            /// <param name="durationSeconds">Duration of the tween in seconds</param>
            /// <param name="onTween">Callback invoked each frame with the current tweened value (0-1)</param>
            /// <param name="ease">Easing function (defaults to Linear)</param>
            /// <param name="from">Starting value (defaults to 0)</param>
            /// <param name="to">Ending value (defaults to 1)</param>
            /// <returns>Builder for chaining</returns>
            /// <example>
            /// // Fade in a panel over 1 second
            /// orchestrator.Build("FadeIn")
            ///     .Tween(1f, t => panel.Alpha = t, Ease.QuadOut)
            ///     .Register();
            /// 
            /// // Custom range tween
            /// orchestrator.Build("Countdown")
            ///     .Tween(5f, value => countdownText = value.ToString(), Ease.Linear, from: 5f, to: 0f)
            ///     .Register();
            /// </example>
            public Builder Tween(
                float durationSeconds,
                Action<float> onTween,
                Func<float, float>? ease = null,
                float from = 0f,
                float to = 1f)
            {
                _steps.Add(new Steps.TweenStep(durationSeconds, onTween, ease, from, to));
                return this;
            }

            /// <summary>
            /// Animate a value over time with access to OrchestrationContext.
            /// Useful when you need scene or context information during the tween.
            /// </summary>
            /// <param name="durationSeconds">Duration of the tween in seconds</param>
            /// <param name="onTween">Callback with context and current value</param>
            /// <param name="ease">Easing function (defaults to Linear)</param>
            /// <param name="from">Starting value (defaults to 0)</param>
            /// <param name="to">Ending value (defaults to 1)</param>
            public Builder TweenWithContext(
                float durationSeconds,
                Action<OrchestrationContext, float> onTween,
                Func<float, float>? ease = null,
                float from = 0f,
                float to = 1f)
            {
                _steps.Add(new Steps.TweenWithContextStep(durationSeconds, onTween, ease, from, to));
                return this;
            }
        }

        public static partial class Steps
        {
            /// <summary>
            /// Generic tween step that animates a value from start to end over a duration.
            /// Invokes a callback each frame with the current eased value.
            /// </summary>
            public sealed class TweenStep : IStep
            {
                #region Fields
                private readonly float _duration;
                private readonly Action<float> _onTween;
                private readonly Func<float, float> _ease;
                private readonly float _from;
                private readonly float _to;

                private float _t;
                #endregion

                #region Constructors
                public TweenStep(
                    float durationSeconds,
                    Action<float> onTween,
                    Func<float, float>? ease,
                    float from,
                    float to)
                {
                    if (onTween == null)
                        throw new ArgumentNullException(nameof(onTween));

                    _duration = Math.Max(0f, durationSeconds);
                    _onTween = onTween;
                    _ease = ease ?? Ease.Linear;
                    _from = from;
                    _to = to;
                    _t = 0f;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _t = 0f;

                    // Invoke with starting value
                    if (_duration <= 0f)
                    {
                        _onTween(_to);
                    }
                    else
                    {
                        _onTween(_from);
                    }
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    // Instant completion if duration is zero
                    if (_duration <= 0f)
                    {
                        _onTween(_to);
                        return StepStatus.Succeeded;
                    }

                    // Advance time
                    _t += dt;

                    // Calculate normalized time (0-1)
                    float u = _t / _duration;
                    u = Math.Clamp(u, 0f, 1f);

                    // Apply easing
                    float easedValue = _ease(u);

                    // Lerp between from and to
                    float currentValue = _from + (_to - _from) * easedValue;

                    // Invoke callback
                    _onTween(currentValue);

                    // Check completion
                    if (_t >= _duration)
                    {
                        // Ensure final value is exact
                        _onTween(_to);
                        return StepStatus.Succeeded;
                    }

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                    // Ensure final value on early exit
                    _onTween(_to);
                }
                #endregion
            }

            /// <summary>
            /// Tween step that provides OrchestrationContext to the callback.
            /// </summary>
            public sealed class TweenWithContextStep : IStep
            {
                #region Fields
                private readonly float _duration;
                private readonly Action<OrchestrationContext, float> _onTween;
                private readonly Func<float, float> _ease;
                private readonly float _from;
                private readonly float _to;

                private float _t;
                #endregion

                #region Constructors
                public TweenWithContextStep(
                    float durationSeconds,
                    Action<OrchestrationContext, float> onTween,
                    Func<float, float>? ease,
                    float from,
                    float to)
                {
                    if (onTween == null)
                        throw new ArgumentNullException(nameof(onTween));

                    _duration = Math.Max(0f, durationSeconds);
                    _onTween = onTween;
                    _ease = ease ?? Ease.Linear;
                    _from = from;
                    _to = to;
                    _t = 0f;
                }
                #endregion

                #region Methods
                public void OnEnter(OrchestrationContext ctx)
                {
                    _t = 0f;

                    if (_duration <= 0f)
                    {
                        _onTween(ctx, _to);
                    }
                    else
                    {
                        _onTween(ctx, _from);
                    }
                }

                public StepStatus Tick(float dt, OrchestrationContext ctx)
                {
                    if (_duration <= 0f)
                    {
                        _onTween(ctx, _to);
                        return StepStatus.Succeeded;
                    }

                    _t += dt;
                    float u = _t / _duration;
                    u = Math.Clamp(u, 0f, 1f);

                    float easedValue = _ease(u);
                    float currentValue = _from + (_to - _from) * easedValue;

                    _onTween(ctx, currentValue);

                    if (_t >= _duration)
                    {
                        _onTween(ctx, _to);
                        return StepStatus.Succeeded;
                    }

                    return StepStatus.Running;
                }

                public void OnExit(OrchestrationContext ctx)
                {
                    _onTween(ctx, _to);
                }
                #endregion
            }
        }
    }
}
