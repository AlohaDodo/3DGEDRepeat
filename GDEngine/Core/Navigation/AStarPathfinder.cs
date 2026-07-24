
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Navigation
{
    /// <summary>
    /// A* pathfinding strategy that operates on an <see cref="INavGraphReader"/>.
    /// Uses an injected heuristic to support different distance metrics.
    /// </summary>
    public sealed class AStarPathfinder : IPathfinder
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Func<Vector3, Vector3, float> _heuristic;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public AStarPathfinder(Func<Vector3, Vector3, float> heuristic)
        {
            _heuristic = heuristic ?? throw new ArgumentNullException(nameof(heuristic));
        }
        #endregion

        #region Methods
        public bool TryFindPath(
            INavGraphReader graph,
            Vector3 start,
            Vector3 end,
            List<Vector3> worldPath)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            if (worldPath == null)
                throw new ArgumentNullException(nameof(worldPath));

            worldPath.Clear();

            if (graph.NodeCount == 0)
                return false;

            int startId = graph.FindClosestWalkableNode(start);
            int endId = graph.FindClosestWalkableNode(end);

            if (startId < 0)
                return false;

            if (endId < 0)
                return false;

            if (startId == endId)
            {
                worldPath.Add(graph.GetNodePosition(startId));
                return true;
            }

            return RunAStar(graph, startId, endId, worldPath);
        }
        #endregion

        #region Methods (private)
        private bool RunAStar(
            INavGraphReader graph,
            int startId,
            int goalId,
            List<Vector3> worldPath)
        {
            int count = graph.NodeCount;

            float[] gScore = new float[count];
            float[] fScore = new float[count];
            int[] cameFrom = new int[count];
            bool[] openSet = new bool[count];
            bool[] closedSet = new bool[count];

            for (int i = 0; i < count; i++)
            {
                gScore[i] = float.MaxValue;
                fScore[i] = float.MaxValue;
                cameFrom[i] = -1;
                openSet[i] = false;
                closedSet[i] = false;
            }

            gScore[startId] = 0f;
            fScore[startId] = Heuristic(graph, startId, goalId);
            openSet[startId] = true;

            while (true)
            {
                int current = -1;
                float bestF = float.MaxValue;

                for (int i = 0; i < count; i++)
                {
                    if (!openSet[i])
                        continue;

                    if (fScore[i] < bestF)
                    {
                        bestF = fScore[i];
                        current = i;
                    }
                }

                if (current == -1)
                {
                    return false;
                }

                if (current == goalId)
                {
                    ReconstructPath(graph, cameFrom, goalId, worldPath);
                    return true;
                }

                openSet[current] = false;
                closedSet[current] = true;

                int neighbourCount = graph.GetNeighbourCount(current);

                for (int n = 0; n < neighbourCount; n++)
                {
                    int neighbourId = graph.GetNeighbourId(current, n);

                    if (closedSet[neighbourId])
                        continue;

                    if (!graph.IsWalkable(neighbourId))
                        continue;

                    float tentativeG = gScore[current] + graph.GetNeighbourCost(current, n);

                    if (!openSet[neighbourId])
                    {
                        openSet[neighbourId] = true;
                    }
                    else if (tentativeG >= gScore[neighbourId])
                    {
                        continue;
                    }

                    cameFrom[neighbourId] = current;
                    gScore[neighbourId] = tentativeG;
                    fScore[neighbourId] = tentativeG + Heuristic(graph, neighbourId, goalId);
                }
            }
        }

        private float Heuristic(INavGraphReader graph, int aId, int bId)
        {
            Vector3 a = graph.GetNodePosition(aId);
            Vector3 b = graph.GetNodePosition(bId);

            return _heuristic(a, b);
        }

        private void ReconstructPath(
            INavGraphReader graph,
            int[] cameFrom,
            int goalId,
            List<Vector3> worldPath)
        {
            List<int> stack = new List<int>(64);

            int current = goalId;
            while (current != -1)
            {
                stack.Add(current);
                current = cameFrom[current];
            }

            for (int i = stack.Count - 1; i >= 0; i--)
            {
                int nodeId = stack[i];
                worldPath.Add(graph.GetNodePosition(nodeId));
            }
        }
        #endregion
    }
}
