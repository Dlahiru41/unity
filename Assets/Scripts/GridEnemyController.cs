using System.Collections.Generic;
using UnityEngine;

public class GridEnemyController : EnemyBase
{
    public float moveSpeed = 2.5f;
    public float rePathInterval = 0.5f;
    public float reachThreshold = 0.1f;

    private GridPathfinder _pathfinder;
    private readonly List<Vector2Int> _path = new List<Vector2Int>();
    private int _pathIndex;
    private float _pathTimer;
    private bool _initialized;

    private ArenaGenerator _arenaGenerator;
    private Rigidbody _rb;

    public void Init(GridPathfinder pathfinder)
    {
        _pathfinder = pathfinder;
        _initialized = true;

        _arenaGenerator = FindObjectOfType<ArenaGenerator>();
        _rb = GetComponent<Rigidbody>();
    }

    protected override void HandleBehaviour()
    {
        if (!_initialized || playerTarget == null || _pathfinder == null)
            return;

        _pathTimer -= Time.deltaTime;
        if (_pathTimer <= 0f)
        {
            Vector2Int start = WorldToCell(transform.position);
            Vector2Int target = WorldToCell(playerTarget.position);
            if (_pathfinder.TryFindPath(start, target, _path))
            {
                _pathIndex = Mathf.Min(1, _path.Count - 1);
            }
            _pathTimer = rePathInterval;
        }

        MoveAlongPath();
        TryFire();
    }

    private void MoveAlongPath()
    {
        if (_path.Count == 0 || _pathIndex >= _path.Count)
            return;

        Vector3 goal = CellToWorld(_path[_pathIndex]);
        Vector3 toGoal = goal - transform.position;
        toGoal.y = 0f;
        if (toGoal.magnitude <= reachThreshold)
        {
            _pathIndex++;
            return;
        }

        Vector3 dir = toGoal.normalized;
        
        // Use Rigidbody movement for proper collision handling
        if (_rb != null)
        {
            Vector3 newPos = _rb.position + dir * (moveSpeed * Time.deltaTime);
            _rb.MovePosition(newPos);
        }
        else
        {
            transform.position += dir * (moveSpeed * Time.deltaTime);
        }
        
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private Vector2Int WorldToCell(Vector3 world)
    {
        if (_arenaGenerator == null)
            return Vector2Int.zero;
        int cx = Mathf.Clamp(Mathf.RoundToInt(world.x / _arenaGenerator.tileSize), 0, _arenaGenerator.width - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(world.z / _arenaGenerator.tileSize), 0, _arenaGenerator.height - 1);
        return new Vector2Int(cx, cy);
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        if (_arenaGenerator == null)
            return Vector3.zero;
        return new Vector3(cell.x * _arenaGenerator.tileSize, 0f, cell.y * _arenaGenerator.tileSize);
    }
}
