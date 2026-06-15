using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class HUD : MonoBehaviour
{
    [Header("Vie")]
    public Image healthBarFill;
    public TMP_Text healthText;

    [Header("Feedback mort")]
    public GameObject deathPanel;

    private Health _health;
    private bool _initialized = false;

    void Update()
    {
        // Cherche le joueur local à chaque frame jusqu'à ce qu'il soit trouvé
        if (_initialized) return;
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) return;

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.IsOwner && netObj.TryGetComponent<Health>(out var health))
            {
                _health = health;
                _health.OnHealthChanged += UpdateHealth;
                _health.OnDeath += ShowDeathPanel;

                UpdateHealth(_health.CurrentHealth, _health.maxHealth);
                _initialized = true;
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= UpdateHealth;
            _health.OnDeath -= ShowDeathPanel;
        }
    }

    private void UpdateHealth(float current, float max)
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = current / max;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        if (current > 0f && deathPanel != null)
            deathPanel.SetActive(false);
    }

    private void ShowDeathPanel()
    {
        if (deathPanel != null)
            deathPanel.SetActive(true);
    }
}