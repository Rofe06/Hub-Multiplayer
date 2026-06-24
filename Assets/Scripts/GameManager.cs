using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

// Doit être placé directement dans la scène (pas un prefab) avec un NetworkObject.
// Netcode le spawn automatiquement quand le Host démarre.
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── État synchronisé ─────────────────────────────────────────────────────
    public NetworkVariable<int> Mode = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> KillTarget = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(
        300f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> MatchEnded = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> WinnerClientId = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> WinnerName = new NetworkVariable<FixedString64Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public GameMode CurrentMode => (GameMode)Mode.Value;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (IsServer)
        {
            // Applique les réglages choisis dans le menu par le Host
            Mode.Value = (int)GameModeSettings.SelectedMode;
            KillTarget.Value = GameModeSettings.KillTarget;
            TimeRemaining.Value = GameModeSettings.TimeLimitMinutes * 60f;

            Debug.Log($"[GameManager] Match démarré — Mode: {CurrentMode} | KillTarget: {KillTarget.Value} | TimeLimit: {TimeRemaining.Value}s");
        }
    }

    void Update()
    {
        if (!IsServer || MatchEnded.Value) return;
        if (CurrentMode != GameMode.TimeLimit) return;

        TimeRemaining.Value -= Time.deltaTime;

        if (TimeRemaining.Value <= 0f)
        {
            TimeRemaining.Value = 0f;
            EndMatchByTime();
        }
    }

    // ── Appelé par Health.cs (côté serveur) après chaque kill confirmé ─────────
    public void NotifyKill(ulong killerId, int killerCurrentKills)
    {
        if (!IsServer || MatchEnded.Value) return;
        if (CurrentMode != GameMode.KillCount) return;

        if (killerCurrentKills >= KillTarget.Value)
            EndMatch(killerId);
    }

    private void EndMatchByTime()
    {
        ulong bestId = ulong.MaxValue;
        int bestKills = -1;

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.TryGetComponent<PlayerStats>(out var stats))
            {
                if (stats.Kills.Value > bestKills)
                {
                    bestKills = stats.Kills.Value;
                    bestId = netObj.OwnerClientId;
                }
            }
        }

        EndMatch(bestId);
    }

    private void EndMatch(ulong winnerId)
    {
        MatchEnded.Value = true;
        WinnerClientId.Value = winnerId;

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.OwnerClientId == winnerId && netObj.TryGetComponent<PlayerStats>(out var stats))
            {
                WinnerName.Value = stats.PlayerName.Value;
                break;
            }
        }

        Debug.Log($"[GameManager] Match terminé ! Gagnant : {WinnerName.Value}");
    }
}