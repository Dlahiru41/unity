using UnityEngine;

/// <summary>
/// Displays all enemy stats in a HUD legend at the corner of the screen.
/// </summary>
public class EnemyHUDLegend : MonoBehaviour
{
    public enum CornerPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public CornerPosition position = CornerPosition.TopRight;
    public bool showLegend = true;
    
    private GUIStyle _boxStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _textStyle;
    private bool _initialized;

    private void Start()
    {
        InitializeStyles();
    }

    private void InitializeStyles()
    {
        // Box style
        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));
        _boxStyle.padding = new RectOffset(10, 10, 5, 5);

        // Header style
        _headerStyle = new GUIStyle();
        _headerStyle.fontSize = 14;
        _headerStyle.fontStyle = FontStyle.Bold;
        _headerStyle.normal.textColor = Color.yellow;
        _headerStyle.alignment = TextAnchor.MiddleLeft;

        // Text style
        _textStyle = new GUIStyle();
        _textStyle.fontSize = 12;
        _textStyle.normal.textColor = Color.white;
        _textStyle.alignment = TextAnchor.MiddleLeft;

        _initialized = true;
    }

    private void OnGUI()
    {
        if (!_initialized)
            InitializeStyles();

        if (!showLegend)
            return;

        // Find all enemies
        var enemies = FindObjectsOfType<EnemyBase>();
        if (enemies.Length == 0)
            return;

        // Calculate box size
        float width = 200f;
        float lineHeight = 20f;
        float height = 30f + (enemies.Length * lineHeight * 2) + 10f;

        // Calculate position based on corner
        Rect boxRect = GetBoxRect(width, height);

        // Draw background box
        GUI.Box(boxRect, "", _boxStyle);

        // Draw header
        Rect headerRect = new Rect(boxRect.x + 10, boxRect.y + 5, boxRect.width - 20, 20);
        GUI.Label(headerRect, "Enemy Status", _headerStyle);

        // Draw each enemy's stats
        float yOffset = 30f;
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            // Enemy ID
            Rect idRect = new Rect(boxRect.x + 15, boxRect.y + yOffset, boxRect.width - 30, lineHeight);
            
            // Color code by type
            Color typeColor = Color.white;
            if (enemy is NavMeshEnemyController)
                typeColor = Color.magenta;
            else if (enemy is GridEnemyController)
                typeColor = Color.green;
            
            _textStyle.normal.textColor = typeColor;
            GUI.Label(idRect, $"{enemy.enemyID}", _textStyle);

            yOffset += lineHeight;

            // Health bar
            float healthPercent = Mathf.Clamp01(enemy.currentHP / enemy.maxHealth);
            Rect healthBgRect = new Rect(boxRect.x + 15, boxRect.y + yOffset, boxRect.width - 30, 15);
            
            // Background
            GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            GUI.DrawTexture(healthBgRect, Texture2D.whiteTexture);

            // Foreground (health)
            Rect healthFgRect = new Rect(healthBgRect.x, healthBgRect.y, healthBgRect.width * healthPercent, healthBgRect.height);
            Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
            GUI.color = healthColor;
            GUI.DrawTexture(healthFgRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // HP text
            _textStyle.normal.textColor = Color.white;
            _textStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(healthBgRect, $"{Mathf.CeilToInt(enemy.currentHP)}/{enemy.maxHealth} HP", _textStyle);
            _textStyle.alignment = TextAnchor.MiddleLeft;

            yOffset += lineHeight + 5f;
        }
    }

    private Rect GetBoxRect(float width, float height)
    {
        float margin = 10f;
        switch (position)
        {
            case CornerPosition.TopLeft:
                return new Rect(margin, margin, width, height);
            case CornerPosition.TopRight:
                return new Rect(Screen.width - width - margin, margin, width, height);
            case CornerPosition.BottomLeft:
                return new Rect(margin, Screen.height - height - margin, width, height);
            case CornerPosition.BottomRight:
                return new Rect(Screen.width - width - margin, Screen.height - height - margin, width, height);
            default:
                return new Rect(Screen.width - width - margin, margin, width, height);
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}

