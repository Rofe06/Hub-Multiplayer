using UnityEngine;
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float respawnDelay = 3f;

    private NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _isDying = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentHealth => _currentHealth.Value;
    public bool IsDead => _currentHealth.Value <= 0f;

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action OnDeath;

    private CharacterController _cc;
    private PlayerMovements _movements;
    private PlayerController _controller;

    public override void OnNetworkSpawn()
    {
        _cc = GetComponent<CharacterController>();
        _movements = GetComponent<PlayerMovements>();
        _controller = GetComponent<PlayerController>();

        _currentHealth.OnValueChanged += HandleHealthChanged;
        _isDying.OnValueChanged += HandleDyingChanged;

        if (IsServer)
            _currentHealth.Value = maxHealth;
    }

    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= HandleHealthChanged;
        _isDying.OnValueChanged -= HandleDyingChanged;
    }

    private void HandleHealthChanged(float previous, float current)
    {
        OnHealthChanged?.Invoke(current, maxHealth);
        if (current <= 0f)
            OnDeath?.Invoke();
    }

    private void HandleDyingChanged(bool previous, bool current)
    {
        if (current)
            ApplyDeathState();
        else
            ApplyAliveState();
    }

    private void ApplyDeathState()
    {
        if (_movements != null) _movements.enabled = false;
        if (_controller != null) _controller.enabled = false;

        if (IsOwner)
        {
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ApplyAliveState()
    {
        if (_movements != null) _movements.enabled = true;
        if (_controller != null) _controller.enabled = true;

        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer || IsDead) return;

        _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - amount);

        if (_currentHealth.Value <= 0f)
            StartCoroutine(DeathAndRespawn());
    }

    private System.Collections.IEnumerator DeathAndRespawn()
    {
        _isDying.Value = true;

        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = GetSpawnPoint();

        // Téléporte côté serveur
        if (_cc != null) _cc.enabled = false;
        transform.position = spawnPos;
        if (_cc != null) _cc.enabled = true;

        // Téléporte côté client owner via ClientRpc
        TeleportClientRpc(spawnPos);

        _currentHealth.Value = maxHealth;
        _isDying.Value = false;
    }

    // ── Téléporte le joueur côté client ──────────────────────────────────────
    [ClientRpc]
    private void TeleportClientRpc(Vector3 position)
    {
        // Ne s'applique qu'au owner (le joueur concerné)
        if (!IsOwner) return;

        if (_cc != null) _cc.enabled = false;
        transform.position = position;
        if (_cc != null) _cc.enabled = true;
    }

    private Vector3 GetSpawnPoint()
    {
        GameObject sp = GameObject.Find("SpawnPoint");
        return sp != null ? sp.transform.position : new Vector3(0f, 1f, 0f);
    }
}