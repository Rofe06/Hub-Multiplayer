using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Caméra")]
    [SerializeField] private float mouseSensitivity = 2f;

    private Camera _camera;
    private float _xRotation = 0f;
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
}