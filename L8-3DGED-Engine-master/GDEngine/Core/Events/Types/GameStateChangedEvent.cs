namespace GDEngine.Core.Events
{
    /// <summary>
    /// High-level game outcome state managed by <see cref="GameManager"/>.
    /// </summary>
    /// <see cref="GameManager"/>
    public enum GameOutcomeState : sbyte
    {
        InProgress = 0,
        Won = 1,
        Lost = 2,
        Paused = 3
    }

    /// <summary>
    /// Event raised whenever the game outcome state changes.
    /// </summary>
    /// <see cref="GameManager"/>
    public sealed class GameStateChangedEvent
    {
        #region Fields
        public readonly GameOutcomeState OldState;
        public readonly GameOutcomeState NewState;
        #endregion

        #region Constructors
        public GameStateChangedEvent(GameOutcomeState oldState, GameOutcomeState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
        #endregion
    }

    /// <summary>
    /// Event raised when the game transitions to the Won state.
    /// </summary>
    public sealed class GameWonEvent
    {
    }

    /// <summary>
    /// Event raised when the game transitions to the Lost state.
    /// </summary>
    public sealed class GameLostEvent
    {
    }
}
