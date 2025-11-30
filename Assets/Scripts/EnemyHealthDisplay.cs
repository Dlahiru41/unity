using UnityEngine;

/// <summary>
/// Displays enemy ID and health bar above each enemy.
/// </summary>
public class EnemyHealthDisplay : MonoBehaviour
{
    public EnemyBase enemy;
    public Vector3 offset = new Vector3(0, 2f, 0);
    public float barWidth = 1f;
    public float barHeight = 0.15f;
    
    private Camera _mainCamera;
    private GUIStyle _labelStyle;
    private bool _initialized;

    private void Start()
    {
        _mainCamera = Camera.main;
        if (enemy == null)
        {
            enemy = GetComponent<EnemyBase>();
        }
        
        _labelStyle = new GUIStyle();
        _labelStyle.alignment = TextAnchor.MiddleCenter;
        _labelStyle.normal.textColor = Color.white;
        _labelStyle.fontSize = 12;
        _labelStyle.fontStyle = FontStyle.Bold;
        
        _initialized = true;
    }

    private void OnGUI()
    {
        if (!_initialized || enemy == null || _mainCamera == null)
            return;

        // Calculate screen position above enemy
        Vector3 worldPos = transform.position + offset;
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

        // Only draw if in front of camera
        if (screenPos.z > 0)
        {
            // Flip Y coordinate for GUI
            screenPos.y = Screen.height - screenPos.y;

            // Draw ID label
            Rect labelRect = new Rect(screenPos.x - 30, screenPos.y - 30, 60, 20);
            GUI.Label(labelRect, enemy.enemyID, _labelStyle);

            // Draw health bar background (black)
            float barWidthPixels = barWidth * 50f;
            Rect bgRect = new Rect(screenPos.x - barWidthPixels / 2, screenPos.y - 10, barWidthPixels, barHeight * 50f);
            GUI.color = Color.black;
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Draw health bar foreground (red to green gradient based on health)
            float healthPercent = Mathf.Clamp01(enemy.currentHP / enemy.maxHealth);
            Rect fgRect = new Rect(bgRect.x + 2, bgRect.y + 2, (bgRect.width - 4) * healthPercent, bgRect.height - 4);
            
            Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
            GUI.color = healthColor;
            GUI.DrawTexture(fgRect, Texture2D.whiteTexture);

            // Reset color
            GUI.color = Color.white;
        }
    }
}

