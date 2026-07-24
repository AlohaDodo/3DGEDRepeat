namespace GDEngine.Core.Serialization
{
    /// <summary>
    /// JSON-driven asset list for preloading and dictionary registration.
    /// </summary>
    /// <see cref="AssetEntry"/>
    public sealed class AssetManifest
    {
        #region Fields
        private List<AssetEntry> _models = new();
        private List<AssetEntry> _textures = new();
        private List<AssetEntry> _fonts = new();
        private List<AssetEntry> _sounds = new();
        private List<AssetEntry> _songs = new();
        private List<AssetEntry> _effects = new();

        //if you want to add more string key/value pairs for an asset then add a List field for that type here and a property.
        #endregion

        #region Properties
        public List<AssetEntry> Models { get => _models; set => _models = value ?? new(); }
        public List<AssetEntry> Textures { get => _textures; set => _textures = value ?? new(); }
        public List<AssetEntry> Fonts { get => _fonts; set => _fonts = value ?? new(); }
        public List<AssetEntry> Sounds { get => _sounds; set => _sounds = value ?? new(); }
        public List<AssetEntry> Songs { get => _songs; set => _songs = value ?? new(); }
        public List<AssetEntry> Effects { get => _effects; set => _effects = value; }
        #endregion
    }

    /// <summary>
    /// Name → Content pipeline path pair.
    /// </summary>
    public sealed class AssetEntry
    {
        #region Fields
        private string _name = string.Empty;
        private string _contentPath = string.Empty;
        #endregion

        #region Properties
        public string Name { get => _name; set => _name = value ?? string.Empty; }
        public string ContentPath { get => _contentPath; set => _contentPath = value ?? string.Empty; }
        #endregion
    }
}
