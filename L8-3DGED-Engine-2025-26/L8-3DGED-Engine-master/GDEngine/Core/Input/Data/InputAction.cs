namespace GDEngine.Core.Input.Data
{
    public enum InputAction : sbyte
    {
        MoveX = 0,
        MoveY = 1,
        LookX = 2,
        LookY = 3,
        //add more keyboard here
        Jump = 10,
        Fire = 11,
        Boost = 12,

        // mouse wheel support (reported via OnAxis)
        ScrollWheelDelta = 20, // per-frame change (usually ±120 ticks per notch before scaling)
        ScrollWheelValue = 21  // absolute wheel value (scaled)
    }
}
