using UnityEngine;

/// <summary>
/// Displays helpful runtime information about the combat arena setup.
/// Attach to any GameObject to see status messages.
/// </summary>
public class SetupStatusDisplay : MonoBehaviour
{
    private GUIStyle _textStyle;
    private bool _showInstructions = true;
    private float _hideTimer = 10f;

    private void Awake()
    {
        _textStyle = new GUIStyle();
        _textStyle.fontSize = 14;
        _textStyle.normal.textColor = Color.white;
        _textStyle.wordWrap = true;
    }

    private void Update()
    {
        if (_showInstructions)
        {
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f || Input.GetKeyDown(KeyCode.H))
            {
                _showInstructions = false;
            }
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            _showInstructions = true;
            _hideTimer = 10f;
        }
    }

    private void OnGUI()
    {
        if (!_showInstructions) return;

        // Semi-transparent background
        GUI.Box(new Rect(10, 10, 400, 280), "");
        
        _textStyle.fontStyle = FontStyle.Bold;
        _textStyle.fontSize = 16;
        GUI.Label(new Rect(20, 15, 380, 25), "Combat Arena - Quick Reference", _textStyle);

        _textStyle.fontStyle = FontStyle.Normal;
        _textStyle.fontSize = 13;

        int y = 45;
        GUI.Label(new Rect(20, y, 380, 20), "Controls:", _textStyle);
        y += 22;
        _textStyle.fontSize = 12;
        GUI.Label(new Rect(30, y, 380, 20), "• Move: WASD or Arrow Keys", _textStyle);
        y += 18;
        GUI.Label(new Rect(30, y, 380, 20), "• Shoot: Space or Left Mouse", _textStyle);
        y += 18;
        GUI.Label(new Rect(30, y, 380, 20), "• Toggle Help: H", _textStyle);
        
        y += 30;
        _textStyle.fontSize = 13;
        GUI.Label(new Rect(20, y, 380, 20), "Enemy Types:", _textStyle);
        y += 22;
        _textStyle.fontSize = 12;
        GUI.Label(new Rect(30, y, 380, 20), "• Magenta: NavMesh AI (direct chaser)", _textStyle);
        y += 18;
        GUI.Label(new Rect(30, y, 380, 20), "• Green: Grid AI (strategic pathfinder)", _textStyle);
        y += 18;
        GUI.Label(new Rect(30, y, 380, 20), "• ID & HP shown above each enemy", _textStyle);
        
        y += 30;
        _textStyle.fontSize = 13;
        GUI.Label(new Rect(20, y, 380, 20), "Status:", _textStyle);
        y += 22;
        _textStyle.fontSize = 12;

        // Check scene status
        var arenaGen = FindObjectOfType<ArenaGenerator>();
        var spawner = FindObjectOfType<EnemySpawner>();
        var player = GameObject.Find("Player");

        _textStyle.normal.textColor = arenaGen != null ? Color.green : Color.yellow;
        GUI.Label(new Rect(30, y, 380, 20), $"Arena: {(arenaGen != null ? "✓ Generated" : "⚠ Missing")}", _textStyle);
        y += 18;
        
        _textStyle.normal.textColor = player != null ? Color.green : Color.yellow;
        GUI.Label(new Rect(30, y, 380, 20), $"Player: {(player != null ? "✓ Active" : "⚠ Missing")}", _textStyle);
        y += 18;
        
        _textStyle.normal.textColor = spawner != null ? Color.green : Color.yellow;
        GUI.Label(new Rect(30, y, 380, 20), $"Spawner: {(spawner != null ? "✓ Active" : "⚠ Missing")}", _textStyle);
        y += 18;
        
        int enemyCount = CountActiveEnemies();
        _textStyle.normal.textColor = enemyCount > 0 ? Color.green : Color.yellow;
        GUI.Label(new Rect(30, y, 380, 20), $"Enemies: {enemyCount} active", _textStyle);

        _textStyle.normal.textColor = Color.white;
        _textStyle.fontSize = 11;
        _textStyle.fontStyle = FontStyle.Italic;
        GUI.Label(new Rect(20, 265, 380, 20), $"Press H to toggle this help (auto-hide in {Mathf.Ceil(_hideTimer)}s)", _textStyle);
    }

    private int CountActiveEnemies()
    {
        var navEnemies = FindObjectsOfType<NavMeshEnemyController>();
        var gridEnemies = FindObjectsOfType<GridEnemyController>();
        return navEnemies.Length + gridEnemies.Length;
    }
}

