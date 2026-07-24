namespace GDEngine.Core.Events
{
    /// <summary>
    /// Raised whenever the global pause state changes.
    /// </summary>
    public readonly struct GamePauseChangedEvent
    {
        public bool IsPaused { get; }

        public GamePauseChangedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }
}
