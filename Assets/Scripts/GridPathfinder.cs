using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder
{
    private readonly int[,] _map;
    private readonly int _width;
    private readonly int _height;
    private readonly float[,] _costs;

    private readonly Vector2Int[] _dirs =
    {
        new Vector2Int(1,0),
        new Vector2Int(-1,0),
        new Vector2Int(0,1),
        new Vector2Int(0,-1)
    };

    public GridPathfinder(int[,] map, float[,] costs)
    {
        _map = map;
        _width = map.GetLength(0);
        _height = map.GetLength(1);
        _costs = costs;
    }

    public bool TryFindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> outPath)
    {
        outPath.Clear();
        var frontier = new SimplePriorityQueue();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var costSoFar = new Dictionary<Vector2Int, float>();

        frontier.Enqueue(start, 0);
        cameFrom[start] = start;
        costSoFar[start] = 0f;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == goal)
                break;

            foreach (var dir in _dirs)
            {
                Vector2Int next = current + dir;
                if (!IsWalkable(next))
                    continue;

                float newCost = costSoFar[current] + GetTileCost(next);
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    float priority = newCost + Heuristic(next, goal);
                    frontier.Enqueue(next, priority);
                    cameFrom[next] = current;
                }
            }
        }

        if (!cameFrom.ContainsKey(goal))
            return false;

        Vector2Int node = goal;
        while (node != start)
        {
            outPath.Add(node);
            node = cameFrom[node];
        }
        outPath.Add(start);
        outPath.Reverse();
        return true;
    }

    private bool IsWalkable(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= _width || cell.y < 0 || cell.y >= _height)
            return false;
        return _map[cell.x, cell.y] == 0;
    }

    private float GetTileCost(Vector2Int cell)
    {
        if (_costs == null)
            return 1f;
        return Mathf.Max(0.1f, _costs[cell.x, cell.y]);
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private class SimplePriorityQueue
    {
        private readonly List<(Vector2Int node, float priority)> _elements = new List<(Vector2Int, float)>();

        public int Count => _elements.Count;

        public void Enqueue(Vector2Int item, float priority)
        {
            _elements.Add((item, priority));
        }

        public Vector2Int Dequeue()
        {
            int bestIndex = 0;
            float bestPriority = _elements[0].priority;
            for (int i = 1; i < _elements.Count; i++)
            {
                if (_elements[i].priority < bestPriority)
                {
                    bestPriority = _elements[i].priority;
                    bestIndex = i;
                }
            }

            Vector2Int best = _elements[bestIndex].node;
            _elements.RemoveAt(bestIndex);
            return best;
        }
    }
}
