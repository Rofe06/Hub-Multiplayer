using UnityEngine;

public enum GameMode
{
    KillCount = 0, // premier à X kills
    TimeLimit = 1  // le plus de kills quand le temps est écoulé
}

// Réglages du mode de jeu choisis dans le menu avant de lancer une partie.
// Seul le Host applique réellement ces valeurs (c'est lui qui fait autorité).
public static class GameModeSettings
{
    public static GameMode SelectedMode = GameMode.KillCount;
    public static int KillTarget = 10;   // pour le mode KillCount
    public static float TimeLimitMinutes = 5f; // pour le mode TimeLimit
}