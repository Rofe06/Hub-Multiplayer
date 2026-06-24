using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Référence")]
    public GameObject networkButtonUI; // le panel Host/Join existant, désactivé par défaut

    // ── État ─────────────────────────────────────────────────────────────────
    private enum MenuState { Title, Options }
    private MenuState _state = MenuState.Title;

    private bool _menuActive = true; // tant que true, on affiche ce menu

    // ── Styles (même langage visuel que NetworkButtonUI) ───────────────────────
    private GUIStyle _panelStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _subtitleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _sliderLabelStyle;

    private Texture2D _panelTex, _btnNormal, _btnHover, _btnActive, _accentTex;
    private bool _stylesInitialised;

    private const float PanelW = 360f;
    private const float PanelH = 420f;
    private const float BtnW = 260f;
    private const float BtnH = 50f;

    private static readonly Color ColPanel = new Color(0.05f, 0.07f, 0.12f, 0.97f);
    private static readonly Color ColBtnNorm = new Color(0.10f, 0.14f, 0.22f, 1f);
    private static readonly Color ColBtnHov = new Color(0.06f, 0.55f, 0.72f, 1f);
    private static readonly Color ColBtnAct = new Color(0.04f, 0.40f, 0.55f, 1f);
    private static readonly Color ColAccent = new Color(0.06f, 0.65f, 0.85f, 1f);
    private static readonly Color ColTextNorm = new Color(0.78f, 0.85f, 0.95f, 1f);

    private float _tempSensitivity;
    private float _tempVolume;

    void Awake()
    {
        // Charge les valeurs sauvegardées dans des champs temporaires éditables
        _tempSensitivity = GameSettings.MouseSensitivity;
        _tempVolume = GameSettings.Volume;
        GameSettings.ApplySavedSettings();

        if (networkButtonUI != null)
            networkButtonUI.SetActive(false);
    }

    private void InitStyles()
    {
        if (_stylesInitialised) return;
        _stylesInitialised = true;

        _panelTex = MakeTex(ColPanel);
        _btnNormal = MakeTex(ColBtnNorm);
        _btnHover = MakeTex(ColBtnHov);
        _btnActive = MakeTex(ColBtnAct);
        _accentTex = MakeTex(ColAccent);

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _panelTex },
            border = new RectOffset(4, 4, 4, 4),
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = ColAccent },
        };

        _subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.5f, 0.6f, 0.7f, 1f) },
        };

        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(6, 6, 6, 6),
            normal = { background = _btnNormal, textColor = ColTextNorm },
            hover = { background = _btnHover, textColor = Color.white },
            active = { background = _btnActive, textColor = Color.white },
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = ColTextNorm },
        };

        _sliderLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = ColAccent },
        };
    }

    void Update()
    {
        // Sécurité : si une session réseau est active (host ou client),
        // force le menu à rester caché — corrige le cas où la synchronisation
        // de scène côté client réinitialise l'état du menu après un Join
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            (Unity.Netcode.NetworkManager.Singleton.IsConnectedClient ||
             Unity.Netcode.NetworkManager.Singleton.IsServer))
        {
            if (_menuActive)
            {
                _menuActive = false;
                if (networkButtonUI != null)
                    networkButtonUI.SetActive(false);
            }
        }
    }

    void OnGUI()
    {
        if (!_menuActive) return;

        InitStyles();

        float px = (Screen.width - PanelW) * 0.5f;
        float py = (Screen.height - PanelH) * 0.5f;

        GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);
        GUI.Box(new Rect(px, py, PanelW, 3f), GUIContent.none,
                new GUIStyle { normal = { background = _accentTex } });

        if (_state == MenuState.Title)
            DrawTitleScreen(px, py);
        else
            DrawOptionsScreen(px, py);
    }

    // ── Écran titre ──────────────────────────────────────────────────────────
    private void DrawTitleScreen(float px, float py)
    {
        GUI.Label(new Rect(px, py + 40f, PanelW, 40f), "⬡ FPS MULTIPLAYER", _titleStyle);
        GUI.Label(new Rect(px, py + 80f, PanelW, 20f), "Projet Epitech — 2026", _subtitleStyle);

        float btnX = px + (PanelW - BtnW) * 0.5f;
        float btnY = py + 150f;

        if (GUI.Button(new Rect(btnX, btnY, BtnW, BtnH), "⬡  JOUER", _btnStyle))
        {
            _menuActive = false;
            if (networkButtonUI != null)
                networkButtonUI.SetActive(true);
        }

        if (GUI.Button(new Rect(btnX, btnY + BtnH + 16f, BtnW, BtnH), "⬡  OPTIONS", _btnStyle))
        {
            _state = MenuState.Options;
        }

        if (GUI.Button(new Rect(btnX, btnY + (BtnH + 16f) * 2, BtnW, BtnH), "⬡  QUITTER", _btnStyle))
        {
            Application.Quit();
        }
    }

    // ── Écran options ────────────────────────────────────────────────────────
    private void DrawOptionsScreen(float px, float py)
    {
        GUI.Label(new Rect(px, py + 30f, PanelW, 30f), "OPTIONS", _titleStyle);

        float sideMargin = 30f;
        float contentW = PanelW - sideMargin * 2f;

        // ── Sensibilité souris ──
        float sensY = py + 110f;
        GUI.Label(new Rect(px + sideMargin, sensY, contentW, 20f), "Sensibilité souris", _labelStyle);
        _tempSensitivity = GUI.HorizontalSlider(
            new Rect(px + sideMargin, sensY + 24f, contentW - 50f, 20f),
            _tempSensitivity, 0.1f, 10f
        );
        GUI.Label(new Rect(px + sideMargin + contentW - 45f, sensY + 22f, 45f, 20f),
                  _tempSensitivity.ToString("F1"), _sliderLabelStyle);

        // ── Volume ──
        float volY = sensY + 70f;
        GUI.Label(new Rect(px + sideMargin, volY, contentW, 20f), "Volume", _labelStyle);
        _tempVolume = GUI.HorizontalSlider(
            new Rect(px + sideMargin, volY + 24f, contentW - 50f, 20f),
            _tempVolume, 0f, 1f
        );
        GUI.Label(new Rect(px + sideMargin + contentW - 45f, volY + 22f, 45f, 20f),
                  $"{Mathf.RoundToInt(_tempVolume * 100f)}%", _sliderLabelStyle);

        // Applique en temps réel pour feedback immédiat
        GameSettings.MouseSensitivity = _tempSensitivity;
        GameSettings.Volume = _tempVolume;

        // ── Retour ──
        float btnX = px + (PanelW - BtnW) * 0.5f;
        float btnY = py + PanelH - 80f;

        if (GUI.Button(new Rect(btnX, btnY, BtnW, BtnH), "⬡  RETOUR", _btnStyle))
        {
            _state = MenuState.Title;
        }
    }

    private static Texture2D MakeTex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }
}