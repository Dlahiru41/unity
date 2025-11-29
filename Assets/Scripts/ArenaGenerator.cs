using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Generates a tile-based combat arena (minimum 50x50) with boundary walls,
// internal rooms, corridors, hiding areas and open regions. Deterministic by default.
[ExecuteInEditMode]
public class ArenaGenerator : MonoBehaviour
{
    [Header("Size")]
    public int width = 60; // >= 50
    public int height = 60; // >= 50
    public int tileSize = 1;

    [Header("Random / Seed")]
    public bool useRandomSeed;
    public int seed = 12345;
    [Range(0, 100)]
    public int randomFillPercent = 45;

    [Header("Cellular Automata")]
    public int smoothIterations = 4;

    [Header("Rooms")]
    public int roomAttempts = 8;
    public int minRoomSize = 6;
    public int maxRoomSize = 14;

    [Header("Runtime")]
    public bool generateOnStart = true;

    [Header("Player")]
    public GameObject playerPrefab; // optional: assign a player prefab (named "Player")
    public bool autoSpawnPlayer = true; // if true, will attempt to spawn/move player after Generate()

    [Header("Camera")]
    public bool setupMainCamera = true;
    public Vector3 cameraOffset = new Vector3(0f, 30f, -30f);
    public float cameraSmoothTime = 0.12f;

    [Header("Events")]
    public UnityEvent OnArenaGenerated;

    // internal map: 1 = wall, 0 = floor
    private int[,] _map;
    private float[,] _costMap;

    // parent objects to keep the hierarchy clean
    private Transform _arenaParent;
    private Transform _floorParent;
    private Transform _wallParent;

    [ContextMenu("Generate Arena")]
    public void Generate()
    {
        // enforce minimums
        width = Mathf.Max(50, width);
        height = Mathf.Max(50, height);

        // prepare map
        _map = new int[width, height];

        // deterministic PRNG
        System.Random prng = useRandomSeed ? new System.Random() : new System.Random(seed);

        InitializeMap(prng);

        for (int i = 0; i < smoothIterations; i++)
            SmoothMap();

        // add deterministic rooms and corridors to ensure partitioning
        List<RectInt> rooms = PlaceRooms(prng);
        ConnectRooms(rooms);

        // keep the largest open region (removes small isolated caverns)
        KeepLargestFloorRegion();

        // build GameObjects in scene
        BuildGameObjects();

        // optionally spawn player at center of arena
        if (autoSpawnPlayer)
            SpawnPlayerAtCenter();

        // optionally set up the main camera to follow the player/arena
        if (setupMainCamera)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                // try find any camera in the scene
                cam = FindObjectOfType<Camera>();
                if (cam != null)
                {
                    // ensure it can be accessed via Camera.main for later code
                    cam.tag = "MainCamera";
                }
            }

