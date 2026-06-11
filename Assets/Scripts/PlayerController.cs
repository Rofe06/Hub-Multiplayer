using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Caméra")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Tir")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.2f;

    private Camera _camera;
    private float _xRotation = 0f;
    private float _nextFireTime = 0f;
    private Vector2 _lookInput;

    public override void OnNetworkSpawn()
    {
        _camera = GetComponentInChildren<Camera>(true);

        if (_camera == null)
        {
            Debug.LogError("Aucune caméra trouvée !");
            return;
        }

        bool isLocal = IsOwner;
        _camera.enabled = isLocal;

        if (_camera.TryGetComponent<AudioListener>(out var al))
            al.enabled = isLocal;

        if (isLocal)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            enabled = false;
        }
    }

    public void OnLook(InputValue value)
    {
        _lookInput = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (!IsOwner) return;

        if (value.isPressed && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        HandleLook();
    }

    private void HandleLook()
    {
        float mouseX = _lookInput.x * mouseSensitivity;
        float mouseY = _lookInput.y * mouseSensitivity;

        _xRotation = Mathf.Clamp(_xRotation - mouseY, -90f, 90f);
        _camera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void Shoot()
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.Log($"[Shoot] Hit: {hit.collider.name}");

            // Cherche un NetworkObject sur l'objet touché ou ses parents
            NetworkObject netObj = hit.collider.GetComponentInParent<NetworkObject>();
            ulong targetId = netObj != null ? netObj.NetworkObjectId : ulong.MaxValue;

            ShootServerRpc(targetId, damage);
        }
        else
        {
            Debug.Log("[Shoot] Tir dans le vide !");
        }
    }

    [ServerRpc]
    private void ShootServerRpc(ulong targetNetworkObjectId, float dmg)
    {
        if (targetNetworkObjectId == ulong.MaxValue) return;

        // Retrouve l'objet via son NetworkObjectId — fiable même avec plusieurs clones
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                .TryGetValue(targetNetworkObjectId, out NetworkObject target))
        {
            if (target.TryGetComponent<Health>(out var health))
            {
                health.TakeDamage(dmg);
                Debug.Log($"[Shoot] Dégâts infligés à {target.name} : -{dmg} HP");
            }
            else
            {
                Debug.Log($"[Shoot] {target.name} n'a pas de composant Health !");
            }
        }
    }
}