using System.Collections.Generic;
using UnityEngine;

public class GridEnemyController : EnemyFSM
{
    [Header("Grid Movement")]
    public float moveSpeed = 2.5f;
    public float rePathInterval = 0.5f;
    public float reachThreshold = 0.1f;
    
    [Header("Tactical Settings")]
    public float tacticalThinkingInterval = 1f;
    public float influenceMapRadius = 15f;
    
    [Header("Influence Weights")]
    public float dangerWeight = 2f;
    public float coverWeight = 1.5f;
    public float attackPositionWeight = 1.8f;

    private GridPathfinder _pathfinder;
    private readonly List<Vector2Int> _path = new List<Vector2Int>();
    private int _pathIndex;
    private float _pathTimer;
    private bool _initialized;
    private ArenaGenerator _arenaGenerator;
    private Rigidbody _rb;
    
    // Influence map for tactical positioning
    private float[,] _influenceMap;
    private float _influenceUpdateTimer;
    private Vector3 _tacticalPosition;
    private bool _holdingPosition;

    public void Init(GridPathfinder pathfinder)
    {
        _pathfinder = pathfinder;
        _initialized = true;
        _arenaGenerator = FindObjectOfType<ArenaGenerator>();
        _rb = GetComponent<Rigidbody>();
        
        if (_arenaGenerator != null)
        {
            _influenceMap = new float[_arenaGenerator.width, _arenaGenerator.height];
        }
        
        // Initialize as cautious tactical enemy
        aggressiveness = 0.5f;
        cautiousness = 0.7f;
        teamworkTendency = 0.6f;
    }

    protected override void Update()
    {
        base.Update();
        
        if (_initialized && _arenaGenerator != null)
        {
            _influenceUpdateTimer += Time.deltaTime;
            if (_influenceUpdateTimer >= tacticalThinkingInterval)
            {
                UpdateInfluenceMap();
                _influenceUpdateTimer = 0f;
            }
        }
    }

    protected override void OnStateEnter(State state)
    {
        base.OnStateEnter(state);
        _holdingPosition = false;
        
        switch (state)
        {
            case State.TakeCover:
                _tacticalPosition = FindBestCoverPosition();
                break;
                
            case State.Flank:
                _tacticalPosition = FindBestFlankPosition();
                break;
                
            case State.Ambush:
                _tacticalPosition = FindBestAmbushPosition();
                _holdingPosition = true;
                break;
                
            case State.Strafe:
                _holdingPosition = false;
                break;
        }
    }

    protected override void ExecuteState(State state)
    {
        if (!_initialized || target == null || _pathfinder == null)
            return;

        switch (state)
        {
            case State.Idle:
                ExecuteIdle();
                break;
            case State.Patrol:
                ExecutePatrol();
                break;
            case State.Seek:
                ExecuteSeek();
                break;
            case State.Chase:
                ExecuteChase();
                break;
            case State.Strafe:
                ExecuteStrafe();
                break;
            case State.Retreat:
                ExecuteRetreat();
                break;
            case State.TakeCover:
                ExecuteTakeCover();
                break;
            case State.Ambush:
                ExecuteAmbush();
                break;
            case State.Flank:
                ExecuteFlank();
                break;
        }

        UpdateMovement();
    }

    private void ExecuteIdle()
    {
        _path.Clear();
        if (stateTimer > 2f)
        {
            TransitionToState(State.Patrol);
        }
    }

    private void ExecutePatrol()
    {
        _pathTimer -= Time.deltaTime;
        if (_pathTimer <= 0f)
        {
            Vector2Int randomTarget = GetRandomPatrolPoint();
            Vector2Int start = WorldToCell(transform.position);
            _pathfinder.TryFindPath(start, randomTarget, _path);
            _pathIndex = Mathf.Min(1, _path.Count - 1);
            _pathTimer = rePathInterval * 3f;
        }
    }

    private void ExecuteSeek()
    {
        _pathTimer -= Time.deltaTime;
        if (_pathTimer <= 0f)
        {
            Vector2Int start = WorldToCell(transform.position);
            Vector2Int targetCell = WorldToCell(lastKnownTargetPos);
            if (_pathfinder.TryFindPath(start, targetCell, _path))
            {
                _pathIndex = Mathf.Min(1, _path.Count - 1);
            }
            _pathTimer = rePathInterval;
        }
        
        if (hasLineOfSight)
        {
            TransitionToState(State.Chase);
        }
    }

    private void ExecuteChase()
    {
        if (target == null) return;
        
        _pathTimer -= Time.deltaTime;
        if (_pathTimer <= 0f)
        {
            Vector2Int start = WorldToCell(transform.position);
            Vector2Int targetCell = WorldToCell(target.position);
            
            // Use influence map to find better attack position
            Vector2Int tacticalTarget = FindBestAttackCell(targetCell);
            
            if (_pathfinder.TryFindPath(start, tacticalTarget, _path))
            {
                _pathIndex = Mathf.Min(1, _path.Count - 1);
            }
            _pathTimer = rePathInterval;
        }
        
        TryFire();
    }

