
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// Editable navigation graph abstraction used by builders.
    /// </summary>
    public interface INavGraphEditable : INavGraphReader
    {
        #region Methods
        void Clear();

        int AddNode(Vector3 position, bool walkable);

        void AddLink(int a, int b, float cost);
        #endregion
    }
}
