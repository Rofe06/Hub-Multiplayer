using UnityEngine;

// Stocke les réglages persistants du joueur (sensibilité, volume)
// via PlayerPrefs — survit entre les sessions de jeu
public static class GameSettings
{
    private const string SensitivityKey = "Settings_MouseSensitivity";
    private const string VolumeKey = "Settings_Volume";

    private const float DefaultSensitivity = 2f;
    private const float DefaultVolume = 1f;

    public static event System.Action OnSettingsChanged;

    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
        set
        {
            PlayerPrefs.SetFloat(SensitivityKey, value);
            OnSettingsChanged?.Invoke();
        }
    }

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        set
        {
            PlayerPrefs.SetFloat(VolumeKey, value);
            AudioListener.volume = value;
            OnSettingsChanged?.Invoke();
        }
    }

    // Applique le volume sauvegardé au lancement du jeu
    public static void ApplySavedSettings()
    {
        AudioListener.volume = Volume;
    }
}