    private void ExecuteStrafe()
    {
        if (target == null) return;
        
        // Strafe perpendicular to player
        Vector3 toPlayer = (target.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toPlayer);
        int direction = stateTimer % 4f < 2f ? 1 : -1;
        
        Vector3 strafeTargetPos = transform.position + right * (direction * 3f);
        Vector2Int targetCell = WorldToCell(strafeTargetPos);
        
        _pathTimer -= Time.deltaTime;
        if (_pathTimer <= 0f)
        {
            Vector2Int start = WorldToCell(transform.position);
            if (_pathfinder.TryFindPath(start, targetCell, _path))
            {
                _pathIndex = Mathf.Min(1, _path.Count - 1);
            }
            _pathTimer = rePathInterval * 1.5f;
        }
        
        TryFire();
    }

    private void ExecuteRetreat()
    {
        if (target == null) return;
        
        Vector3 retreatDir = (transform.position - target.position).normalized;
        Vector3 retreatPos = transform.position + retreatDir * 10f;
        Vector2Int targetCell = WorldToCell(retreatPos);
        
        _pathTimer -= Time.deltaTime;
        if (_pathTimer <= 0f)
        {
            Vector2Int start = WorldToCell(transform.position);
            if (_pathfinder.TryFindPath(start, targetCell, _path))
            {
                _pathIndex = Mathf.Min(1, _path.Count - 1);
            }
            _pathTimer = rePathInterval;
        }
    }

    private void ExecuteTakeCover()
    {
        if (_tacticalPosition == Vector3.zero)
            _tacticalPosition = FindBestCoverPosition();
        
        Vector2Int targetCell = WorldToCell(_tacticalPosition);
        Vector2Int currentCell = WorldToCell(transform.position);
        
        if (currentCell == targetCell || Vector3.Distance(transform.position, _tacticalPosition) < 1f)
        {
            _path.Clear();
            _holdingPosition = true;
            
            // Peek and shoot from cover
            if (stateTimer > 1f && Random.value > 0.6f)
            {
                TryFire();
            }
        }
        else
        {
            _pathTimer -= Time.deltaTime;
            if (_pathTimer <= 0f)
            {
                Vector2Int start = WorldToCell(transform.position);
                if (_pathfinder.TryFindPath(start, targetCell, _path))
                {
                    _pathIndex = Mathf.Min(1, _path.Count - 1);
                }
                _pathTimer = rePathInterval;
            }
        }
    }

