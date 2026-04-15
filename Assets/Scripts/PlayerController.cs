using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float mouseSensitivity = 2f;

    private Camera playerCamera;
    private float xRotation = 0f;

    public override void OnNetworkSpawn()
    {
        playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera == null)
        {
            Debug.LogError("Aucune caméra trouvée dans les enfants du Player !");
            return;
        }

        bool isLocal = IsOwner;
        playerCamera.enabled = isLocal;

        if (playerCamera.TryGetComponent<AudioListener>(out var al))
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

    void Update()
    {
        if (!IsOwner) return;
        HandleLook();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation = Mathf.Clamp(xRotation - mouseY, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}