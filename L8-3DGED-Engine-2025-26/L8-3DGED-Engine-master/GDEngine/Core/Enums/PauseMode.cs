using System.ComponentModel;

namespace GDEngine.Core.Enums
{
    /// <summary>
    /// Bitmask describing which parts of a system should be paused.
    /// </summary>
    [Flags]
    public enum PauseMode
    {
        [Description("Ignore pause state; Update and Draw always run.")]
        None = 0,

        [Description("Skip Update when the game is paused; Draw still runs.")]
        Update = 1 << 0,

        [Description("Skip Draw when the game is paused; Update still runs.")]
        Draw = 1 << 1,

        [Description("Skip both Update and Draw when the game is paused.")]
        All = Update | Draw
    }
}