    private void ExecuteAmbush()
    {
        if (!_holdingPosition)
        {
            _tacticalPosition = FindBestAmbushPosition();
            Vector2Int targetCell = WorldToCell(_tacticalPosition);
            Vector2Int start = WorldToCell(transform.position);
            
            if (_pathfinder.TryFindPath(start, targetCell, _path))
            {
                _pathIndex = Mathf.Min(1, _path.Count - 1);
            }
            
            if (Vector3.Distance(transform.position, _tacticalPosition) < 1f)
            {
                _holdingPosition = true;
                _path.Clear();
            }
        }
        else
        {
            // Wait for player, spring ambush when close
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                if (distance < attackRange && hasLineOfSight)
                {
                    TryFire();
                    if (stateTimer > 1f)
                    {
                        TransitionToState(State.Chase);
                    }
                }
            }
        }
    }

    private void ExecuteFlank()
    {
        if (_tacticalPosition == Vector3.zero)
            _tacticalPosition = FindBestFlankPosition();
        
        Vector2Int targetCell = WorldToCell(_tacticalPosition);
        _pathTimer -= Time.deltaTime;
        if (_pathTimer <= 0f)
        {
            Vector2Int start = WorldToCell(transform.position);
            if (_pathfinder.TryFindPath(start, targetCell, _path))
            {
                _pathIndex = Mathf.Min(1, _path.Count - 1);
            }
            _pathTimer = rePathInterval;
        }
        
        if (Vector3.Distance(transform.position, _tacticalPosition) < 2f)
        {
            TransitionToState(State.Chase);
        }
        
        if (hasLineOfSight && Random.value > 0.7f)
        {
            TryFire();
        }
    }

    private void UpdateMovement()
    {
        if (_path.Count == 0 || _pathIndex >= _path.Count || _holdingPosition)
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

    private void UpdateInfluenceMap()
    {
        if (_influenceMap == null || target == null)
            return;

        // Reset map
        System.Array.Clear(_influenceMap, 0, _influenceMap.Length);

        Vector2Int playerCell = WorldToCell(target.position);

        // Add danger influence around player
        AddInfluence(playerCell, influenceMapRadius, -dangerWeight);

        // Add positive influence for cover positions
        AddCoverInfluence();

        // Add positive influence for attack positions with line of sight
        AddAttackPositionInfluence(playerCell);
    }

    private void AddInfluence(Vector2Int center, float radius, float weight)
    {
        int cellRadius = Mathf.CeilToInt(radius);
        
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                int cellX = center.x + x;
                int cellY = center.y + y;
                
                if (cellX < 0 || cellX >= _influenceMap.GetLength(0) || 
                    cellY < 0 || cellY >= _influenceMap.GetLength(1))
                    continue;

                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= radius)
                {
                    float falloff = 1f - (distance / radius);
                    _influenceMap[cellX, cellY] += weight * falloff;
                }
            }
        }
    }

    private void AddCoverInfluence()
    {
        // Add positive influence near walls (cover)
        // Note: This is a simplified version - ideally would check actual arena map
        for (int x = 1; x < _influenceMap.GetLength(0) - 1; x++)
        {
            for (int y = 1; y < _influenceMap.GetLength(1) - 1; y++)
            {
                // Add cover influence to cells near the edges (likely near walls)
                int distFromEdgeX = Mathf.Min(x, _influenceMap.GetLength(0) - 1 - x);
                int distFromEdgeY = Mathf.Min(y, _influenceMap.GetLength(1) - 1 - y);
                
                if (distFromEdgeX < 3 || distFromEdgeY < 3)
                {
                    _influenceMap[x, y] += coverWeight * 0.5f;
                }
            }
        }
    }

    private void AddAttackPositionInfluence(Vector2Int playerCell)
    {
        // Positions at good attack range get positive influence
        float optimalRange = attackRange * 0.8f;
        
        for (int x = 0; x < _influenceMap.GetLength(0); x++)
        {
            for (int y = 0; y < _influenceMap.GetLength(1); y++)
            {
                float distance = Vector2Int.Distance(new Vector2Int(x, y), playerCell);
                if (Mathf.Abs(distance - optimalRange) < 3f)
                {
                    _influenceMap[x, y] += attackPositionWeight;
                }
            }
        }
    }

    private Vector2Int FindBestAttackCell(Vector2Int targetCell)
    {
        Vector2Int bestCell = targetCell;
        float bestScore = float.MinValue;
        
        // Search around target for best tactical position
        int searchRadius = 5;
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector2Int cell = new Vector2Int(targetCell.x + x, targetCell.y + y);
                
                if (cell.x < 0 || cell.x >= _influenceMap.GetLength(0) ||
                    cell.y < 0 || cell.y >= _influenceMap.GetLength(1))
                    continue;

                float score = _influenceMap[cell.x, cell.y];
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }
        }
        
        return bestCell;
    }

    private Vector3 FindBestCoverPosition()
    {
        Vector2Int myCell = WorldToCell(transform.position);
        Vector2Int bestCell = myCell;
        float bestScore = float.MinValue;
        
        int searchRadius = Mathf.CeilToInt(influenceMapRadius);
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector2Int cell = new Vector2Int(myCell.x + x, myCell.y + y);
                
                if (cell.x < 0 || cell.x >= _influenceMap.GetLength(0) ||
                    cell.y < 0 || cell.y >= _influenceMap.GetLength(1))
                    continue;

                float score = _influenceMap[cell.x, cell.y] + coverWeight;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }
        }
        
        return CellToWorld(bestCell);
    }

    private Vector3 FindBestAmbushPosition()
    {
        if (target == null)
            return transform.position;

        // Find position with high cover near player's path
        Vector2Int playerCell = WorldToCell(target.position);
        Vector2Int myCell = WorldToCell(transform.position);
        
        // Look for positions between self and player
        Vector2Int midPoint = new Vector2Int(
            (myCell.x + playerCell.x) / 2,
            (myCell.y + playerCell.y) / 2
        );
        
        return CellToWorld(FindBestAttackCell(midPoint));
    }

    private Vector3 FindBestFlankPosition()
    {
        if (target == null)
            return transform.position;

        Vector2Int playerCell = WorldToCell(target.position);
        Vector2Int myCell = WorldToCell(transform.position);
        
        // Calculate perpendicular positions
        Vector2Int toPlayer = playerCell - myCell;
        Vector2Int perpendicular = new Vector2Int(-toPlayer.y, toPlayer.x);
        perpendicular.x = perpendicular.x != 0 ? perpendicular.x / Mathf.Abs(perpendicular.x) : 0;
        perpendicular.y = perpendicular.y != 0 ? perpendicular.y / Mathf.Abs(perpendicular.y) : 0;
        
        Vector2Int flankCell = playerCell + perpendicular * 5;
        
        return CellToWorld(flankCell);
    }

    private Vector2Int GetRandomPatrolPoint()
    {
        if (_arenaGenerator == null)
            return WorldToCell(transform.position);

        int x = Random.Range(5, _arenaGenerator.width - 5);
        int y = Random.Range(5, _arenaGenerator.height - 5);
        return new Vector2Int(x, y);
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
