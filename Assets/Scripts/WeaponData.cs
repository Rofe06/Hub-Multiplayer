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

    [Header("Visuel — Modèle 3D")]
    public GameObject weaponModelPrefab;            // le modèle FBX/OBJ importé
    public Vector3 modelPositionOffset = Vector3.zero; // ajustement fin de position
    public Vector3 modelRotationOffset = Vector3.zero; // ajustement fin de rotation
    public Vector3 modelScale = Vector3.one;  // échelle du modèle
}