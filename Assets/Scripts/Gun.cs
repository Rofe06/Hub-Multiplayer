using UnityEngine;
using Unity.Netcode;

public class Gun : MonoBehaviour
{
    [Header("Stats")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.2f;

    [Header("Références")]
    public Camera fpsCam;
    public Transform gunMuzzle;

    [Header("Effets visuels")]
    public GameObject impactEffectPrefab;

    private float _nextFireTime = 0f;
    private NetworkObject _playerNetObj; // NetworkObject du Player parent

    void Start()
    {
        // Récupère le NetworkObject sur le Player parent
        _playerNetObj = GetComponentInParent<NetworkObject>();
    }

    void Update()
    {
        Debug.Log($"[Gun] RAW F: {Input.GetKeyDown(KeyCode.F)}");
        if (_playerNetObj == null || !_playerNetObj.IsOwner)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[Gun] Clic détecté ! fpsCam: {fpsCam}");
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (fpsCam == null)
        {
            Debug.LogWarning("[Gun] fpsCam non assigné !");
            return;
        }

        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.Log($"[Gun] Tir ! hit: {hit.collider.name}");

            // Cherche un Health sur l'objet touché ou ses parents
            Health health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
                ShootServerRpc(hit.collider.gameObject.GetComponent<NetworkObject>() != null
                    ? hit.collider.gameObject.GetComponent<NetworkObject>().NetworkObjectId
                    : ulong.MaxValue, damage);

            SpawnImpactEffect(hit.point, hit.normal);
        }
        else
        {
            Debug.Log("[Gun] Tir dans le vide !");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc(ulong targetNetworkId, float dmg)
    {
        // Retrouve l'objet via son NetworkObjectId (fiable en multijoueur)
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                targetNetworkId, out NetworkObject target))
        {
            if (target.TryGetComponent<Health>(out var health))
                health.TakeDamage(dmg);
        }
    }

    private void SpawnImpactEffect(Vector3 point, Vector3 normal)
    {
        if (impactEffectPrefab != null)
        {
            var fx = Instantiate(impactEffectPrefab, point,
                                 Quaternion.LookRotation(normal));
            Destroy(fx, 1.5f);
        }
    }
}