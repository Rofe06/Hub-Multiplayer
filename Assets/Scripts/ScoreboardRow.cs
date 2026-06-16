using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class ScoreboardRow : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text nameText;
    public TMP_Text killsText;
    public TMP_Text deathsText;
    public TMP_Text pingText;
    public Image background;

    [Header("Couleurs")]
    public Color localPlayerColor = new Color(0.2f, 0.6f, 1f, 0.3f);
    public Color otherPlayerColor = new Color(0f, 0f, 0f, 0.3f);

    private PlayerStats _stats;
    private bool _isLocal;

    public void Setup(PlayerStats stats, bool isLocal)
    {
        _stats = stats;
        _isLocal = isLocal;

        if (background != null)
            background.color = isLocal ? localPlayerColor : otherPlayerColor;

        Refresh();
    }

    public void Refresh()
    {
        if (_stats == null)
            return;

        if (nameText != null)
            nameText.text = _stats.PlayerName.Value.ToString();
        if (killsText != null)
            killsText.text = _stats.Kills.Value.ToString();
        if (deathsText != null)
            deathsText.text = _stats.Deaths.Value.ToString();

        // Ping approximatif via Netcode
        if (pingText != null)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                ulong clientId = _stats.GetComponent<NetworkObject>().OwnerClientId;
                // Netcode ne donne pas le ping directement — on affiche N/A pour l'instant
                // ou on peut utiliser le RTT si disponible
                pingText.text = GetPing(clientId);
            }
        }
    }

    private string GetPing(ulong clientId)
    {
        try
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                return $"{Mathf.RoundToInt(transport.GetCurrentRtt(clientId))} ms";
            }
            return "-- ms";
        }
        catch
        {
            return "-- ms";
        }
    }
}