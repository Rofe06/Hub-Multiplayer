using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Stocke et applique les bonus cumulés d'un joueur.
/// ► Ajoutez ce composant sur le même GameObject que Health.
/// ► Référencez les NetworkVariables publiques dans vos scripts
///   de mouvement et d'arme (voir commentaires en bas).
/// </summary>
public class PlayerBonuses : NetworkBehaviour
{
    // ─── Stats réseau (lecture = tout le monde | écriture = serveur) ─────────

    public NetworkVariable<float> SpeedMultiplier  = new NetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> DamageMultiplier = new NetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> JumpMultiplier   = new NetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> RegenPerSecond   = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> DamageReduction  = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> BonusMaxHealth   = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ─── Références internes ─────────────────────────────────────────────────

    private Health _health;

    public override void OnNetworkSpawn()
    {
        _health = GetComponent<Health>();
    }

    // ─── Régénération (serveur uniquement, chaque frame) ─────────────────────

    private void Update()
    {
        if (!IsServer || _health == null || _health.IsDead) return;
        if (RegenPerSecond.Value > 0f)
            _health.Heal(RegenPerSecond.Value * Time.deltaTime);
    }

    // ─── Application d'un bonus ───────────────────────────────────────────────

    /// <summary>Applique un bonus au joueur. Serveur uniquement.</summary>
    public void ApplyBonus(BonusType bonus)
    {
        if (!IsServer) return;

        switch (bonus)
        {
            case BonusType.SpeedBoost:
                SpeedMultiplier.Value  += 0.20f;
                break;

            case BonusType.MaxHealth:
                BonusMaxHealth.Value   += 50f;
                _health?.Heal(50f);          // soigne immédiatement le bonus accordé
                break;

            case BonusType.DamageBoost:
                DamageMultiplier.Value += 0.25f;
                break;

            case BonusType.JumpBoost:
                JumpMultiplier.Value   += 0.30f;
                break;

            case BonusType.Regeneration:
                RegenPerSecond.Value   += 3f;
                break;

            case BonusType.ArmorBoost:
                // Plafonnée à 75 % de réduction pour éviter l'invincibilité
                DamageReduction.Value   = Mathf.Min(DamageReduction.Value + 0.20f, 0.75f);
                break;
        }
    }
}

/*
 ╔══════════════════════════════════════════════════════════════════════════════╗
 ║  INTÉGRATION DANS VOS AUTRES SCRIPTS                                        ║
 ╠══════════════════════════════════════════════════════════════════════════════╣
 ║                                                                              ║
 ║  ► PlayerMovements.cs  (vitesse & saut)                                      ║
 ║  ─────────────────────────────────────                                        ║
 ║  private PlayerBonuses _bonuses;                                              ║
 ║                                                                              ║
 ║  void Start() { _bonuses = GetComponent<PlayerBonuses>(); }                  ║
 ║                                                                              ║
 ║  // Dans Update / FixedUpdate :                                              ║
 ║  float speed = baseSpeed * (_bonuses != null                                 ║
 ║                    ? _bonuses.SpeedMultiplier.Value : 1f);                   ║
 ║  float jump  = baseJump  * (_bonuses != null                                 ║
 ║                    ? _bonuses.JumpMultiplier.Value  : 1f);                   ║
 ║                                                                              ║
 ║  ► Weapon.cs / Gun.cs  (dégâts)                                              ║
 ║  ───────────────────────────────                                              ║
 ║  private PlayerBonuses _bonuses;                                              ║
 ║                                                                              ║
 ║  void Start() { _bonuses = GetComponentInParent<PlayerBonuses>(); }          ║
 ║                                                                              ║
 ║  // Au moment de tirer :                                                     ║
 ║  float dmg = baseDamage * (_bonuses != null                                  ║
 ║                  ? _bonuses.DamageMultiplier.Value : 1f);                   ║
 ║  target.TakeDamage(dmg, OwnerClientId);                                      ║
 ║                                                                              ║
 ╚══════════════════════════════════════════════════════════════════════════════╝
*/
