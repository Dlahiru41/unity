using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script to auto-configure the scene for the combat arena with enemies.
/// </summary>
public class SceneSetupHelper : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/Combat Arena/Setup Complete Scene", false, 10)]
    private static void SetupCompleteScene()
    {
        // Find or create ArenaGenerator
        ArenaGenerator arenaGen = FindObjectOfType<ArenaGenerator>();
        if (arenaGen == null)
        {
            GameObject arenaObj = new GameObject("ArenaGenerator");
            arenaGen = arenaObj.AddComponent<ArenaGenerator>();
            arenaGen.generateOnStart = true;
            arenaGen.autoSpawnPlayer = true;
            arenaGen.setupMainCamera = true;
            Debug.Log("Created ArenaGenerator");
        }

        // Find or create EnemySpawner
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("EnemySpawner");
            spawner = spawnerObj.AddComponent<EnemySpawner>();
            spawner.spawnOnStart = true;
            spawner.respawnOnArenaGenerated = true;
            spawner.navMeshEnemyCount = 2;
            spawner.gridEnemyCount = 2;
            Debug.Log("Created EnemySpawner (will create runtime enemies automatically)");
        }

        // Ensure Main Camera exists
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
            mainCam.transform.position = new Vector3(30, 30, -30);
            mainCam.transform.LookAt(Vector3.zero);
            Debug.Log("Created Main Camera");
        }

        // Ensure Directional Light exists
        Light dirLight = FindObjectOfType<Light>();
        if (dirLight == null || dirLight.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            Debug.Log("Created Directional Light");
        }

        // Add status display helper
        if (FindObjectOfType<SetupStatusDisplay>() == null)
        {
            GameObject statusObj = new GameObject("StatusDisplay");
            statusObj.AddComponent<SetupStatusDisplay>();
            Debug.Log("Added Runtime Status Display (press H in Play mode)");
        }

        // Add enemy HUD legend
        if (FindObjectOfType<EnemyHUDLegend>() == null)
        {
            GameObject hudObj = new GameObject("EnemyHUD");
            var hud = hudObj.AddComponent<EnemyHUDLegend>();
            hud.position = EnemyHUDLegend.CornerPosition.TopRight;
            Debug.Log("Added Enemy HUD Legend (top-right corner)");
        }

        EditorUtility.DisplayDialog("Scene Setup Complete",
            "Combat Arena scene configured successfully!\n\n" +
            "- ArenaGenerator: ✓\n" +
            "- EnemySpawner: ✓ (runtime enemies enabled)\n" +
            "- Main Camera: ✓\n" +
            "- Lighting: ✓\n\n" +
            "Press Play to see enemies spawn automatically!",
            "OK");

        Selection.activeGameObject = spawner.gameObject;
    }

    [MenuItem("GameObject/Combat Arena/Add Enemy Spawner", false, 11)]
    private static void AddEnemySpawner()
    {
        GameObject spawnerObj = new GameObject("EnemySpawner");
        EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();
        spawner.spawnOnStart = true;
        spawner.respawnOnArenaGenerated = true;
        spawner.navMeshEnemyCount = 2;
        spawner.gridEnemyCount = 2;
        
        Selection.activeGameObject = spawnerObj;
        Undo.RegisterCreatedObjectUndo(spawnerObj, "Create Enemy Spawner");
        
        Debug.Log("Enemy Spawner created! It will auto-generate runtime enemies when arena is ready.");
    }

    [ContextMenu("Setup NavMesh for Arena")]
    public void SetupNavMesh()
    {
#if UNITY_EDITOR
        Debug.Log("NavMesh Setup Instructions:\n" +
                  "1. Press Play to generate the arena, then Stop\n" +
                  "2. Select the Arena/Floor/ArenaBaseFloor GameObject\n" +
                  "3. Window → AI → Navigation\n" +
                  "4. In Navigation window, go to 'Bake' tab\n" +
                  "5. Click 'Bake' button\n" +
                  "Note: NavMesh is optional - Grid enemies work without it!");
        
        EditorUtility.DisplayDialog("NavMesh Setup",
            "NavMesh baking instructions logged to Console.\n\n" +
            "Note: NavMesh is optional for this project.\n" +
            "Grid enemies (green) work perfectly without NavMesh!\n" +
            "NavMesh enemies (magenta) will still move, just less optimally.",
            "OK");
#endif
    }
#endif
}
