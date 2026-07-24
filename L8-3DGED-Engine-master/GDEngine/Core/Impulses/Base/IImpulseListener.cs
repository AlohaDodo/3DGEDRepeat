namespace GDEngine.Core.Impulses
{
    /// <summary>
    /// Implemented by components that respond to impulse events dispatched via the ImpulseBus.
    /// </summary>
    /// <typeparam name="T">Impulse payload type.</typeparam>
    /// <see cref="ImpulseBus"/>
    public interface IImpulseListener<T>
    {
        /// <summary>
        /// Subscribes this listener to the given ImpulseBus.
        /// </summary>
        void Subscribe();

        /// <summary>
        /// Unsubscribes this listener from its current ImpulseBus, if any.
        /// </summary>
        void Unsubscribe();

        /// <summary>
        /// Handles an incoming impulse payload.
        /// </summary>
        /// <param name="impulse">Impulse payload.</param>
        void OnImpulse(T impulse);
    }
}
