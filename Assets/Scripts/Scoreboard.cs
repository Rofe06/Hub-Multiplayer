using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Scoreboard : MonoBehaviour
{
    [Header("UI")]
    public GameObject scoreboardPanel;
    public Transform rowContainer;
    public GameObject rowPrefab;

    private Dictionary<ulong, ScoreboardRow> _rows = new Dictionary<ulong, ScoreboardRow>();
    private InputAction _tabAction;

    void Awake()
    {
        _tabAction = new InputAction("Tab", binding: "<Keyboard>/tab");
        _tabAction.performed += _ => ShowScoreboard(true);
        _tabAction.canceled += _ => ShowScoreboard(false);
        _tabAction.Enable();
    }

    void OnDestroy()
    {
        _tabAction.Disable();
        _tabAction.Dispose();
    }

    void Update()
    {
        if (scoreboardPanel != null && scoreboardPanel.activeSelf)
            RefreshRows();
    }

    private void ShowScoreboard(bool show)
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(show);

        if (show)
        {
            rowContainer.gameObject.SetActive(true);
            RebuildScoreboard();
        }
    }

    private void RebuildScoreboard()
    {
        if (NetworkManager.Singleton == null) return;

        foreach (Transform child in rowContainer)
            Destroy(child.gameObject);
        _rows.Clear();

        // Force la taille du RowContainer
        RectTransform containerRect = rowContainer.GetComponent<RectTransform>();
        RectTransform panelRect = scoreboardPanel.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(0f, 400f);

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (!netObj.TryGetComponent<PlayerStats>(out var stats)) continue;

            GameObject rowGO = Instantiate(rowPrefab, rowContainer);

            // Force la largeur de chaque ligne
            RectTransform rowRect = rowGO.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 40f);

            var row = rowGO.GetComponent<ScoreboardRow>();
            row.Setup(stats, netObj.IsOwner);
            _rows[netObj.OwnerClientId] = row;
        }
    }

    private void RefreshRows()
    {
        foreach (var kvp in _rows)
            kvp.Value.Refresh();
    }
}