using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkButtonUI : MonoBehaviour
{
    // ── Styles ──────────────────────────────────────────────────────────────
    private GUIStyle _panelStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _dividerStyle;
    private GUIStyle _inputStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _ipLabelStyle;

    private Texture2D _panelTex;
    private Texture2D _btnNormal;
    private Texture2D _btnHover;
    private Texture2D _btnActive;
    private Texture2D _dividerTex;
    private Texture2D _accentTex;
    private Texture2D _inputTex;
    private Texture2D _inputBorderTex;

    private bool _stylesInitialised;

    // ── Layout constants ────────────────────────────────────────────────────
    private const float PanelW  = 280f;
    private const float PanelH  = 320f; // plus grand pour le champ IP
    private const float BtnW    = 220f;
    private const float BtnH    = 46f;
    private const float BtnGap  = 12f;
    private const float PadTop  = 110f; // descend pour laisser place au champ IP
    private const float PadSide = 30f;

    // ── Colours ──────────────────────────────────────────────────────────────
    private static readonly Color ColPanel    = new Color(0.05f, 0.07f, 0.12f, 0.96f);
    private static readonly Color ColBtnNorm  = new Color(0.10f, 0.14f, 0.22f, 1f);
    private static readonly Color ColBtnHov   = new Color(0.06f, 0.55f, 0.72f, 1f);
    private static readonly Color ColBtnAct   = new Color(0.04f, 0.40f, 0.55f, 1f);
    private static readonly Color ColAccent   = new Color(0.06f, 0.65f, 0.85f, 1f);
    private static readonly Color ColTextNorm = new Color(0.78f, 0.85f, 0.95f, 1f);
    private static readonly Color ColTextHov  = Color.white;
    private static readonly Color ColDivider  = new Color(0.10f, 0.60f, 0.80f, 0.45f);
    private static readonly Color ColInput    = new Color(0.08f, 0.11f, 0.18f, 1f);
    private static readonly Color ColInputBorder = new Color(0.06f, 0.55f, 0.72f, 0.7f);

    // ── IP Field ─────────────────────────────────────────────────────────────
    private string _ipAddress = "127.0.0.1";
    private const ushort Port  = 7777;

    private int _hoveredBtn = -1;

    // ────────────────────────────────────────────────────────────────────────

    private void InitStyles()
    {
        if (_stylesInitialised) return;
        _stylesInitialised = true;

        _panelTex       = MakeTex(ColPanel);
        _btnNormal      = MakeTex(ColBtnNorm);
        _btnHover       = MakeTex(ColBtnHov);
        _btnActive      = MakeTex(ColBtnAct);
        _dividerTex     = MakeTex(ColDivider);
        _accentTex      = MakeTex(ColAccent);
        _inputTex       = MakeTex(ColInput);
        _inputBorderTex = MakeTex(ColInputBorder);

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = _panelTex },
            border  = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(0, 0, 0, 0),
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = ColAccent },
        };

        _dividerStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = _dividerTex },
            border  = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            margin  = new RectOffset(0, 0, 0, 0),
        };

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

        // ── champ IP ──
        _inputStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal    = { background = _inputTex, textColor = Color.white },
            focused   = { background = _inputTex, textColor = Color.white },
            hover     = { background = _inputTex, textColor = Color.white },
            padding   = new RectOffset(8, 8, 0, 0),
            border    = new RectOffset(2, 2, 2, 2),
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = new Color(0.5f, 0.7f, 0.9f, 1f) },
        };

        _ipLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 10,
            alignment = TextAnchor.MiddleRight,
            normal    = { textColor = new Color(0.4f, 0.5f, 0.6f, 1f) },
        };
    }

    // ────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            return;

        InitStyles();

        float px = (Screen.width  - PanelW) * 0.5f;
        float py = (Screen.height - PanelH) * 0.5f;

        // ── panel ──
        GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

        // ── accent top line ──
        GUI.Box(new Rect(px, py, PanelW, 3f),
                GUIContent.none,
                new GUIStyle { normal = { background = _accentTex } });

        // ── titre ──
        GUI.Label(new Rect(px, py + 12f, PanelW, 28f), "⬡  NETWORK LOBBY", _titleStyle);

        // ── divider ──
        GUI.Box(new Rect(px + PadSide, py + 44f, PanelW - PadSide * 2f, 1f),
                GUIContent.none, _dividerStyle);

        // ── label + champ IP ──────────────────────────────────────────────
        float fieldY = py + 54f;
        GUI.Label(new Rect(px + PadSide, fieldY, 120f, 20f), "IP du serveur hôte", _labelStyle);

        // Petite indication port
        GUI.Label(new Rect(px + PanelW - PadSide - 70f, fieldY, 70f, 20f),
                  $"Port : {Port}", _ipLabelStyle);

        // Bordure du champ
        GUI.Box(new Rect(px + PadSide - 1, fieldY + 21f, BtnW + 2f, 34f),
                GUIContent.none,
                new GUIStyle { normal = { background = _inputBorderTex } });

        // Champ de saisie IP
        _ipAddress = GUI.TextField(
            new Rect(px + PadSide, fieldY + 22f, BtnW, 32f),
            _ipAddress,
            _inputStyle
        );

        // ── divider ──
        GUI.Box(new Rect(px + PadSide, py + 100f, PanelW - PadSide * 2f, 1f),
                GUIContent.none, _dividerStyle);

        // ── boutons ──────────────────────────────────────────────────────
        string[] labels = { "⬡  HOST A GAME", "⬡  JOIN A GAME", "⬡  DEDICATED SERVER" };
        float btnX = px + (PanelW - BtnW) * 0.5f;

        for (int i = 0; i < labels.Length; i++)
        {
            float btnY = py + PadTop + i * (BtnH + BtnGap);
            Rect  btnR = new Rect(btnX, btnY, BtnW, BtnH);

            _hoveredBtn = btnR.Contains(Event.current.mousePosition) ? i : _hoveredBtn;
            if (!btnR.Contains(Event.current.mousePosition) && _hoveredBtn == i)
                _hoveredBtn = -1;

            if (GUI.Button(btnR, labels[i], _btnStyle))
            {
                var transport = NetworkManager.Singleton
                                .GetComponent<UnityTransport>();
                switch (i)
                {
                    case 0: // HOST — écoute sur toutes les interfaces
                        transport.SetConnectionData("0.0.0.0", Port);
                        NetworkManager.Singleton.StartHost();
                        break;

                    case 1: // CLIENT — se connecte à l'IP saisie
                        transport.SetConnectionData(_ipAddress.Trim(), Port);
                        NetworkManager.Singleton.StartClient();
                        break;

                    case 2: // SERVEUR DÉDIÉ
                        transport.SetConnectionData("0.0.0.0", Port);
                        NetworkManager.Singleton.StartServer();
                        break;
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
        Texture2D[] textures = { _panelTex, _btnNormal, _btnHover, _btnActive,
                                  _dividerTex, _accentTex, _inputTex, _inputBorderTex };
        foreach (var t in textures)
            if (t != null) Destroy(t);
    }
}