using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class HUD : MonoBehaviour
{
    [Header("Vie")]
    public Image healthBarFill;
    public TMP_Text healthText;

    [Header("Munitions")]
    public TMP_Text ammoText; // affiche "12 / 60"

    [Header("Feedback mort")]
    public GameObject deathPanel;

    private Health _health;
    private WeaponManager _weaponManager;
    private bool _initialized = false;

    void Update()
    {
        if (_initialized) return;
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) return;

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (!netObj.IsOwner) continue;

            if (netObj.TryGetComponent<Health>(out var health))
            {
                _health = health;
                _health.OnHealthChanged += UpdateHealth;
                _health.OnDeath += ShowDeathPanel;
                UpdateHealth(_health.CurrentHealth, _health.maxHealth);
            }

            if (netObj.TryGetComponent<WeaponManager>(out var weaponManager))
            {
                _weaponManager = weaponManager;
                _weaponManager.OnAmmoChanged += UpdateAmmo;
                UpdateAmmo(); // affichage initial
            }

            if (_health != null && _weaponManager != null)
            {
                _initialized = true;
                break;
            }
        }
    }

    void LateUpdate()
    {
        // Met à jour les munitions en continu pour refléter le rechargement en cours
        if (_weaponManager != null)
            UpdateAmmo();
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= UpdateHealth;
            _health.OnDeath -= ShowDeathPanel;
        }

        if (_weaponManager != null)
            _weaponManager.OnAmmoChanged -= UpdateAmmo;
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

    private void UpdateAmmo()
    {
        if (ammoText == null || _weaponManager == null) return;

        if (_weaponManager.IsReloading)
        {
            ammoText.text = "Rechargement...";
        }
        else
        {
            string reserveDisplay = _weaponManager.CurrentWeapon.infiniteReserve
                ? "∞"
                : _weaponManager.CurrentReserve.ToString();

            ammoText.text = $"{_weaponManager.CurrentAmmoInMag} / {reserveDisplay}";
        }
    }

    private void ShowDeathPanel()
    {
        if (deathPanel != null)
            deathPanel.SetActive(true);
    }
}