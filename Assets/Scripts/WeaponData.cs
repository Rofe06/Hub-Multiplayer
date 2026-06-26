using UnityEngine;

// ScriptableObject : permet de créer des "fiches d'arme" réutilisables dans l'éditeur
[CreateAssetMenu(fileName = "NewWeapon", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identité")]
    public string weaponName = "Pistol";

    [Header("Dégâts & Portée")]
    public float damage = 25f;
    public float range = 100f;

    [Header("Cadence de tir")]
    public float fireRate = 0.2f;     // secondes entre 2 tirs
    public bool isAutomatic = false; // maintenir le clic ou semi-auto

    [Header("Shotgun (multi-pellets)")]
    public int pelletsCount = 1;    // 1 pour pistolet/sniper, ~8 pour shotgun
    public float spreadAngle = 0f;   // dispersion en degrés (0 = précis)

    [Header("Munitions")]
    public int magazineSize = 12;  // taille du chargeur
    public int maxReserve = 90;  // munitions totales en réserve (hors chargeur)
    public float reloadTime = 1.5f; // durée du rechargement en secondes
    public bool infiniteReserve = false; // true = jamais à court de munitions (arme de base)

    [Header("Visuel — Modèle 3D")]
    public GameObject weaponModelPrefab;            // le modèle FBX/OBJ importé
    public Vector3 modelPositionOffset = Vector3.zero; // ajustement fin de position
    public Vector3 modelRotationOffset = Vector3.zero; // ajustement fin de rotation
    public Vector3 modelScale = Vector3.one;  // échelle du modèle

    [Header("Audio")]
    public AudioClip fireSound;   // son joué à chaque tir
    public AudioClip reloadSound; // son joué au rechargement (optionnel)
}