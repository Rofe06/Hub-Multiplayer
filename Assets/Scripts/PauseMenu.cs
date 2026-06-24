using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PauseMenu : MonoBehaviour
{
    // ── État ─────────────────────────────────────────────────────────────────
    private enum MenuState { Closed, Main, Options }
    private MenuState _state = MenuState.Closed;

    private InputAction _escapeAction;

    // Scripts du joueur local à désactiver pendant la pause
    private PlayerMovements _movements;
    private PlayerController _controller;
    private WeaponManager _weaponManager;

    private float _tempSensitivity;
    private float _tempVolume;

    // ── Styles (cohérents avec MainMenu / NetworkButtonUI) ─────────────────────
    private GUIStyle _panelStyle, _btnStyle, _titleStyle, _labelStyle, _sliderLabelStyle;
    private Texture2D _panelTex, _btnNormal, _btnHover, _btnActive, _accentTex;
    private bool _stylesInitialised;

    private const float PanelW = 340f;
    private const float PanelH = 360f;
    private const float BtnW = 250f;
    private const float BtnH = 48f;

    private static readonly Color ColPanel = new Color(0.05f, 0.07f, 0.12f, 0.97f);
    private static readonly Color ColBtnNorm = new Color(0.10f, 0.14f, 0.22f, 1f);
    private static readonly Color ColBtnHov = new Color(0.06f, 0.55f, 0.72f, 1f);
    private static readonly Color ColBtnAct = new Color(0.04f, 0.40f, 0.55f, 1f);
    private static readonly Color ColAccent = new Color(0.06f, 0.65f, 0.85f, 1f);
    private static readonly Color ColTextNorm = new Color(0.78f, 0.85f, 0.95f, 1f);

    void Awake()
    {
        _escapeAction = new InputAction("Pause", binding: "<Keyboard>/escape");
        _escapeAction.performed += _ => TogglePause();
        _escapeAction.Enable();

        _tempSensitivity = GameSettings.MouseSensitivity;
        _tempVolume = GameSettings.Volume;
    }

    void OnDestroy()
    {
        _escapeAction.Disable();
        _escapeAction.Dispose();
    }

    // ── Trouve le joueur local (une fois spawné) ────────────────────────────
    private void FindLocalPlayer()
    {
        if (_movements != null) return; // déjà trouvé
        if (NetworkManager.Singleton == null) return;

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (!netObj.IsOwner) continue;

            netObj.TryGetComponent(out _movements);
            netObj.TryGetComponent(out _controller);
            netObj.TryGetComponent(out _weaponManager);
            break;
        }
    }

    private void TogglePause()
    {
        // Pas de pause possible si on n'est pas encore en partie
        FindLocalPlayer();
        if (_movements == null && _controller == null) return;

        if (_state == MenuState.Closed)
            OpenPause();
        else
            ClosePause();
    }

    private void OpenPause()
    {
        _state = MenuState.Main;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetGameplayEnabled(false);
    }

    private void ClosePause()
    {
        _state = MenuState.Closed;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetGameplayEnabled(true);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (_movements != null) _movements.enabled = enabled;
        if (_controller != null) _controller.enabled = enabled;
        if (_weaponManager != null) _weaponManager.enabled = enabled;
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
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = ColAccent },
        };

        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
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

    void OnGUI()
    {
        if (_state == MenuState.Closed) return;

        InitStyles();

        float px = (Screen.width - PanelW) * 0.5f;
        float py = (Screen.height - PanelH) * 0.5f;

        GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);
        GUI.Box(new Rect(px, py, PanelW, 3f), GUIContent.none,
                new GUIStyle { normal = { background = _accentTex } });

        if (_state == MenuState.Main)
            DrawMainPause(px, py);
        else
            DrawOptions(px, py);
    }

    private void DrawMainPause(float px, float py)
    {
        GUI.Label(new Rect(px, py + 24f, PanelW, 30f), "⬡ PAUSE", _titleStyle);

        float btnX = px + (PanelW - BtnW) * 0.5f;
        float y = py + 90f;

        if (GUI.Button(new Rect(btnX, y, BtnW, BtnH), "⬡  REPRENDRE", _btnStyle))
            ClosePause();

        y += BtnH + 14f;
        if (GUI.Button(new Rect(btnX, y, BtnW, BtnH), "⬡  OPTIONS", _btnStyle))
            _state = MenuState.Options;

        y += BtnH + 14f;
        if (GUI.Button(new Rect(btnX, y, BtnW, BtnH), "⬡  QUITTER LA PARTIE", _btnStyle))
            QuitToMainMenu();
    }

    private void DrawOptions(float px, float py)
    {
        GUI.Label(new Rect(px, py + 24f, PanelW, 30f), "OPTIONS", _titleStyle);

        float sideMargin = 30f;
        float contentW = PanelW - sideMargin * 2f;

        float sensY = py + 90f;
        GUI.Label(new Rect(px + sideMargin, sensY, contentW, 20f), "Sensibilité souris", _labelStyle);
        _tempSensitivity = GUI.HorizontalSlider(
            new Rect(px + sideMargin, sensY + 24f, contentW - 50f, 20f),
            _tempSensitivity, 0.1f, 10f);
        GUI.Label(new Rect(px + sideMargin + contentW - 45f, sensY + 22f, 45f, 20f),
                  _tempSensitivity.ToString("F1"), _sliderLabelStyle);

        float volY = sensY + 70f;
        GUI.Label(new Rect(px + sideMargin, volY, contentW, 20f), "Volume", _labelStyle);
        _tempVolume = GUI.HorizontalSlider(
            new Rect(px + sideMargin, volY + 24f, contentW - 50f, 20f),
            _tempVolume, 0f, 1f);
        GUI.Label(new Rect(px + sideMargin + contentW - 45f, volY + 22f, 45f, 20f),
                  $"{Mathf.RoundToInt(_tempVolume * 100f)}%", _sliderLabelStyle);

        GameSettings.MouseSensitivity = _tempSensitivity;
        GameSettings.Volume = _tempVolume;

        float btnX = px + (PanelW - BtnW) * 0.5f;
        if (GUI.Button(new Rect(btnX, py + PanelH - 70f, BtnW, BtnH), "⬡  RETOUR", _btnStyle))
            _state = MenuState.Main;
    }

    // ── Quitte proprement la partie et revient au menu principal ──────────────
    private void QuitToMainMenu()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Recharge la scène pour repartir sur un état propre
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static Texture2D MakeTex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }
}