namespace GDEngine.Core.Enums
{
    /// <summary>
    /// Deterministic execution lifecycle for systems.
    /// </summary>
    public enum FrameLifecycle : sbyte
    {
        EarlyUpdate = 0,  // e.g. Input, Events
        Update = 1,       // e.g. Game logic
        LateUpdate = 2,   // e.g. Transforms, culling, lighting
        Render = 3,       // e.g. Draw calls
        PostRender = 4    // e.g.  Debug overlay, Post-processing
    }

   
}
