using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class WeaponManager : NetworkBehaviour
{
    [Header("Armes disponibles")]
    public WeaponData[] weapons; // [0] Pistol, [1] Sniper, [2] Shotgun

    [Header("Références")]
    public Camera fpsCam;
    public Transform weaponHoldPoint; // emplacement où accrocher le modèle 3D

    private int _currentWeaponIndex = 0;
    private float _nextFireTime = 0f;
    private GameObject _currentWeaponInstance;

    public WeaponData CurrentWeapon => weapons[_currentWeaponIndex];

    void Start()
    {
        UpdateWeaponVisual();
    }

    public void OnSwitchWeapon(InputValue value)
    {
        if (!IsOwner) return;

        float scroll = value.Get<float>();
        if (Mathf.Approximately(scroll, 0f)) return;

        int direction = scroll > 0 ? 1 : -1;
        _currentWeaponIndex = (_currentWeaponIndex + direction + weapons.Length) % weapons.Length;

        UpdateWeaponVisual();
        Debug.Log($"[Weapon] Arme actuelle : {CurrentWeapon.weaponName}");
    }

    public void OnAttack(InputValue value)
    {
        if (!IsOwner) return;

        if (CurrentWeapon.isAutomatic)
        {
            _holdingFire = value.isPressed;
        }
        else if (value.isPressed && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + CurrentWeapon.fireRate;
            Shoot();
        }
    }

    private bool _holdingFire = false;

    void Update()
    {
        if (!IsOwner) return;

        if (CurrentWeapon.isAutomatic && _holdingFire && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + CurrentWeapon.fireRate;
            Shoot();
        }
    }

    // ── Change le modèle 3D affiché selon l'arme active ────────────────────────
    private void UpdateWeaponVisual()
    {
        Debug.Log($"[Weapon] === UpdateWeaponVisual appelé === IsOwner: {IsOwner}");

        // Détruit l'ancien modèle affiché
        if (_currentWeaponInstance != null)
        {
            Debug.Log($"[Weapon] Destruction de l'ancien modèle: {_currentWeaponInstance.name}");
            Destroy(_currentWeaponInstance);
        }

        if (weaponHoldPoint == null)
        {
            Debug.LogWarning("[Weapon] weaponHoldPoint est NULL ! Assigne-le dans l'Inspector.");
            return;
        }

        if (CurrentWeapon.weaponModelPrefab == null)
        {
            Debug.LogWarning($"[Weapon] weaponModelPrefab est NULL pour l'arme {CurrentWeapon.weaponName} !");
            return;
        }

        Debug.Log($"[Weapon] holdPoint chemin complet: {GetFullPath(weaponHoldPoint)} | childCount avant: {weaponHoldPoint.childCount}");

        // Instancie le nouveau modèle comme enfant du point d'accroche
        _currentWeaponInstance = Instantiate(
            CurrentWeapon.weaponModelPrefab,
            weaponHoldPoint
        );

        _currentWeaponInstance.transform.localPosition = CurrentWeapon.modelPositionOffset;
        _currentWeaponInstance.transform.localRotation = Quaternion.Euler(CurrentWeapon.modelRotationOffset);
        _currentWeaponInstance.transform.localScale = CurrentWeapon.modelScale;

        Debug.Log($"[Weapon] Modèle instancié: {_currentWeaponInstance.name} | " +
                  $"Active: {_currentWeaponInstance.activeSelf} | " +
                  $"World Position: {_currentWeaponInstance.transform.position} | " +
                  $"childCount après: {weaponHoldPoint.childCount} | " +
                  $"chemin complet de l'instance: {GetFullPath(_currentWeaponInstance.transform)}");
    }

    private string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    // ── Tir avec gestion multi-pellets (shotgun) ───────────────────────────────
    private void Shoot()
    {
        WeaponData weapon = CurrentWeapon;

        for (int i = 0; i < weapon.pelletsCount; i++)
        {
            Vector3 direction = GetSpreadDirection(weapon.spreadAngle);
            Ray ray = new Ray(fpsCam.transform.position, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, weapon.range))
            {
                NetworkObject netObj = hit.collider.GetComponentInParent<NetworkObject>();
                ulong targetId = netObj != null ? netObj.NetworkObjectId : ulong.MaxValue;

                ShootServerRpc(targetId, weapon.damage);
            }
        }
    }

    private Vector3 GetSpreadDirection(float spreadAngle)
    {
        Vector3 baseDirection = fpsCam.transform.forward;

        if (spreadAngle <= 0f) return baseDirection;

        float randomAngleX = Random.Range(-spreadAngle, spreadAngle);
        float randomAngleY = Random.Range(-spreadAngle, spreadAngle);

        Quaternion spreadRotation = Quaternion.Euler(randomAngleY, randomAngleX, 0f);
        return spreadRotation * baseDirection;
    }

    [ServerRpc]
    private void ShootServerRpc(ulong targetNetworkObjectId, float dmg)
    {
        if (targetNetworkObjectId == ulong.MaxValue) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                .TryGetValue(targetNetworkObjectId, out NetworkObject target))
        {
            if (target.TryGetComponent<Health>(out var health))
                health.TakeDamage(dmg, OwnerClientId);
        }
    }
}