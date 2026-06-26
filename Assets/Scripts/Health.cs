using UnityEngine;
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth    = 100f;
    public float respawnDelay = 3f;   // délai (en s) après le choix du bonus avant le respawn

    [Header("Bonus")]
    [SerializeField] private float _bonusSelectTime = 8f; // temps accordé pour choisir un bonus

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
    public bool  IsDead        => _currentHealth.Value <= 0f;

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action               OnDeath;

    private CharacterController _cc;
    private PlayerMovements     _movements;
    private PlayerController    _controller;
    private PlayerBonuses       _bonuses;   // ← nouveau

    /// Serveur uniquement — bloque DeathAndRespawn tant que le joueur choisit.
    private bool _waitingForBonus = false;

    // ─── Cycle de vie réseau ─────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        _cc         = GetComponent<CharacterController>();
        _movements  = GetComponent<PlayerMovements>();
        _controller = GetComponent<PlayerController>();
        _bonuses    = GetComponent<PlayerBonuses>();   // ← nouveau

        _currentHealth.OnValueChanged += HandleHealthChanged;
        _isDying.OnValueChanged       += HandleDyingChanged;

        if (IsServer)
            _currentHealth.Value = maxHealth;
    }

    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= HandleHealthChanged;
        _isDying.OnValueChanged       -= HandleDyingChanged;
    }

    // ─── Callbacks NetworkVariable ────────────────────────────────────────────

    private void HandleHealthChanged(float previous, float current)
    {
        OnHealthChanged?.Invoke(current, TotalMaxHealth());
        if (current <= 0f)
            OnDeath?.Invoke();
    }

    private void HandleDyingChanged(bool previous, bool current)
    {
        if (current) ApplyDeathState();
        else         ApplyAliveState();
    }

    // ─── États mort / vivant ─────────────────────────────────────────────────

    private void ApplyDeathState()
    {
        if (_movements  != null) _movements.enabled  = false;
        if (_controller != null) _controller.enabled = false;

        if (IsOwner)
        {
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
            Cursor.lockState   = CursorLockMode.None;
            Cursor.visible     = true;
        }
    }

    private void ApplyAliveState()
    {
        if (_movements  != null) _movements.enabled  = true;
        if (_controller != null) _controller.enabled = true;

        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    // ─── Dégâts et soins ─────────────────────────────────────────────────────

    /// <summary>killerId = OwnerClientId du tireur.</summary>
    public void TakeDamage(float amount, ulong killerId = ulong.MaxValue)
    {
        if (!IsServer || IsDead) return;
        if (GameManager.Instance != null && GameManager.Instance.MatchEnded.Value) return;

        // Réduction d'armure (bonus ArmorBoost)
        if (_bonuses != null && _bonuses.DamageReduction.Value > 0f)
            amount *= (1f - _bonuses.DamageReduction.Value);

        _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - amount);

        if (_currentHealth.Value <= 0f)
            StartCoroutine(DeathAndRespawn(killerId));
    }

    /// <summary>Soigne le joueur jusqu'au maximum (HP de base + bonus). Serveur uniquement.</summary>
    public void Heal(float amount)
    {
        if (!IsServer) return;
        _currentHealth.Value = Mathf.Min(_currentHealth.Value + amount, TotalMaxHealth());
    }

    // ─── Mort → sélection de bonus → respawn ─────────────────────────────────

    private System.Collections.IEnumerator DeathAndRespawn(ulong killerId)
    {
        _isDying.Value = true;

        // ── Stats : mort / kill ──────────────────────────────────────────────
        if (TryGetComponent<PlayerStats>(out var myStats))
            myStats.AddDeath();

        if (killerId != ulong.MaxValue)
        {
            foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (netObj.OwnerClientId == killerId &&
                    netObj.TryGetComponent<PlayerStats>(out var killerStats))
                {
                    killerStats.AddKill();
                    GameManager.Instance?.NotifyKill(killerId, killerStats.Kills.Value);
                    break;
                }
            }
        }

        // ── Sélection de bonus ───────────────────────────────────────────────
        _waitingForBonus = true;
        BonusType[] choices = BonusDatabase.GetRandom(3);
        Debug.Log($"[Health] Appel ShowBonusSelectionClientRpc avec {choices[0]}, {choices[1]}, {choices[2]}");
        ShowBonusSelectionClientRpc(choices[0], choices[1], choices[2], _bonusSelectTime);

        // Attend la réponse du joueur ou le timeout
        float elapsed = 0f;
        while (_waitingForBonus && elapsed < _bonusSelectTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        _waitingForBonus = false; // reset au cas où timeout
        // ────────────────────────────────────────────────────────────────────

        // ── Respawn ──────────────────────────────────────────────────────────
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = GetSpawnPoint();

        if (_cc != null) _cc.enabled = false;
        transform.position = spawnPos;
        if (_cc != null) _cc.enabled = true;

        TeleportClientRpc(spawnPos);

        _currentHealth.Value = TotalMaxHealth(); // HP max avec bonus inclus
        _isDying.Value       = false;
    }

    // ─── RPCs ─────────────────────────────────────────────────────────────────

    /// <summary>Envoie les 3 choix de bonus au propriétaire du personnage.</summary>
    [ClientRpc]
    private void ShowBonusSelectionClientRpc(BonusType b1, BonusType b2, BonusType b3, float timeLimit)
    {
        Debug.Log($"[Health] RPC reçue côté client ! IsOwner: {IsOwner}");
        if (!IsOwner) return;
        Debug.Log($"[Health] BonusSelectionUI.Instance est null ? {BonusSelectionUI.Instance == null}");
        BonusSelectionUI.Instance?.Show(b1, b2, b3, OnBonusChosen, timeLimit);
    }

    /// Appelé localement depuis BonusSelectionUI via le callback.
    private void OnBonusChosen(BonusType bonus)
    {
        ConfirmBonusServerRpc(bonus);
    }

    /// <summary>Le client envoie son choix au serveur → applique le bonus et débloque le respawn.</summary>
    [ServerRpc(RequireOwnership = true)]
    private void ConfirmBonusServerRpc(BonusType bonus)
    {
        _bonuses?.ApplyBonus(bonus);
        _waitingForBonus = false;
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 position)
    {
        if (!IsOwner) return;
        if (_cc != null) _cc.enabled = false;
        transform.position = position;
        if (_cc != null) _cc.enabled = true;
    }

    // ─── Utilitaires ─────────────────────────────────────────────────────────

    /// <summary>HP max total = base + bonus permanents accumulés.</summary>
    private float TotalMaxHealth() =>
        maxHealth + (_bonuses != null ? _bonuses.BonusMaxHealth.Value : 0f);

    private Vector3 GetSpawnPoint()
    {
        GameObject sp = GameObject.Find("SpawnPoint");
        return sp != null ? sp.transform.position : new Vector3(0f, 1f, 0f);
    }
}
