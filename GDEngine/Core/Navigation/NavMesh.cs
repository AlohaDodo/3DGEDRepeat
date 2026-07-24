using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// Simple graph-based navigation mesh on the XZ plane.
    /// Stores nodes with world positions, walkable flags, and neighbour links.
    /// </summary>
    public sealed class NavMesh : INavGraphEditable
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly List<NavNode> _nodes = new List<NavNode>(256);
        #endregion

        #region Properties
        public int NodeCount
        {
            get
            {
                return _nodes.Count;
            }
        }
        #endregion

        #region Constructors
        public NavMesh()
        {
        }
        #endregion

        #region Methods
        public void Clear()
        {
            _nodes.Clear();
        }

        public int AddNode(Vector3 position, bool walkable)
        {
            var node = new NavNode(_nodes.Count, position, walkable);
            _nodes.Add(node);
            return node.Id;
        }

        public void AddLink(int a, int b, float cost)
        {
            if (!IsValidNode(a))
                return;

            if (!IsValidNode(b))
                return;

            if (a == b)
                return;

            NavNode na = _nodes[a];
            NavNode nb = _nodes[b];

            if (cost <= 0f)
            {
                cost = Vector3.Distance(na.Position, nb.Position);
            }

            na.AddNeighbour(b, cost);
            nb.AddNeighbour(a, cost);

            _nodes[a] = na;
            _nodes[b] = nb;
        }

        public bool IsWalkable(int nodeId)
        {
            if (!IsValidNode(nodeId))
                throw new ArgumentOutOfRangeException(nameof(nodeId));

            return _nodes[nodeId].Walkable;
        }

        public Vector3 GetNodePosition(int nodeId)
        {
            if (!IsValidNode(nodeId))
                throw new ArgumentOutOfRangeException(nameof(nodeId));

            return _nodes[nodeId].Position;
        }

        public int GetNeighbourCount(int nodeId)
        {
            if (!IsValidNode(nodeId))
                throw new ArgumentOutOfRangeException(nameof(nodeId));

            return _nodes[nodeId].NeighbourCount;
        }

        public int GetNeighbourId(int nodeId, int neighbourIndex)
        {
            if (!IsValidNode(nodeId))
                throw new ArgumentOutOfRangeException(nameof(nodeId));

            var node = _nodes[nodeId];

            if (neighbourIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(neighbourIndex));

            if (neighbourIndex >= node.NeighbourCount)
                throw new ArgumentOutOfRangeException(nameof(neighbourIndex));

            return node.Neighbours[neighbourIndex];
        }

        public float GetNeighbourCost(int nodeId, int neighbourIndex)
        {
            if (!IsValidNode(nodeId))
                throw new ArgumentOutOfRangeException(nameof(nodeId));

            var node = _nodes[nodeId];

            if (neighbourIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(neighbourIndex));

            if (neighbourIndex >= node.NeighbourCount)
                throw new ArgumentOutOfRangeException(nameof(neighbourIndex));

            return node.Costs[neighbourIndex];
        }

        public int FindClosestWalkableNode(Vector3 point)
        {
            int bestId = -1;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];

                if (!node.Walkable)
                    continue;

                float dx = node.Position.X - point.X;
                float dz = node.Position.Z - point.Z;
                float d2 = dx * dx + dz * dz;

                if (d2 < bestDistSq)
                {
                    bestDistSq = d2;
                    bestId = node.Id;
                }
            }

            return bestId;
        }
        #endregion

        #region Methods (private)
        private bool IsValidNode(int id)
        {
            return id >= 0 && id < _nodes.Count;
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return $"NavMesh(Nodes={_nodes.Count})";
        }
        #endregion

        #region Inner Types
        private struct NavNode
        {
            public readonly int Id;
            public Vector3 Position;
            public bool Walkable;
            public readonly List<int> Neighbours;
            public readonly List<float> Costs;

            public int NeighbourCount
            {
                get
                {
                    return Neighbours.Count;
                }
            }

            public NavNode(int id, Vector3 position, bool walkable)
            {
                Id = id;
                Position = position;
                Walkable = walkable;
                Neighbours = new List<int>(4);
                Costs = new List<float>(4);
            }

            public void AddNeighbour(int neighbourId, float cost)
            {
                Neighbours.Add(neighbourId);
                Costs.Add(cost);
            }
        }
        #endregion
    }
}
