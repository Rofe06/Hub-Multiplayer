using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panneau de sélection de bonus affiché localement au joueur mort.
/// ► Placez ce composant sur le Canvas de jeu (NON un NetworkObject).
/// ► Hiérarchie suggérée :
///
///   Canvas
///   └── BonusPanel  [ce GameObject, SetActive(false) par défaut]
///       ├── TitleText          "Choisissez un bonus"
///       ├── TimerRow
///       │   ├── TimerText      (_timerText)  — TextMeshPro
///       │   └── TimerBar       (_timerFill)  — Image, Type=Filled, Method=Horizontal
///       └── CardsRow           — Horizontal Layout Group
///           ├── Card0          (BonusCard)
///           ├── Card1          (BonusCard)
///           └── Card2          (BonusCard)
/// </summary>
public class BonusSelectionUI : MonoBehaviour
{
    public static BonusSelectionUI Instance { get; private set; }

    [Header("Références UI")]
    [SerializeField] private GameObject      _panel;      // Panneau principal (activé à la mort)
    [SerializeField] private BonusCard[]     _cards;      // Exactement 3 BonusCard
    [SerializeField] private TextMeshProUGUI _timerText;  // Texte du compte à rebours (ex. "7")
    [SerializeField] private Image           _timerFill;  // Barre de progression (Image Filled)

    // ─── État interne ─────────────────────────────────────────────────────────

    private Action<BonusType> _onSelected;
    private float _timeLimit;
    private float _remaining;
    private bool  _active;

    // ─── Cycle de vie ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _panel.SetActive(false);
    }

    // ─── API publique ─────────────────────────────────────────────────────────

    /// <summary>
    /// Affiche le panneau avec 3 bonus et un compte à rebours.
    /// Appelé via ClientRpc depuis Health uniquement sur le propriétaire.
    /// </summary>
    public void Show(BonusType b1, BonusType b2, BonusType b3,
                     Action<BonusType> onSelected, float timeLimit = 8f)
    {
        _onSelected = onSelected;
        _timeLimit  = timeLimit;
        _remaining  = timeLimit;
        _active     = true;

        BonusType[] choices = { b1, b2, b3 };
        for (int i = 0; i < _cards.Length; i++)
            _cards[i].Setup(choices[i], OnCardClicked);

        _panel.SetActive(true);
    }

    // ─── Compte à rebours ─────────────────────────────────────────────────────

    private void Update()
    {
        if (!_active) return;

        _remaining -= Time.deltaTime;
        float ratio = Mathf.Clamp01(_remaining / _timeLimit);

        if (_timerText != null) _timerText.text       = Mathf.CeilToInt(_remaining).ToString();
        if (_timerFill != null) _timerFill.fillAmount = ratio;

        // Timeout : sélection automatique du premier bonus
        if (_remaining <= 0f)
            OnCardClicked(_cards[0].BonusType);
    }

    // ─── Sélection ────────────────────────────────────────────────────────────

    private void OnCardClicked(BonusType bonus)
    {
        if (!_active) return;
        _active = false;
        _panel.SetActive(false);
        _onSelected?.Invoke(bonus);
    }
}
