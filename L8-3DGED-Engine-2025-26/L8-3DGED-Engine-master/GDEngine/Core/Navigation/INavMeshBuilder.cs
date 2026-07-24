
namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// Builder abstraction for constructing navigation meshes.
    /// </summary>
    public interface INavMeshBuilder
    {
        #region Methods
        void Build(INavGraphEditable navGraph);
        #endregion
    }
}
