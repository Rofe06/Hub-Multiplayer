using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class HUD : MonoBehaviour
{
    [Header("Vie")]
    public Image healthBarFill;   // Image remplie (type Filled)
    public TMP_Text healthText;    // "75 / 100"

    [Header("Feedback mort")]
    public GameObject deathPanel;  // Panel rouge "VOUS ÊTES MORT"

    private Health _health;

    void Start()
    {
        // Attend que le NetworkManager spawne le joueur local
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        if (_health != null)
        {
            _health.OnHealthChanged -= UpdateHealth;
            _health.OnDeath -= ShowDeathPanel;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Cherche le joueur local parmi les objets spawnés
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.IsOwner && netObj.TryGetComponent<Health>(out var health))
            {
                _health = health;
                _health.OnHealthChanged += UpdateHealth;
                _health.OnDeath += ShowDeathPanel;

                // Init affichage
                UpdateHealth(_health.CurrentHealth, _health.maxHealth);
                break;
            }
        }
    }

    private void UpdateHealth(float current, float max)
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = current / max;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        // Cache le panel de mort si on respawn
        if (current > 0f && deathPanel != null)
            deathPanel.SetActive(false);
    }

    private void ShowDeathPanel()
    {
        if (deathPanel != null)
            deathPanel.SetActive(true);
    }
}