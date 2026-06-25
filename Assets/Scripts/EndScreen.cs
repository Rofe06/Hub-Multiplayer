using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class EndScreen : MonoBehaviour
{
    private GameManager _gameManager;
    private bool _isShowing = false;
    private bool _gameplayDisabled = false;

    // Scripts du joueur local à désactiver à la fin du match
    private PlayerMovements _movements;
    private PlayerController _controller;
    private WeaponManager _weaponManager;

    // ── Styles ───────────────────────────────────────────────────────────────
    private GUIStyle _panelStyle, _btnStyle, _titleStyle, _subtitleStyle, _rowStyle;
    private Texture2D _panelTex, _btnNormal, _btnHover, _btnActive, _accentTex;
    private bool _stylesInitialised;

    private const float PanelW = 460f;
    private const float PanelH = 480f;
    private const float BtnW = 280f;
    private const float BtnH = 50f;

    private static readonly Color ColPanel = new Color(0.05f, 0.07f, 0.12f, 0.97f);
    private static readonly Color ColBtnNorm = new Color(0.10f, 0.14f, 0.22f, 1f);
    private static readonly Color ColBtnHov = new Color(0.06f, 0.55f, 0.72f, 1f);
    private static readonly Color ColBtnAct = new Color(0.04f, 0.40f, 0.55f, 1f);
    private static readonly Color ColAccent = new Color(0.06f, 0.65f, 0.85f, 1f);
    private static readonly Color ColTextNorm = new Color(0.78f, 0.85f, 0.95f, 1f);

    void Update()
    {
        if (_gameManager == null)
        {
            if (GameManager.Instance != null)
            {
                _gameManager = GameManager.Instance;
                _gameManager.MatchEnded.OnValueChanged += OnMatchEndedChanged;

                if (_gameManager.MatchEnded.Value)
                    ShowEndScreen();
            }
            return;
        }

        // Trouve le joueur local une fois (pour pouvoir désactiver ses contrôles)
        if (_isShowing && !_gameplayDisabled)
            FindAndDisableLocalPlayer();
    }

    void OnDestroy()
    {
        if (_gameManager != null)
            _gameManager.MatchEnded.OnValueChanged -= OnMatchEndedChanged;
    }

    private void OnMatchEndedChanged(bool previous, bool current)
    {
        if (current) ShowEndScreen();
    }

    private void ShowEndScreen()
    {
        _isShowing = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        FindAndDisableLocalPlayer();
    }

    private void FindAndDisableLocalPlayer()
    {
        if (NetworkManager.Singleton == null) return;

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (!netObj.IsOwner) continue;

            netObj.TryGetComponent(out _movements);
            netObj.TryGetComponent(out _controller);
            netObj.TryGetComponent(out _weaponManager);

            if (_movements != null) _movements.enabled = false;
            if (_controller != null) _controller.enabled = false;
            if (_weaponManager != null) _weaponManager.enabled = false;

            _gameplayDisabled = true;
            break;
        }
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
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = ColAccent },
        };

        _subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = ColTextNorm },
        };

        _rowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = ColTextNorm },
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
    }

    void OnGUI()
    {
        if (!_isShowing) return;

        InitStyles();

        float px = (Screen.width - PanelW) * 0.5f;
        float py = (Screen.height - PanelH) * 0.5f;

        GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);
        GUI.Box(new Rect(px, py, PanelW, 3f), GUIContent.none,
                new GUIStyle { normal = { background = _accentTex } });

        bool isLocalWinner = NetworkManager.Singleton != null &&
                              _gameManager.WinnerClientId.Value == NetworkManager.Singleton.LocalClientId;

        string title = isLocalWinner ? "⬡ VICTOIRE !" : "⬡ FIN DE LA PARTIE";
        GUI.Label(new Rect(px, py + 24f, PanelW, 36f), title, _titleStyle);
        GUI.Label(new Rect(px, py + 64f, PanelW, 24f),
                   $"Gagnant : {_gameManager.WinnerName.Value}", _subtitleStyle);

        // ── Récapitulatif des scores ──
        DrawScoreRecap(px, py + 110f);

        // ── Bouton retour ──
        float btnX = px + (PanelW - BtnW) * 0.5f;
        if (GUI.Button(new Rect(btnX, py + PanelH - 70f, BtnW, BtnH), "⬡  RETOUR AU MENU", _btnStyle))
            QuitToMainMenu();
    }

    private void DrawScoreRecap(float px, float startY)
    {
        if (NetworkManager.Singleton == null) return;

        var entries = new List<(string name, int kills, int deaths)>();

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.TryGetComponent<PlayerStats>(out var stats))
                entries.Add((stats.PlayerName.Value.ToString(), stats.Kills.Value, stats.Deaths.Value));
        }

        entries = entries.OrderByDescending(e => e.kills).ToList();

        float rowH = 26f;
        float sideMargin = 30f;
        float contentW = PanelW - sideMargin * 2f;

        GUI.Label(new Rect(px + sideMargin, startY, contentW * 0.5f, rowH), "JOUEUR", _rowStyle);
        GUI.Label(new Rect(px + sideMargin + contentW * 0.5f, startY, contentW * 0.25f, rowH), "KILLS", _rowStyle);
        GUI.Label(new Rect(px + sideMargin + contentW * 0.75f, startY, contentW * 0.25f, rowH), "MORTS", _rowStyle);

        for (int i = 0; i < entries.Count; i++)
        {
            float y = startY + rowH * (i + 1);
            var e = entries[i];

            GUI.Label(new Rect(px + sideMargin, y, contentW * 0.5f, rowH), e.name, _rowStyle);
            GUI.Label(new Rect(px + sideMargin + contentW * 0.5f, y, contentW * 0.25f, rowH), e.kills.ToString(), _rowStyle);
            GUI.Label(new Rect(px + sideMargin + contentW * 0.75f, y, contentW * 0.25f, rowH), e.deaths.ToString(), _rowStyle);
        }
    }

    private void QuitToMainMenu()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        // Cache immédiatement le Canvas de jeu (HUD, munitions, scoreboard...)
        // pour éviter tout résidu visuel pendant la transition de scène
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null) canvas.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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