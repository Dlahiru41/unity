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
    public bool showState = true; // Show FSM state
    
    private Camera _mainCamera;
    private GUIStyle _labelStyle;
    private GUIStyle _stateStyle;
    private bool _initialized;

    private void OnGUI()
    {
        if (!_initialized)
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
            
            _stateStyle = new GUIStyle();
            _stateStyle.alignment = TextAnchor.MiddleCenter;
            _stateStyle.normal.textColor = Color.yellow;
            _stateStyle.fontSize = 10;
            _stateStyle.fontStyle = FontStyle.Normal;
            
            _initialized = true;
        }

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
            Rect labelRect = new Rect(screenPos.x - 30, screenPos.y - 45, 60, 20);
            GUI.Label(labelRect, enemy.enemyID, _labelStyle);

            // Draw FSM state if available
            if (showState && enemy is EnemyFSM)
            {
                EnemyFSM fsm = (EnemyFSM)enemy;
                Rect stateRect = new Rect(screenPos.x - 40, screenPos.y - 28, 80, 15);
                
                // Color-code state text
                _stateStyle.normal.textColor = GetStateColor(fsm.currentState);
                GUI.Label(stateRect, fsm.currentState.ToString(), _stateStyle);
            }

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

    private Color GetStateColor(EnemyFSM.State state)
    {
        switch (state)
        {
            case EnemyFSM.State.Idle: return Color.gray;
            case EnemyFSM.State.Patrol: return Color.blue;
            case EnemyFSM.State.Seek: return Color.cyan;
            case EnemyFSM.State.Chase: return Color.red;
            case EnemyFSM.State.Strafe: return Color.yellow;
            case EnemyFSM.State.Retreat: return Color.magenta;
            case EnemyFSM.State.TakeCover: return Color.green;
            case EnemyFSM.State.Ambush: return new Color(0.5f, 0f, 0.5f);
            case EnemyFSM.State.Flank: return new Color(1f, 0.5f, 0f);
            case EnemyFSM.State.Dead: return Color.black;
            default: return Color.white;
        }
    }
}

