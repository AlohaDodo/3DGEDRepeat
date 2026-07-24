using GDEngine.Core.Components;
using GDEngine.Core.Services;

namespace GDEngine.Core.Impulses
{
    /// <summary>
    /// Base component for impulse listeners that work with the shared ImpulseBus.
    /// Implements IImpulseListener{T} and centralises unsubscription and
    /// optional channel filtering for impulse payloads such as Eased3DImpulse.
    /// Subscription is left to derived classes.
    /// </summary>
    /// <typeparam name="T">Impulse payload type.</typeparam>
    /// <see cref="ImpulseBus"/>
    /// <see cref="IImpulseListener{T}"/>
    public abstract class ImpulseListenerBase<T> : Component, IImpulseListener<T>, IDisposable
    {
        #region Fields

        /// <summary>
        /// The active subscription for this listener. Derived classes should assign this
        /// when subscribing and let <see cref="Unsubscribe"/> dispose it.
        /// </summary>
        protected IDisposable? _subscription;

        #endregion

        #region Properties

        /// <summary>
        /// Logical channel this listener is interested in.
        /// For impulse types that support channels (e.g. <see cref="Eased3DImpulse"/>),
        /// only impulses with a matching channel will be delivered.
        /// For other impulse types this property is ignored.
        /// Return <c>string.Empty</c> or <c>null</c> to receive all channels.
        /// </summary>
        protected virtual string? Channel
        {
            get { return null; }
        }

        /// <summary>
        /// Convenience access to the global <see cref="ImpulseBus"/> from the <see cref="EngineContext"/>.
        /// Derived classes can use this when implementing <see cref="Subscribe"/>.
        /// </summary>
        protected ImpulseBus Impulses
        {
            get
            {
                if (EngineContext.Instance == null)
                    throw new NullReferenceException(nameof(EngineContext.Instance));

                if (EngineContext.Instance.Impulses == null)
                    throw new NullReferenceException("EngineContext.Instance.Impulses");

                return EngineContext.Instance.Impulses;
            }
        }

        #endregion

        #region Constructors
        // No automatic subscription here; derived classes must call Subscribe() explicitly.
        #endregion

        #region Methods

        /// <summary>
        /// Handles an incoming impulse payload.
        /// Implemented by derived listeners to apply domain-specific behaviour.
        /// </summary>
        /// <param name="impulse">Impulse payload.</param>
        public abstract void OnImpulse(T impulse);

        /// <summary>
        /// Implemented by derived listeners to subscribe this listener to the <see cref="ImpulseBus"/>.
        /// Typically uses <see cref="Impulses"/> and assigns <see cref="_subscription"/>.
        /// </summary>
        public abstract void Subscribe();

        /// <summary>
        /// Unsubscribes this listener from its current <see cref="ImpulseBus"/>, if any.
        /// Safe to call multiple times.
        /// </summary>
        public void Unsubscribe()
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
            }
        }

        #endregion

        #region Lifecycle Methods

        protected override void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Housekeeping Methods

        public void Dispose()
        {
            Unsubscribe();
        }

        #endregion
    }
}
