using UnityEngine;

public class GameTimer : MonoBehaviour
{
    private GUIStyle _timerStyle;
    private GUIStyle _bgStyle;
    private Texture2D _bgTex;
    private bool _stylesInitialised;

    private static readonly Color ColBg     = new Color(0.05f, 0.07f, 0.12f, 0.85f);
    private static readonly Color ColAccent = new Color(0.06f, 0.65f, 0.85f, 1f);

    private const float PanelW = 140f;
    private const float PanelH = 44f;

    private void InitStyles()
    {
        if (_stylesInitialised) return;
        _stylesInitialised = true;

        _bgTex = MakeTex(ColBg);

        _bgStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _bgTex },
        };

        _timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = ColAccent },
        };
    }

    void OnGUI()
    {
        // N'affiche le timer que si le mode "Temps" est actif et le match en cours
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentMode != GameMode.TimeLimit) return;
        if (GameManager.Instance.MatchEnded.Value) return;

        InitStyles();

        float px = (Screen.width - PanelW) * 0.5f;
        float py = 16f;

        GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _bgStyle);

        float seconds = Mathf.Max(0f, GameManager.Instance.TimeRemaining.Value);
        int   minutes = Mathf.FloorToInt(seconds / 60f);
        int   secs    = Mathf.FloorToInt(seconds % 60f);

        // Passe en rouge quand il reste moins de 30 secondes (urgence)
        _timerStyle.normal.textColor = seconds <= 30f
            ? new Color(0.9f, 0.2f, 0.2f, 1f)
            : ColAccent;

        GUI.Label(new Rect(px, py, PanelW, PanelH), $"{minutes:00}:{secs:00}", _timerStyle);
    }

    private static Texture2D MakeTex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }
}
