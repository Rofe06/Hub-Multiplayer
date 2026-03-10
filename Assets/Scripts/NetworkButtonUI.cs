using UnityEngine;
using Unity.Netcode;

public class NetworkButtonUI : MonoBehaviour
{
    // ── Styles ──────────────────────────────────────────────────────────────
    private GUIStyle _panelStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _btnHoverStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _dividerStyle;

    private Texture2D _panelTex;
    private Texture2D _btnNormal;
    private Texture2D _btnHover;
    private Texture2D _btnActive;
    private Texture2D _dividerTex;
    private Texture2D _accentTex;

    private bool _stylesInitialised;

    // ── Layout constants ────────────────────────────────────────────────────
    private const float PanelW    = 280f;
    private const float PanelH    = 260f;
    private const float BtnW      = 220f;
    private const float BtnH      = 46f;
    private const float BtnGap    = 12f;
    private const float PadTop    = 54f;  // room for title
    private const float PadSide   = 30f;

    // ── Colours ──────────────────────────────────────────────────────────────
    // dark navy panel  /  electric cyan accent  /  off-white text
    private static readonly Color ColPanel    = new Color(0.05f, 0.07f, 0.12f, 0.96f);
    private static readonly Color ColBorder   = new Color(0.18f, 0.24f, 0.35f, 1f);
    private static readonly Color ColBtnNorm  = new Color(0.10f, 0.14f, 0.22f, 1f);
    private static readonly Color ColBtnHov   = new Color(0.06f, 0.55f, 0.72f, 1f);   // cyan
    private static readonly Color ColBtnAct   = new Color(0.04f, 0.40f, 0.55f, 1f);
    private static readonly Color ColAccent   = new Color(0.06f, 0.65f, 0.85f, 1f);
    private static readonly Color ColTextNorm = new Color(0.78f, 0.85f, 0.95f, 1f);
    private static readonly Color ColTextHov  = Color.white;
    private static readonly Color ColDivider  = new Color(0.10f, 0.60f, 0.80f, 0.45f);

    // ── Hover tracking ───────────────────────────────────────────────────────
    private int _hoveredBtn = -1;   // index of hovered button, or -1

    // ────────────────────────────────────────────────────────────────────────

    private void InitStyles()
    {
        if (_stylesInitialised) return;
        _stylesInitialised = true;

        // ── textures ──
        _panelTex   = MakeTex(ColPanel);
        _btnNormal  = MakeTex(ColBtnNorm);
        _btnHover   = MakeTex(ColBtnHov);
        _btnActive  = MakeTex(ColBtnAct);
        _dividerTex = MakeTex(ColDivider);
        _accentTex  = MakeTex(ColAccent);

        // ── panel ──
        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal    = { background = _panelTex },
            border    = new RectOffset(4, 4, 4, 4),
            padding   = new RectOffset(0, 0, 0, 0),
            margin    = new RectOffset(0, 0, 0, 0),
            overflow  = new RectOffset(0, 0, 0, 0),
        };

        // ── title ──
        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = ColAccent },
            padding   = new RectOffset(0, 0, 0, 0),
        };

        // ── divider ──
        _dividerStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = _dividerTex },
            border  = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            margin  = new RectOffset(0, 0, 0, 0),
        };

        // ── button normal ──
        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border    = new RectOffset(6, 6, 6, 6),
            padding   = new RectOffset(0, 0, 0, 0),
            normal    = { background = _btnNormal, textColor = ColTextNorm },
            hover     = { background = _btnHover,  textColor = ColTextHov  },
            active    = { background = _btnActive, textColor = ColTextHov  },
            focused   = { background = _btnNormal, textColor = ColTextNorm },
        };
    }

    // ────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            return;

        InitStyles();

        // ── centre the panel ──
        float px = (Screen.width  - PanelW) * 0.5f;
        float py = (Screen.height - PanelH) * 0.5f;

        // ── panel background ──
        GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

        // ── thin top accent line ──
        GUI.Box(new Rect(px, py, PanelW, 3f), GUIContent.none, new GUIStyle { normal = { background = _accentTex } });

        // ── title ──
        GUI.Label(new Rect(px, py + 12f, PanelW, 28f), "⬡  NETWORK LOBBY", _titleStyle);

        // ── divider ──
        float divY = py + 44f;
        GUI.Box(new Rect(px + PadSide, divY, PanelW - PadSide * 2f, 1f), GUIContent.none, _dividerStyle);

        // ── buttons ──
        string[] labels = { "⬡  HOST A GAME", "⬡  JOIN A GAME", "⬡  DEDICATED SERVER" };
        float btnX = px + (PanelW - BtnW) * 0.5f;

        for (int i = 0; i < labels.Length; i++)
        {
            float btnY = py + PadTop + i * (BtnH + BtnGap);
            Rect  btnR = new Rect(btnX, btnY, BtnW, BtnH);

            // track hover manually for label color
            _hoveredBtn = btnR.Contains(Event.current.mousePosition) ? i : _hoveredBtn;
            if (!btnR.Contains(Event.current.mousePosition) && _hoveredBtn == i)
                _hoveredBtn = -1;

            if (GUI.Button(btnR, labels[i], _btnStyle))
            {
                switch (i)
                {
                    case 0: NetworkManager.Singleton.StartHost();   break;
                    case 1: NetworkManager.Singleton.StartClient(); break;
                    case 2: NetworkManager.Singleton.StartServer(); break;
                }
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────

    private static Texture2D MakeTex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }

    private void OnDestroy()
    {
        // clean up dynamically created textures
        Texture2D[] textures = { _panelTex, _btnNormal, _btnHover, _btnActive, _dividerTex, _accentTex };
        foreach (var t in textures)
            if (t != null) Destroy(t);
    }
}