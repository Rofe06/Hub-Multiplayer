using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Carte individuelle affichée dans le menu de sélection de bonus.
/// Placez ce composant sur chacun des 3 GameObjects carte.
/// </summary>
public class BonusCard : MonoBehaviour
{
    [Header("Références UI")]
    [SerializeField] private TextMeshProUGUI _iconText;   // Texte emoji/icône
    [SerializeField] private TextMeshProUGUI _nameText;   // Nom du bonus
    [SerializeField] private TextMeshProUGUI _descText;   // Description courte
    [SerializeField] private Image           _background; // Image de fond colorée
    [SerializeField] private Button          _button;     // Bouton de sélection

    // Bonus actuellement affiché sur cette carte
    public BonusType BonusType { get; private set; }

    /// <summary>Initialise la carte avec un bonus et son callback de sélection.</summary>
    public void Setup(BonusType type, Action<BonusType> onClick)
    {
        BonusType      = type;
        BonusData data = BonusDatabase.Get(type);

        if (_iconText   != null) _iconText.text     = data.icon;
        if (_nameText   != null) _nameText.text     = data.displayName;
        if (_descText   != null) _descText.text     = data.description;
        if (_background != null) _background.color  = data.color;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick(BonusType));
    }
}
