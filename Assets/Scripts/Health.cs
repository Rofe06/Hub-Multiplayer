using UnityEngine;
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;

    // NetworkVariable = synchronisé automatiquement chez tous les clients
    private NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentHealth => _currentHealth.Value;
    public bool IsDead => _currentHealth.Value <= 0f;

    // ── Events ────────────────────────────────────────────────────────────────
    public event System.Action<float, float> OnHealthChanged; // (current, max)
    public event System.Action OnDeath;

    // ────────────────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        _currentHealth.OnValueChanged += HandleHealthChanged;

        if (IsServer)
            _currentHealth.Value = maxHealth;
    }

    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float previous, float current)
    {
        OnHealthChanged?.Invoke(current, maxHealth);
        Debug.Log($"[Health] {gameObject.name} : {current}/{maxHealth} HP");

        if (current <= 0f)
            OnDeath?.Invoke();
    }

    // ── Appelé par Gun.cs côté serveur ───────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (!IsServer || IsDead) return;

        _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - amount);

        if (_currentHealth.Value <= 0f)
            HandleDeathServerSide();
    }

    private void HandleDeathServerSide()
    {
        Debug.Log($"[Health] {gameObject.name} est mort !");
        // Pour l'instant on respawn simplement après 3 secondes
        StartCoroutine(RespawnCoroutine());
    }

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(3f);

        // Reset HP
        _currentHealth.Value = maxHealth;

        // Téléporte au spawn (Vector3.zero pour l'instant)
        if (TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = false;
            transform.position = Vector3.zero;
            cc.enabled = true;
        }

        Debug.Log($"[Health] {gameObject.name} a respawn !");
    }
}