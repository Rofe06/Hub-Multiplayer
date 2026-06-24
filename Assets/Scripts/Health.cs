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
        if (current) ApplyDeathState();
        else ApplyAliveState();
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

    // killerId = OwnerClientId du tireur
    public void TakeDamage(float amount, ulong killerId = ulong.MaxValue)
    {
        if (!IsServer || IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.MatchEnded.Value) return; // partie terminée

        _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - amount);

        if (_currentHealth.Value <= 0f)
            StartCoroutine(DeathAndRespawn(killerId));
    }

    private System.Collections.IEnumerator DeathAndRespawn(ulong killerId)
    {
        _isDying.Value = true;

        // Ajoute une mort au joueur tué
        if (TryGetComponent<PlayerStats>(out var myStats))
            myStats.AddDeath();

        // Ajoute un kill au tireur
        if (killerId != ulong.MaxValue)
        {
            foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (netObj.OwnerClientId == killerId &&
                    netObj.TryGetComponent<PlayerStats>(out var killerStats))
                {
                    killerStats.AddKill();

                    // Notifie le GameManager pour vérifier la condition de victoire
                    GameManager.Instance?.NotifyKill(killerId, killerStats.Kills.Value);
                    break;
                }
            }
        }

        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = GetSpawnPoint();

        if (_cc != null) _cc.enabled = false;
        transform.position = spawnPos;
        if (_cc != null) _cc.enabled = true;

        TeleportClientRpc(spawnPos);

        _currentHealth.Value = maxHealth;
        _isDying.Value = false;
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 position)
    {
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