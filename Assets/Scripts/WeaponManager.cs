using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class WeaponManager : NetworkBehaviour
{
    [Header("Armes disponibles")]
    public WeaponData[] weapons; // [0] Pistol, [1] Sniper, [2] Shotgun

    [Header("Références")]
    public Camera fpsCam;
    public Transform weaponHoldPoint;

    private int _currentWeaponIndex = 0;
    private float _nextFireTime = 0f;
    private GameObject _currentWeaponInstance;

    // ── Munitions par arme (indexé comme le tableau weapons) ───────────────────
    private int[] _currentAmmoInMag;   // munitions dans le chargeur actuel
    private int[] _currentReserve;     // munitions en réserve
    private bool _isReloading = false;
    private float _reloadEndTime = 0f;

    public WeaponData CurrentWeapon => weapons[_currentWeaponIndex];
    public int CurrentAmmoInMag => _currentAmmoInMag[_currentWeaponIndex];
    public int CurrentReserve => _currentReserve[_currentWeaponIndex];
    public bool IsReloading => _isReloading;

    // Event pour le HUD
    public event System.Action OnAmmoChanged;

    void Start()
    {
        // Initialise les munitions au max pour chaque arme
        _currentAmmoInMag = new int[weapons.Length];
        _currentReserve = new int[weapons.Length];

        for (int i = 0; i < weapons.Length; i++)
        {
            _currentAmmoInMag[i] = weapons[i].magazineSize;
            _currentReserve[i] = weapons[i].maxReserve;
        }

        UpdateWeaponVisual();
    }

    public void OnSwitchWeapon(InputValue value)
    {
        if (!IsOwner) return;

        float scroll = value.Get<float>();
        if (Mathf.Approximately(scroll, 0f)) return;

        // Annule un rechargement en cours si on change d'arme
        _isReloading = false;

        int direction = scroll > 0 ? 1 : -1;
        _currentWeaponIndex = (_currentWeaponIndex + direction + weapons.Length) % weapons.Length;

        UpdateWeaponVisual();
        OnAmmoChanged?.Invoke();
    }

    // ── Callback touche R ────────────────────────────────────────────────────
    public void OnReload(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        TryStartReload();
    }

    public void OnAttack(InputValue value)
    {
        if (!IsOwner) return;

        if (CurrentWeapon.isAutomatic)
        {
            _holdingFire = value.isPressed;
        }
        else if (value.isPressed)
        {
            TryShoot();
        }
    }

    private bool _holdingFire = false;

    void Update()
    {
        if (!IsOwner) return;

        // Tir automatique
        if (CurrentWeapon.isAutomatic && _holdingFire)
            TryShoot();

        // Fin du rechargement
        if (_isReloading && Time.time >= _reloadEndTime)
            FinishReload();
    }

    // ── Tentative de tir : vérifie munitions + cooldown ────────────────────────
    private void TryShoot()
    {
        if (_isReloading) return;
        if (Time.time < _nextFireTime) return;

        // Plus de munitions dans le chargeur → rechargement auto
        if (CurrentAmmoInMag <= 0)
        {
            TryStartReload();
            return;
        }

        _nextFireTime = Time.time + CurrentWeapon.fireRate;
        _currentAmmoInMag[_currentWeaponIndex]--;
        OnAmmoChanged?.Invoke();

        Shoot();
    }

    // ── Lance un rechargement si possible ────────────────────────────────────
    private void TryStartReload()
    {
        if (_isReloading) return;
        if (CurrentAmmoInMag >= CurrentWeapon.magazineSize) return; // déjà plein
        if (!CurrentWeapon.infiniteReserve && CurrentReserve <= 0) return; // plus de munitions en réserve

        _isReloading = true;
        _reloadEndTime = Time.time + CurrentWeapon.reloadTime;

        PlayReloadSound();

        Debug.Log($"[Weapon] Rechargement de {CurrentWeapon.weaponName}...");
    }

    private void FinishReload()
    {
        _isReloading = false;

        int needed = CurrentWeapon.magazineSize - CurrentAmmoInMag;

        if (CurrentWeapon.infiniteReserve)
        {
            // Réserve infinie : remplit le chargeur sans jamais la diminuer
            _currentAmmoInMag[_currentWeaponIndex] += needed;
        }
        else
        {
            int taken = Mathf.Min(needed, CurrentReserve);
            _currentAmmoInMag[_currentWeaponIndex] += taken;
            _currentReserve[_currentWeaponIndex] -= taken;
        }

        OnAmmoChanged?.Invoke();
        Debug.Log($"[Weapon] Rechargement terminé : {CurrentAmmoInMag}/{CurrentWeapon.magazineSize} (réserve: {CurrentReserve})");
    }

    // ── Change le modèle 3D affiché selon l'arme active ────────────────────────
    private void UpdateWeaponVisual()
    {
        if (_currentWeaponInstance != null)
            Destroy(_currentWeaponInstance);

        if (weaponHoldPoint == null || CurrentWeapon.weaponModelPrefab == null)
            return;

        _currentWeaponInstance = Instantiate(CurrentWeapon.weaponModelPrefab, weaponHoldPoint);
        _currentWeaponInstance.transform.localPosition = CurrentWeapon.modelPositionOffset;
        _currentWeaponInstance.transform.localRotation = Quaternion.Euler(CurrentWeapon.modelRotationOffset);
        _currentWeaponInstance.transform.localScale = CurrentWeapon.modelScale;
    }

    // ── Tir avec gestion multi-pellets (shotgun) ───────────────────────────────
    private void Shoot()
    {
        WeaponData weapon = CurrentWeapon;

        // Joue le son de tir, synchronisé pour tous les joueurs
        PlayFireSoundServerRpc();

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

    // ── Son de tir : diffusé à tous les clients depuis le serveur ──────────────
    [ServerRpc]
    private void PlayFireSoundServerRpc()
    {
        PlayFireSoundClientRpc();
    }

    [ClientRpc]
    private void PlayFireSoundClientRpc()
    {
        if (CurrentWeapon.fireSound == null || weaponHoldPoint == null) return;
        AudioSource.PlayClipAtPoint(CurrentWeapon.fireSound, weaponHoldPoint.position);
    }

    // ── Son de rechargement : local suffit (peu critique pour les autres) ──────
    private void PlayReloadSound()
    {
        if (CurrentWeapon.reloadSound == null || weaponHoldPoint == null) return;
        AudioSource.PlayClipAtPoint(CurrentWeapon.reloadSound, weaponHoldPoint.position);
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