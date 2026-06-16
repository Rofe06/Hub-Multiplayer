using Unity.Netcode;
using Unity.Collections;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> Kills = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Deaths = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(
        "Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            PlayerName.Value = $"Player {OwnerClientId}";
    }

    public void AddKill()
    {
        if (IsServer) Kills.Value++;
    }

    public void AddDeath()
    {
        if (IsServer) Deaths.Value++;
    }
}