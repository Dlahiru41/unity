using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public GameObject navMeshEnemyPrefab;
    public GameObject gridEnemyPrefab;
    public int navMeshEnemyCount = 2;
    public int gridEnemyCount = 2;
    public bool spawnOnStart = true;
    public bool respawnOnArenaGenerated = true;

    private ArenaGenerator _arenaGenerator;
    private GridPathfinder _pathfinder;
    private readonly System.Collections.Generic.List<GameObject> _spawnedEnemies = new System.Collections.Generic.List<GameObject>();
    private bool _pendingInitialSpawn;

    private void Awake()
    {
        _arenaGenerator = FindObjectOfType<ArenaGenerator>();
        if (_arenaGenerator == null)
        {
            Debug.LogWarning("EnemySpawner: No ArenaGenerator found in scene.");
            enabled = false;
            return;
        }

        if (respawnOnArenaGenerated)
        {
            if (_arenaGenerator.OnArenaGenerated == null)
                _arenaGenerator.OnArenaGenerated = new UnityEngine.Events.UnityEvent();
            _arenaGenerator.OnArenaGenerated.AddListener(HandleArenaGenerated);
        }

        _pendingInitialSpawn = spawnOnStart;
    }

    private void OnDestroy()
    {
        if (_arenaGenerator != null && respawnOnArenaGenerated && _arenaGenerator.OnArenaGenerated != null)
        {
            _arenaGenerator.OnArenaGenerated.RemoveListener(HandleArenaGenerated);
        }
        ClearSpawned();
    }

    private void Start()
    {
        if (_pendingInitialSpawn)
        {
            HandleArenaGenerated();
            _pendingInitialSpawn = false;
        }
    }

    private void HandleArenaGenerated()
    {
        if (_arenaGenerator == null)
            return;

        if (!BuildPathfinder())
            return;

        ClearSpawned();
        SpawnEnemies();
    }

    private bool BuildPathfinder()
    {
        if (_arenaGenerator == null)
            return false;

        int[,] map = _arenaGenerator.GetMapCopy();
        float[,] costs = _arenaGenerator.GetCostMap();
        if (map != null)
        {
            _pathfinder = new GridPathfinder(map, costs);
            return true;
        }
        return false;
    }

    private void SpawnEnemies()
    {
        if (_arenaGenerator == null)
            return;

        for (int i = 0; i < navMeshEnemyCount; i++)
        {
            SpawnEnemy(navMeshEnemyPrefab, true);
        }

        for (int i = 0; i < gridEnemyCount; i++)
        {
            SpawnEnemy(gridEnemyPrefab, false);
        }
    }

    private void SpawnEnemy(GameObject prefab, bool isNavMesh)
    {
        Vector3 pos = _arenaGenerator.GetRandomFloorPosition();
        GameObject enemy;
        
        if (prefab != null)
        {
            enemy = Instantiate(prefab, pos, Quaternion.identity, transform);
        }
        else
        {
            enemy = CreateRuntimeEnemy(isNavMesh, pos);
        }

        if (!isNavMesh)
        {
            var gridController = enemy.GetComponent<GridEnemyController>();
            if (gridController != null && _pathfinder != null)
            {
                gridController.Init(_pathfinder);
            }
        }

        _spawnedEnemies.Add(enemy);
    }


    private GameObject CreateRuntimeEnemy(bool navMesh, Vector3 position)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = navMesh ? "RuntimeNavMeshEnemy" : "RuntimeGridEnemy";
        root.transform.SetParent(transform, false);
        root.transform.position = position;

        // Set up collider for proper collision (not trigger)
        var col = root.GetComponent<Collider>();
        col.material = null;
        col.isTrigger = false; // Ensure physical collisions

        if (navMesh)
        {
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.7f;
            agent.acceleration = 20f;
            agent.angularSpeed = 540f;
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            var controller = root.AddComponent<NavMeshEnemyController>();
            controller.moveSpeed = 3.5f;
        }
        else
        {
            var controller = root.AddComponent<GridEnemyController>();
            controller.moveSpeed = 2.5f;
        }

        var baseStats = root.GetComponent<EnemyBase>();
        baseStats.maxHealth = 80;
        baseStats.attackRange = 10f;
        baseStats.fireCooldown = 1.25f;

        // Add health display above enemy
        var healthDisplay = root.AddComponent<EnemyHealthDisplay>();
        healthDisplay.enemy = baseStats;
        healthDisplay.offset = new Vector3(0, 1.2f, 0);

        var mat = new Material(Shader.Find("Standard"));
        mat.color = navMesh ? Color.magenta : Color.green;
        var rend = root.GetComponent<Renderer>();
        if (rend != null)
            rend.sharedMaterial = mat;

        return root;
    }

    private void ClearSpawned()
    {
        for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (_spawnedEnemies[i] != null)
            {
                Destroy(_spawnedEnemies[i]);
            }
        }
        _spawnedEnemies.Clear();
    }
}
