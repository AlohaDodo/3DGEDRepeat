using Microsoft.Xna.Framework;

namespace GDEngine.Core.Serialization
{
    /// <summary>
    /// Plain data container for a single model spawn, loaded from JSON.
    /// </summary>
    public class ModelSpawnData
    {
        public Vector3 Position { get; set; }
        public Vector3 RotationDegrees { get; set; }
        public Vector3 Scale { get; set; }
        public string? TextureName { get; set; }
        public string? ModelName { get; set; }
        public string? ObjectName { get; set; }
    }
}
