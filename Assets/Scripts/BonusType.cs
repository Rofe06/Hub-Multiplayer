using UnityEngine;

/// <summary>
/// Types de bonus disponibles à la mort d'un joueur.
/// Pour ajouter un bonus : ajoutez une entrée ici ET dans BonusDatabase.All.
/// </summary>
public enum BonusType
{
    SpeedBoost   = 0,
    MaxHealth    = 1,
    DamageBoost  = 2,
    JumpBoost    = 3,
    Regeneration = 4,
    ArmorBoost   = 5
}

/// <summary>Données d'affichage d'un bonus (icône, nom, description, couleur).</summary>
[System.Serializable]
public struct BonusData
{
    public string icon;
    public string displayName;
    public string description;
    public Color  color;
}

/// <summary>
/// Base de données statique des bonus.
/// Les indices de All doivent correspondre aux valeurs entières de BonusType.
/// </summary>
public static class BonusDatabase
{
    public static readonly BonusData[] All = new BonusData[]
    {
        // SpeedBoost
        new BonusData { icon = "⚡", displayName = "Vitesse",
            description = "+20% vitesse de déplacement",
            color = new Color(1.00f, 0.80f, 0.00f) },

        // MaxHealth
        new BonusData { icon = "❤", displayName = "Vitalité",
            description = "+50 HP maximum",
            color = new Color(0.90f, 0.20f, 0.20f) },

        // DamageBoost
        new BonusData { icon = "🔥", displayName = "Dégâts",
            description = "+25% dégâts infligés",
            color = new Color(1.00f, 0.40f, 0.00f) },

        // JumpBoost
        new BonusData { icon = "↑", displayName = "Saut",
            description = "+30% hauteur de saut",
            color = new Color(0.40f, 0.80f, 1.00f) },

        // Regeneration
        new BonusData { icon = "+", displayName = "Régénération",
            description = "+3 HP/s de régénération",
            color = new Color(0.20f, 0.85f, 0.30f) },

        // ArmorBoost
        new BonusData { icon = "🛡", displayName = "Armure",
            description = "-20% dégâts reçus (max 75%)",
            color = new Color(0.55f, 0.55f, 0.90f) },
    };

    /// <summary>Retourne les données d'affichage d'un bonus.</summary>
    public static BonusData Get(BonusType type) => All[(int)type];

    /// <summary>Retourne <paramref name="count"/> bonus aléatoires distincts (mélange Fisher-Yates).</summary>
    public static BonusType[] GetRandom(int count)
    {
        int total   = All.Length;
        int[] order = new int[total];
        for (int i = 0; i < total; i++) order[i] = i;

        for (int i = total - 1; i > 0; i--)
        {
            int j    = Random.Range(0, i + 1);
            int tmp  = order[i];
            order[i] = order[j];
            order[j] = tmp;
        }

        BonusType[] result = new BonusType[count];
        for (int i = 0; i < count; i++)
            result[i] = (BonusType)order[i];
        return result;
    }
}