            if (cam != null)
            {
                CameraFollower cf = cam.GetComponent<CameraFollower>();
                if (cf == null)
                    cf = cam.gameObject.AddComponent<CameraFollower>();

                cf.offset = cameraOffset;
                cf.smoothTime = cameraSmoothTime;

                // prefer assigning the Player as target if present
                GameObject playerObj = GameObject.Find("Player");
                if (playerObj != null)
                {
                    cf.target = playerObj.transform;
                }
                else if (_arenaParent != null)
                {
                    // fallback: point camera at arena center
                    cf.target = _arenaParent;
                }

                // ensure camera is enabled
                cam.enabled = true;

                // set an immediate sensible camera position and rotation so the Game view shows the arena immediately
                // compute arena center and size to frame the scene
                Vector3 arenaCenter = new Vector3((width - 1) * 0.5f * tileSize, 0f, (height - 1) * 0.5f * tileSize);
                float frameSize = Mathf.Max(width, height) * tileSize;

                if (cf.target != null)
                {
                    // prefer focusing on player but compute an offset that shows most of the arena
                    Vector3 desired = cf.target.position + cf.offset;
                    // if the desired point is too close to the target, boost it based on arena size
                    if (Vector3.Distance(desired, cf.target.position) < frameSize * 0.25f)
                        desired = arenaCenter + new Vector3(0f, frameSize * 0.6f, -frameSize * 0.6f);
                    cam.transform.position = desired;
                    cam.transform.LookAt(cf.target.position + Vector3.up * 1f);
                }
                else
                {
                    // no player target — position camera to frame the arena center
                    cam.transform.position = arenaCenter + new Vector3(0f, frameSize * 0.6f, -frameSize * 0.6f);
                    cam.transform.LookAt(arenaCenter + Vector3.up * 1f);
                }

                // camera safety: ensure clear flags and clipping are sensible
                cam.clearFlags = CameraClearFlags.Skybox;
                if (cam.nearClipPlane > 0.1f) cam.nearClipPlane = 0.01f;

                // If the camera-to-target ray is immediately occluded (e.g., camera inside/behind a wall), nudge it
                if (cf.target != null)
                {
                    Vector3 dir = (cf.target.position + Vector3.up * 1f) - cam.transform.position;
                    float dist = dir.magnitude;
                    if (dist > 0.01f)
                    {
                        if (Physics.Raycast(cam.transform.position, dir.normalized, dist))
                        {
                            // if we hit something before reaching the target, it's likely occlusion
                            // try moving the camera up and back a bit and re-orient
                            float nudge = Mathf.Max(1f, frameSize * 0.25f);
                            cam.transform.position += new Vector3(0f, nudge, -nudge);
                            cam.transform.LookAt(cf.target.position + Vector3.up * 1f);
                        }
                    }
                }
            }
        }

        OnArenaGenerated?.Invoke();
    }

    private void InitializeMap(System.Random prng)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // make borders walls
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    _map[x, y] = 1;
                }
                else
                {
                    int roll = prng.Next(0, 100);
                    _map[x, y] = (roll < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    private void SmoothMap()
    {
        int[,] newMap = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWalls = GetSurroundingWallCount(x, y);
                if (neighbourWalls > 4)
                    newMap[x, y] = 1;
                else if (neighbourWalls < 4)
                    newMap[x, y] = 0;
                else
                    newMap[x, y] = _map[x, y];

                // ensure boundary stays wall
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    newMap[x, y] = 1;
            }
        }
        _map = newMap;
    }

    private int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (neighbourX == gridX && neighbourY == gridY)
                    continue;

                if (IsInMapRange(neighbourX, neighbourY))
                {
                    wallCount += _map[neighbourX, neighbourY];
                }
                else
                {
                    // outside map counts as a wall
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    private bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Place a few deterministic rectangular rooms across the arena
    private List<RectInt> PlaceRooms(System.Random prng)
    {
        List<RectInt> rooms = new List<RectInt>();

        // divide arena roughly into a grid and place one room per cell to ensure partitioning
        int cols = Mathf.Clamp(roomAttempts / 2, 2, roomAttempts);
        int rows = Mathf.Clamp(roomAttempts / cols, 2, roomAttempts);
        int cellW = width / cols;
        int cellH = height / rows;

        for (int cx = 0; cx < cols; cx++)
        {
            for (int cy = 0; cy < rows; cy++)
            {
                // room size
                int rw = prng.Next(minRoomSize, maxRoomSize + 1);
                int rh = prng.Next(minRoomSize, maxRoomSize + 1);

                // position with small deterministic offset inside the cell
                int minX = cx * cellW + 1;
                int minY = cy * cellH + 1;
                int maxX = Mathf.Min(minX + cellW - rw - 2, width - rw - 2);
                int maxY = Mathf.Min(minY + cellH - rh - 2, height - rh - 2);

                if (maxX <= minX || maxY <= minY)
                    continue;

                int rx = prng.Next(minX, maxX + 1);
                int ry = prng.Next(minY, maxY + 1);

                RectInt room = new RectInt(rx, ry, rw, rh);
                rooms.Add(room);

                CarveRoom(room);
            }
        }

        return rooms;
    }

    private void CarveRoom(RectInt r)
    {
        for (int x = r.xMin; x < r.xMax; x++)
        {
            for (int y = r.yMin; y < r.yMax; y++)
            {
                if (IsInMapRange(x, y))
                    _map[x, y] = 0; // floor
            }
        }
    }

    private void ConnectRooms(List<RectInt> rooms)
    {
        if (rooms == null || rooms.Count == 0)
            return;

        // compute centers
        List<Vector2Int> centers = new List<Vector2Int>();
        foreach (var r in rooms)
            centers.Add(new Vector2Int(r.x + r.width / 2, r.y + r.height / 2));

        // naive connect: sort by x then connect sequentially to ensure corridors
        centers.Sort((a, b) => a.x.CompareTo(b.x));

        for (int i = 0; i < centers.Count - 1; i++)
        {
            Vector2Int a = centers[i];
            Vector2Int b = centers[i + 1];
            CarveCorridor(a, b);
        }
    }

    private void CarveCorridor(Vector2Int a, Vector2Int b)
    {
        Vector2Int current = a;
        // horizontal first
        while (current.x != b.x)
        {
            if (IsInMapRange(current.x, current.y))
                _map[current.x, current.y] = 0;
            current.x += (b.x > current.x) ? 1 : -1;
        }
        // then vertical
        while (current.y != b.y)
        {
            if (IsInMapRange(current.x, current.y))
                _map[current.x, current.y] = 0;
            current.y += (b.y > current.y) ? 1 : -1;
        }
    }

    // Keep the largest connected floor region and fill the rest with walls
    private void KeepLargestFloorRegion()
    {
        int[,] regions = new int[width, height];
        int regionIndex = 0;
        Dictionary<int, int> regionSizes = new Dictionary<int, int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_map[x, y] == 0 && regions[x, y] == 0)
                {
                    regionIndex++;
                    int size = FloodFillRegion(x, y, regionIndex, regions);
                    regionSizes[regionIndex] = size;
                }
            }
        }

        if (regionIndex == 0)
            return;

        // find largest
        int largestIndex = -1;
        int largestSize = -1;
        foreach (var kv in regionSizes)
        {
            if (kv.Value > largestSize)
            {
                largestSize = kv.Value;
                largestIndex = kv.Key;
            }
        }

        // any floor tile not in largest region becomes wall
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_map[x, y] == 0 && regions[x, y] != largestIndex)
                    _map[x, y] = 1;
            }
        }
    }

    private int FloodFillRegion(int startX, int startY, int index, int[,] regions)
    {
        int size = 0;
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(startX, startY));
        regions[startX, startY] = index;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            size++;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (Mathf.Abs(dx) == Mathf.Abs(dy)) // skip diagonals for region connectivity to ensure corridors connect properly
                        continue;

                    int nx = p.x + dx;
                    int ny = p.y + dy;
                    if (!IsInMapRange(nx, ny))
                        continue;
                    if (regions[nx, ny] != 0)
                        continue;
                    if (_map[nx, ny] == 1)
                        continue;

                    regions[nx, ny] = index;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        return size;
    }

    private void BuildGameObjects()
    {
        // destroy previous parents if present
        ClearPreviousParents();

        _arenaParent = new GameObject("Arena").transform;
        _arenaParent.SetParent(transform, false);

        _floorParent = new GameObject("Floor").transform;
        _floorParent.SetParent(_arenaParent, false);

        _wallParent = new GameObject("Walls").transform;
        _wallParent.SetParent(_arenaParent, false);

        _costMap = new float[width, height];

        // create a single big floor under everything for grounding
        GameObject floorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorPlane.name = "ArenaBaseFloor";
        floorPlane.transform.SetParent(_floorParent, false);
        float fx = width * tileSize * 0.5f - tileSize * 0.5f;
        float fz = height * tileSize * 0.5f - tileSize * 0.5f;
        floorPlane.transform.position = new Vector3(fx, -0.51f, fz);
        // scale: Plane is 10x10 units
        float scaleX = width * tileSize / 10f;
        float scaleZ = height * tileSize / 10f;
        floorPlane.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

        // optionally create individual floor tiles for visual variation (disabled for performance by default)
        // create walls at map positions
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_map[x, y] == 1)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Wall_{x}_{y}";
                    cube.transform.SetParent(_wallParent, false);
                    cube.transform.position = new Vector3(x * tileSize, 0f, y * tileSize);
                    cube.transform.localScale = new Vector3(tileSize, 2f, tileSize);
                    var rend = cube.GetComponent<Renderer>();
                    if (rend != null)
                        rend.sharedMaterial = GetDebugMaterial(Color.grey);

                    _costMap[x, y] = float.PositiveInfinity;
                }
                else
                {
                    // assign cost multipliers based on adjacency: more open areas cheaper than narrow corridors
                    int nearbyWalls = GetSurroundingWallCount(x, y);
                    _costMap[x, y] = Mathf.Lerp(1f, 3f, nearbyWalls / 8f);
                }
            }
        }

        // place a few visual markers for hiding spots (small cubes) inside larger open areas
        MarkHidingSpots();
    }

    private void MarkHidingSpots()
    {
        // find open tiles with walls nearby and place small cover cubes
        int spots = 0;
        for (int x = 1; x < width - 1 && spots < 20; x++)
        {
            for (int y = 1; y < height - 1 && spots < 20; y++)
            {
                if (_map[x, y] == 0)
                {
                    int nearbyWalls = 0;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if (!(dx == 0 && dy == 0) && IsInMapRange(x + dx, y + dy) && _map[x + dx, y + dy] == 1)
                                nearbyWalls++;

                    if (nearbyWalls >= 3)
                    {
                        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cover.name = $"HidingSpot_{x}_{y}";
                        cover.transform.SetParent(_arenaParent, false);
                        cover.transform.position = new Vector3(x * tileSize, 0f, y * tileSize);
                        cover.transform.localScale = new Vector3(tileSize * 0.6f, 1f, tileSize * 0.6f);
                        var rend = cover.GetComponent<Renderer>();
                        if (rend != null)
                            rend.sharedMaterial = GetDebugMaterial(Color.red);
                        spots++;
                    }
                }
            }
        }
    }

    private Material _debugMat;
    private Material GetDebugMaterial(Color c)
    {
        if (_debugMat == null)
        {
            _debugMat = new Material(Shader.Find("Standard"));
            _debugMat.enableInstancing = true;
        }
        _debugMat.color = c;
        return _debugMat;
    }

    private void ClearPreviousParents()
    {
        // if parent exists as child of this transform, destroy it
        Transform prev = transform.Find("Arena");
        if (prev != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorApplication.delayCall += () => UnityEngine.Object.DestroyImmediate(prev.gameObject);
            else
                Destroy(prev.gameObject);
#else
            Destroy(prev.gameObject);
#endif
        }
    }

    private void OnValidate()
    {
        // keep sensible ranges in editor
        width = Mathf.Max(50, width);
        height = Mathf.Max(50, height);
        minRoomSize = Mathf.Clamp(minRoomSize, 3, 100);
        maxRoomSize = Mathf.Clamp(maxRoomSize, minRoomSize, 100);
    }

    private void OnDrawGizmosSelected()
    {
        // visualize grid bounds in the editor
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((width - 1) * 0.5f * tileSize, 0f, (height - 1) * 0.5f * tileSize);
        Vector3 size = new Vector3(width * tileSize, 0f, height * tileSize);
        Gizmos.DrawWireCube(center, size);
    }

    [ContextMenu("Spawn Player At Center")]
    public void SpawnPlayerAtCenter()
    {
        if (_map == null)
        {
            Debug.LogWarning("Map not generated yet. Run Generate() first.");
            return;
        }

        // find nearest floor tile starting from center
        int cx = width / 2;
        int cy = height / 2;
        int maxRadius = Mathf.Max(width, height);
        int foundX = -1, foundY = -1;

        for (int r = 0; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) // only check outer ring
                        continue;

                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (!IsInMapRange(nx, ny))
                        continue;
                    if (_map[nx, ny] == 0)
                    {
                        foundX = nx;
                        foundY = ny;
                        break;
                    }
                }
                if (foundX != -1) break;
            }
            if (foundX != -1) break;
        }

        if (foundX == -1)
        {
            Debug.LogWarning("No floor tile found to place player.");
            return;
        }

        Vector3 spawnPos = new Vector3(foundX * tileSize, 0.5f, foundY * tileSize);

        GameObject existing = GameObject.Find("Player");
        if (existing != null)
        {
            existing.transform.position = spawnPos;
            Debug.Log("Moved existing Player to arena center.");
            return;
        }

        if (playerPrefab != null)
        {
            GameObject p = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            p.name = "Player";
            Debug.Log("Instantiated player prefab into arena.");
            return;
        }

        // fallback: create a simple cube player and attach PlayerController if available
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Player";
        cube.transform.position = spawnPos;
        cube.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);
        var rend = cube.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.cyan;
            rend.sharedMaterial = mat;
        }
        if (cube.GetComponent<Collider>() == null) cube.AddComponent<BoxCollider>();
        var rb = cube.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // add front marker
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "FrontMarker";
        marker.transform.SetParent(cube.transform, false);
        marker.transform.localScale = new Vector3(0.6f, 0.9f, 0.05f);
        marker.transform.localPosition = new Vector3(0f, 0f, 0.51f);
        marker.transform.localRotation = Quaternion.identity;
        var mr = marker.GetComponent<Renderer>();
        if (mr != null)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = Color.red;
            mr.sharedMaterial = m;
        }
        // keep the BoxCollider on the marker — safe with non-kinematic Rigidbody

        // attach PlayerController if available
        var pc = cube.AddComponent<PlayerController>();
        pc.moveSpeed = 6f;
        pc.maxHealth = 100;
        pc.currentHealth = pc.maxHealth;
        pc.shootCooldown = 0.25f;
        pc.bulletSpeed = 20f;
        pc.bulletLifetime = 4f;

        Debug.Log("Created fallback Player in arena.");
    }

    // allow generation at runtime
    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    public int[,] GetMapCopy()
    {
        if (_map == null) return null;
        var copy = new int[width, height];
        System.Array.Copy(_map, copy, _map.Length);
        return copy;
    }

    public float[,] GetCostMap()
    {
        if (_costMap == null) return null;
        var copy = new float[width, height];
        System.Array.Copy(_costMap, copy, _costMap.Length);
        return copy;
    }

    public Vector3 GetRandomFloorPosition()
    {
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int x = Random.Range(1, width - 1);
            int y = Random.Range(1, height - 1);
            if (_map != null && _map[x, y] == 0)
            {
                return new Vector3(x * tileSize, 0.5f, y * tileSize);
            }
        }
        return Vector3.zero;
    }
}
