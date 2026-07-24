using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// Builder that constructs a flat grid navigation mesh on the XZ plane.
    /// Useful for prototyping and teaching the Builder pattern.
    /// </summary>
    public sealed class GridNavMeshBuilder : INavMeshBuilder
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly int _width;
        private readonly int _height;
        private readonly float _cellSize;
        private readonly Vector3 _origin;
        private readonly Func<Vector3, bool>? _isWalkable;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public GridNavMeshBuilder(
            int width,
            int height,
            float cellSize,
            Vector3 origin,
            Func<Vector3, bool>? isWalkable = null)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _origin = origin;
            _isWalkable = isWalkable;
        }
        #endregion

        #region Methods
        public void Build(INavGraphEditable navGraph)
        {
            navGraph.Clear();

            if (_width < 1)
                return;

            if (_height < 1)
                return;

            if (_cellSize <= 0f)
                return;

            int[,] ids = new int[_width, _height];

            for (int z = 0; z < _height; z++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Vector3 pos = _origin + new Vector3(x * _cellSize, 0f, z * _cellSize);
                    bool walkable = true;

                    if (_isWalkable != null)
                    {
                        walkable = _isWalkable(pos);
                    }

                    int id = navGraph.AddNode(pos, walkable);
                    ids[x, z] = id;
                }
            }

            for (int z = 0; z < _height; z++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int id = ids[x, z];

                    if (x + 1 < _width)
                    {
                        navGraph.AddLink(id, ids[x + 1, z], 0f);
                    }

                    if (z + 1 < _height)
                    {
                        navGraph.AddLink(id, ids[x, z + 1], 0f);
                    }
                }
            }
        }
        #endregion
    }
}
